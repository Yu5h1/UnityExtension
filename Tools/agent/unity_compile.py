"""Compile a Unity assembly definition against installed editors, without opening Unity.

Usage:
  python unity_compile.py <path/to/X.asmdef> [--editors 6000.3.9f1,6000.6.0f1]
                          [--reference-project <unity project> ...] [--define SYMBOL ...] [--keep]

For each editor the tool compiles the asmdef's *current* source files in a throwaway SDK-style project
(netstandard2.1, C# 9). References and define symbols come, in order of preference, from:

1. A consumer project on that editor version that has Unity's generated `<AssemblyName>.csproj` in its root
   (written when Unity syncs IDE projects). Its HintPaths and DefineConstants are exactly what Unity uses
   for that assembly in that project, so the check matches Unity. Found automatically under W:/UnityProject,
   or named with --reference-project. The csproj is only as fresh as Unity's last IDE sync: when its source
   files or project references differ from the asmdef's current ones, the tool prints a warning and compiles
   anyway.
2. Otherwise a generic set: every DLL in the editor's Data/Managed/UnityEngine (Unity references UnityEditor
   for runtime assemblies too when compiling in the editor), the Core DLLs, Unity's version symbols,
   UNITY_EDITOR and standalone Windows symbols. Precompiled DLLs a consumer project adds (NUnit,
   Newtonsoft.Json ...) are missing in this mode, so its errors can be false.

Referenced asmdefs found under UnityExtension/Packages or Unity/Core are compiled first from their current
sources. A reference found in neither (a Unity package such as Unity.Addressables) is looked up in the packages
that editor ships, following the referencing package's package.json dependencies: the editor's
Data/Resources/PackageManager/BuiltInPackages/<name> folder, else Data/Resources/PackageManager/Editor/<name>-<version>.tgz
extracted into the temp folder (the declared version when shipped, else the newest), and compiled from source with
the generic references. Package sources are never modified; everything is built in a temp folder.
Exit code: 0 when every editor compiled without errors, 1 otherwise, 2 for a setup problem.
"""
import argparse
import glob
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile

HUB_EDITORS = r"C:\Program Files\Unity\Hub\Editor"
PROJECTS_ROOT = r"W:\UnityProject"
EXTENSION_ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
PACKAGES = os.path.join(EXTENSION_ROOT, "Packages")
# Where referenced asmdefs are looked up, in order; the first asmdef with a given name wins.
ASMDEF_ROOTS = [PACKAGES, os.path.normpath(os.path.join(EXTENSION_ROOT, "..", "Core"))]
CORE_RUNTIME = os.path.join(PACKAGES, "Core", "Runtime", "Yu5h1Lib.Runtime.dll")
CORE_EDITOR = os.path.join(PACKAGES, "Core", "Editor", "Yu5h1Lib.Editor.dll")
LEGACY_OR_NEWER = ["5_3", "5_4", "5_5", "5_6"] + [f"{y}_{m}" for y in range(2017, 2023) for m in range(1, 5)
                                                if not (y == 2022 and m == 4)] + ["2023_1", "2023_2", "2023_3"]
BASE_DEFINES = [
    "UNITY_EDITOR", "UNITY_EDITOR_64", "UNITY_EDITOR_WIN", "UNITY_64", "PLATFORM_ARCH_64",
    "UNITY_STANDALONE_WIN", "UNITY_STANDALONE", "PLATFORM_STANDALONE_WIN", "PLATFORM_STANDALONE",
    "UNITY_ASSERTIONS", "ENABLE_MONO", "ENABLE_UNITYEVENTS", "ENABLE_UNITYWEBREQUEST", "ENABLE_PROFILER",
    "NET_STANDARD_2_0", "NET_STANDARD_2_1", "NET_STANDARD", "NETSTANDARD", "NETSTANDARD2_1", "DEBUG", "TRACE",
]


def version_defines(version):
    major, minor, patch = re.match(r"(\d+)\.(\d+)\.(\d+)", version).groups()
    names = [f"UNITY_{major}_{minor}_{patch}", f"UNITY_{major}_{minor}", f"UNITY_{major}"]
    names += [f"UNITY_{v}_OR_NEWER" for v in LEGACY_OR_NEWER]
    names += [f"UNITY_{major}_{m}_OR_NEWER" for m in range(0, int(minor) + 1)]
    return names


