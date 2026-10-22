"""Compile a Unity assembly definition against installed editors, without opening Unity.

Usage:
  python unity_compile.py <path/to/X.asmdef> [--editors 6000.3.9f1,6000.6.0f1]
                          [--reference-project <unity project> ...] [--define SYMBOL ...] [--keep]

For each editor the tool compiles the asmdef's *current* source files in a throwaway SDK-style project
(netstandard2.1, C# 9). References and define symbols come, in order of preference, from:

1. A consumer project on that editor version that has Unity's generated `<AssemblyName>.csproj` in its root
   (written when Unity syncs IDE projects). Its HintPaths and DefineConstants are exactly what Unity uses
   for that assembly in that project, so the check matches Unity. Found automatically under W:/UnityProject,
   or named with --reference-project.
2. Otherwise a generic set: every DLL in the editor's Data/Managed/UnityEngine (Unity references UnityEditor
   for runtime assemblies too when compiling in the editor), the Core DLLs, Unity's version symbols,
   UNITY_EDITOR and standalone Windows symbols. Precompiled DLLs a consumer project adds (NUnit,
   Newtonsoft.Json ...) are missing in this mode, so its errors can be false.

Referenced asmdefs found under UnityExtension/Packages are compiled first from their current sources.
Package sources are never modified; everything is built in a temp folder.
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
import tempfile

HUB_EDITORS = r"C:\Program Files\Unity\Hub\Editor"
PROJECTS_ROOT = r"W:\UnityProject"
EXTENSION_ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
PACKAGES = os.path.join(EXTENSION_ROOT, "Packages")
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


def asmdef_index():
    by_key = {}
    for path in glob.glob(os.path.join(PACKAGES, "**", "*.asmdef"), recursive=True):
        if "~" in path:
            continue
        a = load_asmdef(path)
        by_key[a["name"]] = a
        if a["_guid"]:
            by_key["GUID:" + a["_guid"]] = a
    return by_key


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
    return refs, defines, project_refs


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
        refs, defines, dep_names = read_generated(project, csproj)
        ctx["notes"].add(f"{name}: references and defines from {csproj}")
        deps = []
        for dep_name in dep_names:
            dep = ctx["index"].get(dep_name)
            if dep is None:
                ctx["notes"].add(f"{name}: project reference {dep_name} is not under UnityExtension/Packages; not compiled")
            else:
                deps.append(dep)
    else:
        managed = os.path.join(editor_dir, "Data", "Managed", "UnityEngine")
        refs = sorted(glob.glob(os.path.join(managed, "*.dll"))) + [CORE_RUNTIME, CORE_EDITOR]
        defines = version_defines(version) + BASE_DEFINES
        ctx["notes"].add(f"{name}: no generated csproj on {version}; generic references (consumer precompiled DLLs missing)")
        deps = []
        for key in asmdef.get("references", []):
            dep = ctx["index"].get(key)
            if dep is None:
                ctx["notes"].add(f"{name}: reference {key} is not under UnityExtension/Packages; not compiled")
            else:
                deps.append(dep)
        for vd in asmdef.get("versionDefines", []):
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
            ctx = {"version": version, "editor_dir": editor_dir, "candidates": candidates, "index": index,
                   "work": work, "extra_defines": list(args.define), "notes": set()}
            dll, errors, warnings = build(target, ctx, {})
            status = "ok" if dll else "FAILED"
            print(f"[{version}] {target['name']}: {status}, {len(errors)} error(s), {len(warnings)} warning(s)")
            for line in errors[:30]:
                print(f"  {line}")
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
