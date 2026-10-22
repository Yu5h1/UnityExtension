"""Run a Unity project in batchmode with the project's own editor.

Usage:
  python unity_batch.py <project> [--method Namespace.Class.Method] [--timeout 600] [--log-dir DIR] [-- extra Unity args]

- The editor comes from ProjectSettings/ProjectVersion.txt (C:/Program Files/Unity/Hub/Editor/<version>);
  --editor overrides it.
- Refuses to start when the project is already open in an editor (Temp/UnityLockfile exists).
- Writes the log to <project's parent>/<project name>-Build/logs/ unless --log-dir is given.
- Kills the Unity process tree when the timeout expires.
- Prints a summary (editor, seconds, exit code, compile errors) and exits with Unity's exit code;
  2 means it refused to start, 124 means it timed out.
"""
import argparse
import datetime
import os
import re
import subprocess
import sys
import time

HUB_EDITORS = r"C:\Program Files\Unity\Hub\Editor"


def editor_for(project):
    version_file = os.path.join(project, "ProjectSettings", "ProjectVersion.txt")
    with open(version_file, encoding="utf-8") as f:
        m = re.search(r"m_EditorVersion:\s*(\S+)", f.read())
    version = m.group(1)
    return version, os.path.join(HUB_EDITORS, version, "Editor", "Unity.exe")


def kill_tree(pid):
    if os.name == "nt":
        subprocess.run(["taskkill", "/PID", str(pid), "/T", "/F"], capture_output=True)
    else:
        os.kill(pid, 9)


def main(argv=None):
    argv = list(sys.argv[1:] if argv is None else argv)
    extra = []
    if "--" in argv:
        i = argv.index("--")
        argv, extra = argv[:i], argv[i + 1:]
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("project")
    parser.add_argument("--method")
    parser.add_argument("--timeout", type=int, default=600, help="seconds, default 600")
    parser.add_argument("--log-dir")
    parser.add_argument("--editor", help="Unity.exe to use instead of the project's version")
    args = parser.parse_args(argv)

    project = os.path.abspath(args.project)
    if not os.path.isdir(os.path.join(project, "Assets")):
        print(f"not a Unity project: {project}", file=sys.stderr)
        return 2
    if os.path.exists(os.path.join(project, "Temp", "UnityLockfile")):
        print(f"refused: {project} is open in an editor (Temp/UnityLockfile exists). "
              "Ask the user, or use unityMCP on the open editor.", file=sys.stderr)
        return 2
    version, editor = editor_for(project)
    if args.editor:
        editor = args.editor
    if not os.path.isfile(editor):
        print(f"refused: editor {version} is not installed ({editor})", file=sys.stderr)
        return 2

    name = os.path.basename(project.rstrip("\\/"))
    log_dir = args.log_dir or os.path.join(os.path.dirname(project), f"{name}-Build", "logs")
    os.makedirs(log_dir, exist_ok=True)
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    label = args.method.rsplit(".", 1)[-1] if args.method else "quit"
    log = os.path.join(log_dir, f"{stamp}-{label}.log")

    cmd = [editor, "-batchmode", "-quit", "-projectPath", project, "-logFile", log]
    if args.method:
        cmd += ["-executeMethod", args.method]
    cmd += extra

    start = time.time()
    proc = subprocess.Popen(cmd)
    try:
        code = proc.wait(timeout=args.timeout)
        timed_out = False
    except subprocess.TimeoutExpired:
        kill_tree(proc.pid)
        code, timed_out = 124, True
    seconds = round(time.time() - start)

    errors = []
    if os.path.exists(log):
        with open(log, encoding="utf-8", errors="ignore") as f:
            errors = sorted({line.strip() for line in f if "error CS" in line})
    print(f"editor: {version} ({editor})")
    print(f"log: {log}")
    print(f"seconds: {seconds}")
    print(f"exit code: {code}" + (" (timed out, process tree killed)" if timed_out else ""))
    print(f"compile errors: {len(errors)}")
    for line in errors[:20]:
        print(f"  {line}")
    return code


if __name__ == "__main__":
    sys.exit(main())