def load_asmdef(path):
    with open(path, encoding="utf-8-sig") as f:
        data = json.load(f)
    data["_path"] = os.path.abspath(path)
    data["_dir"] = os.path.dirname(data["_path"])
    data["_guid"] = None
    if os.path.exists(path + ".meta"):
        with open(path + ".meta", encoding="utf-8", errors="ignore") as f:
            m = re.search(r"guid:\s*([0-9a-f]{32})", f.read())
        data["_guid"] = m.group(1) if m else None
    return data


def add_asmdefs(by_key, root):
    for path in glob.glob(os.path.join(root, "**", "*.asmdef"), recursive=True):
        if "~" in os.path.relpath(path, root):
            continue
        a = load_asmdef(path)
        by_key.setdefault(a["name"], a)
        if a["_guid"]:
            by_key.setdefault("GUID:" + a["_guid"], a)


def asmdef_index():
    by_key = {}
    for root in ASMDEF_ROOTS:
        add_asmdefs(by_key, root)
    return by_key


def owning_package(asmdef):
    """The package.json of the package holding the asmdef, or None."""
    folder = asmdef["_dir"]
    while True:
        path = os.path.join(folder, "package.json")
        if os.path.isfile(path):
            with open(path, encoding="utf-8-sig") as f:
                return json.load(f)
        parent = os.path.dirname(folder)
        if parent == folder:
            return None
        folder = parent


def version_key(version):
    return [int(n) for n in re.findall(r"\d+", version)]


def shipped_package(ctx, name, version):
    """(folder, source) of package <name> as the editor ships it, a .tgz extracted into the work folder; (None, None)."""
    shipped = os.path.join(ctx["editor_dir"], "Data", "Resources", "PackageManager")
    builtin = os.path.join(shipped, "BuiltInPackages", name)
    if os.path.isfile(os.path.join(builtin, "package.json")):
        return builtin, "BuiltInPackages"
    tarballs = {}
    for path in glob.glob(os.path.join(shipped, "Editor", f"{name}-*.tgz")):
        shipped_version = os.path.basename(path)[len(name) + 1:-len(".tgz")]
        if re.fullmatch(r"\d[\w.+-]*", shipped_version):  # not another package sharing the prefix
            tarballs[shipped_version] = path
    if not tarballs:
        return None, None
    chosen = version if version in tarballs else max(tarballs, key=version_key)
    root = os.path.join(ctx["work"], ctx["version"], "_shipped", f"{name}-{chosen}")
    if not os.path.isdir(root):
        with tarfile.open(tarballs[chosen], "r:gz") as tar:
            for member in tar.getmembers():
                target = os.path.normpath(os.path.join(root, member.name))
                if target.startswith(root + os.sep) and (member.isfile() or member.isdir()):
                    tar.extract(member, root)
    return os.path.join(root, "package"), os.path.basename(tarballs[chosen])


def add_shipped_dependencies(asmdef, ctx):
    """Index the asmdefs of the shipped packages the asmdef's package depends on, transitively, once per package."""
    pending = list(((owning_package(asmdef) or {}).get("dependencies") or {}).items())
    # Test assemblies use the test framework without their package declaring it: Unity puts it in every project.
    if any("TestRunner" in r for r in asmdef.get("references", [])) or \
            "nunit.framework.dll" in asmdef.get("precompiledReferences", []):
        pending.append(("com.unity.test-framework", ""))
    while pending:
        name, version = pending.pop(0)
        if name in ctx["shipped"] or name.startswith(("com.unity.modules.", "com.yu5h1.")):
            continue
        folder, source = shipped_package(ctx, name, version)
        ctx["shipped"][name] = folder
        if folder is None:
            continue
        add_asmdefs(ctx["index"], folder)
        with open(os.path.join(folder, "package.json"), encoding="utf-8-sig") as f:
            package = json.load(f)
        ctx["notes"].add(f"{name} {package.get('version')}: shipped with {ctx['version']} ({source})")
        pending += list((package.get("dependencies") or {}).items())


