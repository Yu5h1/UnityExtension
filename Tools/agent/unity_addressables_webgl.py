r"""Build Yu5h1LibTest's remote probe, preserving its settings.

Run only after approval for a WebGL build and temporary Player Settings changes:
  python Tools/agent/unity_addressables_webgl.py W:\UnityProject\Yu5h1LibTest
Requires the existing Assets/AddressablesTest/Editor/RemoteTestSetup.cs.
Outputs and timestamped settings backups live in the project's sibling -Build folder.
This builds a player; it starts no HTTP server and makes no browser-cache claim.
On Windows the runner has its own console process group, so Ctrl+C reaches this wrapper;
the wrapper terminates and waits for its owned process tree before restoring settings.
"""
import argparse
import datetime
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from unity_batch import kill_tree


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("project", type=Path)
    args = parser.parse_args()
    project = args.project.resolve(strict=True)
    if not (project / "Assets/AddressablesTest/Editor/RemoteTestSetup.cs").is_file():
        parser.error("the project must contain the existing Addressables remote test setup")
    lock = project / "Temp/UnityLockfile"
    if lock.exists():
        try:
            with lock.open("r+b"):
                pass
        except OSError:
            parser.error("project is open; use its editor instead")
    home = Path(__file__).resolve().parent
    source = home / "fixtures/AddressablesWebGL"
    destination = project / "Assets/AddressablesTest/WebGLProbe"
    for path in source.rglob("*"):
        target = destination / path.relative_to(source)
        if path.is_file() and target.exists() and target.read_bytes() != path.read_bytes():
            parser.error(f"probe differs; refusing to overwrite: {target}")
    build_root = project.with_name(project.name + "-Build")
    output = build_root / "addressables-webgl"
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    backup = build_root / "backup" / ("addressables-webgl-" + stamp)
    backup.mkdir(parents=True)
    paths = list((project / "ProjectSettings").glob("*"))
    paths += list((project / "UserSettings").glob("*.asset"))
    paths += list((project / "Assets/AddressableAssetsData").rglob("*.asset"))
    snapshot = {str(p.relative_to(project)): p.read_bytes() for p in paths if p.is_file()}
    for rel, data in snapshot.items():
        target = backup / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
    shutil.copytree(source, destination, dirs_exist_ok=True)
    try:
        process = subprocess.Popen([
            sys.executable, str(home / "unity_batch.py"), str(project), "--timeout", "1800",
            "--method", "WebGLRemoteBuild.Run", "--", "-buildTarget", "WebGL",
        ], env=dict(os.environ, ADDRESSABLES_WEBGL_OUT=str(output)),
            creationflags=subprocess.CREATE_NEW_PROCESS_GROUP if os.name == "nt" else 0)
        try:
            exit_code = process.wait()
            if exit_code == 0:
                shutil.copyfile(source / "probe.html", output / "probe.html")
        except KeyboardInterrupt:
            kill_tree(process.pid)
            process.wait()
            raise
    finally:
        for rel, data in snapshot.items():
            (project / rel).write_bytes(data)
        restored = {rel: hashlib.sha256((project / rel).read_bytes()).hexdigest()
                    == hashlib.sha256(data).hexdigest() for rel, data in snapshot.items()}
        (backup / "restoration.json").write_text(json.dumps(restored, indent=2), encoding="utf-8")
        print(f"Settings restored byte-identically: {all(restored.values())}; {len(restored)} files", flush=True)
        print(f"Backup: {backup}", flush=True)
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
