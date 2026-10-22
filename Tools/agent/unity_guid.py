"""GUID and asset index for a Unity project, without opening Unity.

Usage:
  python unity_guid.py <project> find <guid>          asset path, package and type for a GUID
  python unity_guid.py <project> guid <asset-path>    GUID of an asset (path relative to the project or absolute)
  python unity_guid.py <project> refs <guid|path>     files that reference the asset (a GUID must belong to an indexed asset)
Options:
  --cache   also index Library/PackageCache (registry packages); slower
  --json    print JSON instead of text

Exit code: 0 found, 1 not found, 2 not a Unity project or bad arguments.

Covers Assets, embedded packages under Packages/, and file: packages from Packages/manifest.json.
Reads only the guid line of .meta files and GUID strings inside asset files; it never parses YAML structure.
"""
import argparse
import json
import os
import re
import sys

GUID_RE = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.M)
YAML_ASSET_EXT = {
    ".unity", ".prefab", ".asset", ".mat", ".controller", ".overridecontroller", ".anim", ".playable",
    ".mask", ".signal", ".lighting", ".spriteatlas", ".spriteatlasv2", ".physicmaterial", ".physicsmaterial2d",
    ".guiskin", ".fontsettings", ".terrainlayer", ".mixer", ".preset", ".brush", ".flare", ".rendertexture",
    ".cubemap", ".giparams", ".shadervariants", ".asmdef", ".asmref", ".uxml", ".uss", ".tss",
}


def roots(project, include_cache):
    """(label, directory) pairs to index: Assets, embedded and file: packages, optionally the package cache."""
    project = os.path.abspath(project)
    found = [("Assets", os.path.join(project, "Assets"))]
    packages_dir = os.path.join(project, "Packages")
    manifest = os.path.join(packages_dir, "manifest.json")
    if os.path.isdir(packages_dir):
        for entry in sorted(os.listdir(packages_dir)):
            full = os.path.join(packages_dir, entry)
            if os.path.isfile(os.path.join(full, "package.json")):
                found.append((entry, full))
    if os.path.isfile(manifest):
        with open(manifest, encoding="utf-8-sig") as f:
            deps = json.load(f).get("dependencies", {})
        for name, value in sorted(deps.items()):
            if isinstance(value, str) and value.startswith("file:"):
                path = value[5:]
                if not os.path.isabs(path):
                    path = os.path.normpath(os.path.join(packages_dir, path))
                if os.path.isdir(path):
                    found.append((name, path))
    if include_cache:
        cache = os.path.join(project, "Library", "PackageCache")
        if os.path.isdir(cache):
            for entry in sorted(os.listdir(cache)):
                found.append((entry.split("@")[0], os.path.join(cache, entry)))
    return found


def build_index(project, include_cache):
    """GUID -> {path, package} for every .meta under the indexed roots."""
    index = {}
    for label, base in roots(project, include_cache):
        for dirpath, dirnames, filenames in os.walk(base):
            dirnames[:] = [d for d in dirnames if not d.startswith(".") and not d.endswith("~")]
            for name in filenames:
                if not name.endswith(".meta"):
                    continue
                meta = os.path.join(dirpath, name)
                try:
                    with open(meta, encoding="utf-8", errors="ignore") as f:
                        head = f.read(512)
                except OSError:
                    continue
                m = GUID_RE.search(head)
                if m:
                    index.setdefault(m.group(1), {"path": meta[:-5].replace("\\", "/"), "package": label})
    return index


def describe(entry):
    path = entry["path"]
    ext = os.path.splitext(path)[1].lower()
    info = dict(entry)
    info["type"] = "folder" if os.path.isdir(path) else (ext[1:] if ext else "file")
    if ext == ".cs":
        info["class"] = os.path.splitext(os.path.basename(path))[0]
    return info


def to_abs(project, path):
    return os.path.normpath(path if os.path.isabs(path) else os.path.join(project, path)).replace("\\", "/")


def guid_of(path):
    with open(path + ".meta", encoding="utf-8", errors="ignore") as f:
        m = GUID_RE.search(f.read(512))
    return m.group(1) if m else None


def find_refs(project, guid, include_cache):
    hits = []
    needle = guid.encode("ascii")
    for label, base in roots(project, include_cache):
        for dirpath, dirnames, filenames in os.walk(base):
            dirnames[:] = [d for d in dirnames if not d.startswith(".") and not d.endswith("~")]
            for name in filenames:
                if os.path.splitext(name)[1].lower() not in YAML_ASSET_EXT:
                    continue
                full = os.path.join(dirpath, name)
                try:
                    with open(full, "rb") as f:
                        if needle in f.read():
                            hits.append({"path": full.replace("\\", "/"), "package": label})
                except OSError:
                    continue
    return hits


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("project")
    parser.add_argument("command", choices=["find", "guid", "refs"])
    parser.add_argument("target")
    parser.add_argument("--cache", action="store_true")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)
    project = os.path.abspath(args.project)
    if not os.path.isdir(os.path.join(project, "Assets")):
        print(f"not a Unity project: {project}", file=sys.stderr)
        return 2

    if args.command == "guid":
        path = to_abs(project, args.target)
        guid = guid_of(path) if os.path.exists(path + ".meta") else None
        result = {"path": path, "guid": guid}
    elif args.command == "find":
        entry = build_index(project, args.cache).get(args.target.lower())
        result = describe(entry) if entry else None
    else:
        guid = args.target.lower()
        if not re.fullmatch(r"[0-9a-f]{32}", guid):
            path = to_abs(project, args.target)
            guid = guid_of(path) if os.path.exists(path + ".meta") else None
            if guid is None:
                print(f"no .meta for {path}", file=sys.stderr)
                return 1
        elif guid not in build_index(project, args.cache):
            guid = None
        result = {"guid": guid, "references": find_refs(project, guid, args.cache)} if guid else None

    if args.json:
        print(json.dumps(result, ensure_ascii=False, indent=2))
    elif result is None or (args.command == "guid" and result["guid"] is None):
        print("not found")
    elif args.command == "refs":
        print(f"{result['guid']}: {len(result['references'])} file(s)")
        for hit in result["references"]:
            print(f"  [{hit['package']}] {hit['path']}")
    else:
        for key, value in result.items():
            print(f"{key}: {value}")
    found = result is not None and not (args.command == "guid" and result["guid"] is None)
    return 0 if found else 1


if __name__ == "__main__":
    sys.exit(main())