def shipped_dll(ctx, dll_name):
    """A precompiled DLL of that file name inside the shipped packages indexed so far, or None."""
    for folder in ctx["shipped"].values():
        if folder:
            found = glob.glob(os.path.join(folder, "**", dll_name), recursive=True)
            if found:
                return sorted(found)[0]
    return None


def unity_version_defines(asmdef, version):
    """Defines of versionDefines on the Unity version, written as a bare minimum version, that this editor meets."""
    return [vd["define"] for vd in asmdef.get("versionDefines", [])
            if vd.get("name") == "Unity" and re.fullmatch(r"\d[\w.]*", vd.get("expression", ""))
            and version_key(version) >= version_key(vd["expression"])]


def sources(asmdef):
    """.cs files under the asmdef's folder, excluding folders that own another asmdef/asmref."""
    result = []
    for dirpath, dirnames, filenames in os.walk(asmdef["_dir"]):
        if dirpath != asmdef["_dir"] and any(n.endswith((".asmdef", ".asmref")) for n in filenames):
            dirnames[:] = []
            continue
        dirnames[:] = [d for d in dirnames if not d.startswith(".") and not d.endswith("~")]
        result += [os.path.join(dirpath, n) for n in filenames if n.endswith(".cs")]
    return sorted(result)


def project_version(project):
    try:
        with open(os.path.join(project, "ProjectSettings", "ProjectVersion.txt"), encoding="utf-8") as f:
            m = re.search(r"m_EditorVersion:\s*(\S+)", f.read())
        return m.group(1) if m else None
    except OSError:
        return None


def find_generated(assembly, version, candidates):
    """Newest generated <assembly>.csproj among candidate projects on this editor version."""
    found = []
    for project in candidates:
        csproj = os.path.join(project, assembly + ".csproj")
        if project_version(project) == version and os.path.isfile(csproj):
            found.append((os.path.getmtime(csproj), project, csproj))
    return max(found)[1:] if found else (None, None)


def read_generated(project, csproj):
    with open(csproj, encoding="utf-8-sig") as f:
        text = f.read()
    refs = []
    for hint in re.findall(r"<HintPath>([^<]+)</HintPath>", text):
        path = hint if os.path.isabs(hint) else os.path.join(project, hint)
        refs.append(os.path.normpath(path))
    m = re.search(r"<DefineConstants>([^<]*)</DefineConstants>", text)
    defines = [d for d in (m.group(1).split(";") if m else []) if d and d != "$(DefineConstants)"]
    project_refs = [os.path.splitext(os.path.basename(p))[0]
                    for p in re.findall(r'<ProjectReference Include="([^"]+)"', text)]
    absolute = lambda p: os.path.normpath(p if os.path.isabs(p) else os.path.join(project, p))
    compiled = [absolute(p) for p in re.findall(r'<Compile Include="([^"]+)"', text)]
    asmdef_item = next((absolute(p) for p in re.findall(r'<None Include="([^"]+\.asmdef)"', text)), None)
    return refs, defines, project_refs, compiled, asmdef_item


def stale_warnings(asmdef, project, csproj, refs, project_refs, compiled, asmdef_item, index):
    """Differences between a generated csproj and the asmdef's current sources and references."""
    name, problems = asmdef["name"], []
    # Compare paths relative to the asmdef folder, so a copy of the package elsewhere compares too.
    csproj_root = os.path.dirname(asmdef_item) if asmdef_item else asmdef["_dir"]
    def rel(path, root):
        try:
            return os.path.relpath(path, root)
        except ValueError:  # another drive
            return path
    current = {os.path.normcase(rel(p, asmdef["_dir"])): rel(p, asmdef["_dir"]) for p in sources(asmdef)}
    listed = {os.path.normcase(rel(p, csproj_root)): rel(p, csproj_root) for p in compiled}
    added = sorted(current[k] for k in current.keys() - listed.keys())
    removed = sorted(listed[k] for k in listed.keys() - current.keys())
    if added or removed:
        sample = added[:5] + removed[:5]
        problems.append(f"source files differ ({len(added)} not in the csproj, {len(removed)} listed but no longer "
                        f"in the sources: {', '.join(sample)}{' ...' if len(added) > 5 or len(removed) > 5 else ''})")
    declared = set()
    for ref in asmdef.get("references", []):
        dep = index.get(ref)
        if dep is not None:
            declared.add(dep["name"])
        elif not ref.startswith("GUID:"):
            declared.add(ref)
    in_csproj = set(project_refs) | {os.path.splitext(os.path.basename(r))[0] for r in refs}
    # A reference the project does not build (no csproj for it there) is dropped by Unity too, so it is not stale.
    missing = sorted(n for n in declared - in_csproj if os.path.isfile(os.path.join(project, n + ".csproj")))
    extra = sorted(n for n in set(project_refs) - declared if n in index)
    if missing or extra:
        problems.append("asmdef references differ (" + "; ".join(
            part for part in (f"not in the csproj: {', '.join(missing)}" if missing else "",
                              f"no longer in the asmdef: {', '.join(extra)}" if extra else "") if part) + ")")
    return [f"{name}: {csproj} looks stale: {p}; its references and defines may be out of date. Regenerate it in "
            f"Unity ({project}: Preferences > External Tools > Regenerate project files); compiling anyway"
            for p in problems]


