"""Run a Unity project in batchmode with the project's own editor.

Usage:
  python unity_batch.py <project> [--method Namespace.Class.Method] [--timeout 600] [--log-dir DIR] [-- extra Unity args]

- The editor comes from ProjectSettings/ProjectVersion.txt (C:/Program Files/Unity/Hub/Editor/<version>);
  --editor overrides it.
- Refuses to start when the project is already open in an editor: an open editor holds
  Temp/UnityLockfile locked. A lockfile nobody holds is a leftover (Unity leaves it when batchmode
  exits on compile errors); the runner deletes it, prints one line saying so, and continues.
- Writes the log to <project's parent>/<project name>-Build/logs/ unless --log-dir is given.
- Kills the Unity process tree when the timeout expires.
- Prints a summary (editor, seconds, exit code, compile errors) and exits with Unity's exit code;
  2 means it refused to start, 124 means it timed out.

Test runs: when the extra Unity args contain -runTests, the runner leaves out -quit (Unity would
exit before the tests run and write no results) and, unless -testResults is given, writes the
results to <log dir>/<stamp>-tests.xml. It then exits with its own code instead of Unity's:
  0 = every test passed, 1 = one or more tests failed,
  3 = Unity error (a run error, compile errors, no result file, or no test ran at all);
2 and 124 keep their meaning.
"""
import argparse
import datetime
import os
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

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


def remove_stale_lockfile(path):
    """Delete the lockfile unless an editor holds it. Return None when deleted, else why not."""
    try:
        if os.name != "nt":
            import fcntl
            with open(path, "rb") as f:
                fcntl.flock(f, fcntl.LOCK_EX | fcntl.LOCK_NB)
        # On Windows the editor opens the file without sharing, so deleting it fails while held.
        os.remove(path)
        return None
    except BlockingIOError:
        return "held"
    except PermissionError as e:
        return "held" if getattr(e, "winerror", None) == 32 else str(e)
    except OSError as e:
        return str(e)


def arg_value(args, name):
    lowered = [a.lower() for a in args]
    if name.lower() in lowered:
        i = lowered.index(name.lower())
        if i + 1 < len(args):
            return args[i + 1]
    return None


def read_results(path):
    """Return (totals, failed test names) from an NUnit 3 result file, or None if unreadable."""
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError):
        return None
    totals = {k: int(root.get(k, 0)) for k in ("total", "passed", "failed", "inconclusive", "skipped")}
    failed = [c.get("fullname") for c in root.iter("test-case") if c.get("result") == "Failed"]
    return totals, failed


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
    lockfile = os.path.join(project, "Temp", "UnityLockfile")
    if os.path.exists(lockfile):
        why = remove_stale_lockfile(lockfile)
        if why == "held":
            print(f"refused: {project} is open in an editor (Temp/UnityLockfile is held). "
                  "Ask the user, or use unityMCP on the open editor.", file=sys.stderr)
            return 2
        if why:
            print(f"refused: cannot remove Temp/UnityLockfile in {project}: {why}", file=sys.stderr)
            return 2
        print(f"removed stale Temp/UnityLockfile (no editor holds it): {lockfile}", flush=True)
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
    run_tests = "-runtests" in (a.lower() for a in extra)
    label = args.method.rsplit(".", 1)[-1] if args.method else ("tests" if run_tests else "quit")
    log = os.path.join(log_dir, f"{stamp}-{label}.log")

    cmd = [editor, "-batchmode"] + ([] if run_tests else ["-quit"]) + ["-projectPath", project, "-logFile", log]
    if args.method:
        cmd += ["-executeMethod", args.method]
    cmd += extra
    results = None
    if run_tests:
        results = arg_value(extra, "-testResults")
        if results:
            results = os.path.abspath(results)
        else:
            results = os.path.join(log_dir, f"{stamp}-tests.xml")
            cmd += ["-testResults", results]

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
    if not run_tests or timed_out:
        return code

    parsed = read_results(results) if os.path.exists(results) else None
    print(f"test results: {results}" + ("" if parsed else " (missing or unreadable)"))
    if parsed is None:
        return 3
    totals, failed = parsed
    print("tests: " + ", ".join(f"{k} {v}" for k, v in totals.items()))
    for name in failed[:20]:
        print(f"  failed: {name}")
    if errors or code not in (0, 2) or totals["total"] == 0:
        return 3
    return 1 if code == 2 or totals["failed"] else 0


if __name__ == "__main__":
    sys.exit(main())