def write_project(folder, asmdef, refs, defines, own_framework):
    xml_files = "\n".join(f'    <Compile Include="{s}" />' for s in sources(asmdef))
    xml_refs = "\n".join(
        f'    <Reference Include="{os.path.splitext(os.path.basename(r))[0]}"><HintPath>{r}</HintPath><Private>false</Private></Reference>'
        for r in refs)
    unsafe = "true" if asmdef.get("allowUnsafeCode") else "false"
    project = f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <AssemblyName>{asmdef['name']}</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <AllowUnsafeBlocks>{unsafe}</AllowUnsafeBlocks>
    <DefineConstants>{';'.join(defines)}</DefineConstants>
    <OutputPath>{os.path.join(folder, 'out')}</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
    <DisableImplicitFrameworkReferences>{'true' if own_framework else 'false'}</DisableImplicitFrameworkReferences>
    <NoStdLib>{'true' if own_framework else 'false'}</NoStdLib>
  </PropertyGroup>
  <ItemGroup>
{xml_files}
  </ItemGroup>
  <ItemGroup>
{xml_refs}
  </ItemGroup>
</Project>
"""
    path = os.path.join(folder, asmdef["name"] + ".csproj")
    with open(path, "w", encoding="utf-8") as f:
        f.write(project)
    return path


def build(asmdef, ctx, built):
    """Compile asmdef (referenced asmdefs first). Returns (dll or None, errors, warnings)."""
    name = asmdef["name"]
    if name in built:
        return built[name], [], []
    version, editor_dir = ctx["version"], ctx["editor_dir"]
    project, csproj = find_generated(name, version, ctx["candidates"])
    if csproj:
        refs, defines, dep_names, compiled, asmdef_item = read_generated(project, csproj)
        ctx["notes"].add(f"{name}: references and defines from {csproj}")
        ctx["warnings"].extend(stale_warnings(asmdef, project, csproj, refs, dep_names, compiled, asmdef_item,
                                              ctx["index"]))
        deps = []
        for dep_name in dep_names:
            dep = ctx["index"].get(dep_name)
            if dep is None:
                ctx["notes"].add(f"{name}: project reference {dep_name} not found under UnityExtension/Packages "
                                 f"or Unity/Core; not compiled")
            else:
                deps.append(dep)
    else:
        managed = os.path.join(editor_dir, "Data", "Managed", "UnityEngine")
        refs = sorted(glob.glob(os.path.join(managed, "*.dll"))) + [CORE_RUNTIME, CORE_EDITOR]
        defines = version_defines(version) + BASE_DEFINES
        ctx["notes"].add(f"{name}: no generated csproj on {version}; generic references (consumer precompiled DLLs missing)")
        deps = []
        for key in asmdef.get("references", []):
            if key not in ctx["index"]:
                add_shipped_dependencies(asmdef, ctx)
            dep = ctx["index"].get(key)
            if dep is None:
                ctx["notes"].add(f"{name}: reference {key} not found under UnityExtension/Packages, Unity/Core or "
                                 f"the packages {version} ships; not compiled")
            else:
                deps.append(dep)
        for dll_name in asmdef.get("precompiledReferences", []):
            if any(os.path.basename(r) == dll_name for r in refs):
                continue
            found = shipped_dll(ctx, dll_name) or (add_shipped_dependencies(asmdef, ctx) or shipped_dll(ctx, dll_name))
            if found:
                refs.append(found)
            else:
                ctx["notes"].add(f"{name}: precompiled reference {dll_name} not found in the packages {version} ships")
        evaluated = unity_version_defines(asmdef, version)
        defines = defines + evaluated
        for vd in asmdef.get("versionDefines", []):
            if vd.get("define") not in evaluated:
                ctx["notes"].add(f"{name}: versionDefine {vd.get('define')} not evaluated; treated as absent")
    dep_names_set = set()
    for dep in deps:
        dll, errors, warnings = build(dep, ctx, built)
        if dll is None:
            built[name] = None
            return None, [f"dependency {dep['name']} failed"] + errors, warnings
        dep_names_set.add(dep["name"])
        refs = [r for r in refs if os.path.splitext(os.path.basename(r))[0] != dep["name"]] + [dll]
    folder = os.path.join(ctx["work"], version, name)
    os.makedirs(folder, exist_ok=True)
    csproj_path = write_project(folder, asmdef, refs, defines + ctx["extra_defines"], own_framework=bool(csproj))
    out = subprocess.run(["dotnet", "build", csproj_path, "-nologo", "-v:q", "-clp:NoSummary"],
                         capture_output=True, text=True, encoding="utf-8", errors="ignore")
    lines = (out.stdout + out.stderr).splitlines()
    trim = lambda l: re.sub(r"\s*\[[^\]]+\.csproj\]\s*$", "", l.strip())
    errors = sorted({trim(l) for l in lines if re.search(r"error (CS|MSB|NU)\d+", l)})
    warnings = sorted({trim(l) for l in lines if re.search(r"warning CS\d+", l)})
    dll = os.path.join(folder, "out", name + ".dll")
    ok = out.returncode == 0 and os.path.exists(dll) and not errors
    built[name] = dll if ok else None
    return (dll if ok else None), errors, warnings


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("asmdef")
    parser.add_argument("--editors", default="6000.3.9f1,6000.6.0f1")
    parser.add_argument("--reference-project", action="append", default=[],
                        help="consumer Unity project whose generated csproj supplies references; default: all under W:/UnityProject")
    parser.add_argument("--define", action="append", default=[])
    parser.add_argument("--keep", action="store_true", help="keep the temp build folder")
    args = parser.parse_args(argv)

    if shutil.which("dotnet") is None:
        print("dotnet SDK not found", file=sys.stderr)
        return 2
    candidates = [os.path.abspath(p) for p in args.reference_project] or \
        sorted(p for p in glob.glob(os.path.join(PROJECTS_ROOT, "*")) if os.path.isdir(os.path.join(p, "Assets")))
    target = load_asmdef(args.asmdef)
    index = asmdef_index()
    work = tempfile.mkdtemp(prefix="unity_compile_")
    failed = False
    try:
        for version in [v.strip() for v in args.editors.split(",") if v.strip()]:
            editor_dir = os.path.join(HUB_EDITORS, version, "Editor")
            if not os.path.isdir(editor_dir):
                print(f"[{version}] editor not installed: {editor_dir}")
                failed = True
                continue
            ctx = {"version": version, "editor_dir": editor_dir, "candidates": candidates, "index": dict(index),
                   "shipped": {}, "work": work, "extra_defines": list(args.define), "notes": set(), "warnings": []}
            dll, errors, warnings = build(target, ctx, {})
            status = "ok" if dll else "FAILED"
            print(f"[{version}] {target['name']}: {status}, {len(errors)} error(s), {len(warnings)} warning(s)")
            for line in errors[:30]:
                print(f"  {line}")
            for line in ctx["warnings"]:
                print(f"  WARNING: {line}")
            for note in sorted(ctx["notes"]):
                print(f"  note: {note}")
            failed = failed or dll is None
    finally:
        if args.keep:
            print(f"build folder kept: {work}")
        else:
            shutil.rmtree(work, ignore_errors=True)
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
