# Compiles and runs the four test runners, saves each real output as
# <language>/test_result.txt, the raw timings as <language>/timings.txt, and
# prints source LOC of each snippet. Standard library only.
#
# Usage (from this folder):  python measure.py
import os
import statistics
import subprocess
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
WARMUP_RUNS = 1
MEASURED_RUNS = 5

LANGUAGES = [
    {
        "name": "java",
        "snippet": "VehicleCategoryRulesFactory.java",
        "version": ["javac", "-version"],
        "compile": ["javac", "VehicleCategoryRulesFactory.java", "TestRunner.java"],
        "run": ["java", "-cp", ".", "TestRunner"],
    },
    {
        "name": "python",
        "snippet": "vehicle_category_rules.py",
        "version": ["python", "--version"],
        "compile": None,
        "run": ["python", "test_runner.py"],
    },
    {
        "name": "javascript",
        "snippet": "vehicleCategoryRules.js",
        "version": ["node", "--version"],
        "compile": None,
        "run": ["node", "testRunner.js"],
    },
    {
        "name": "cpp",
        "snippet": "vehicle_category_rules.hpp",
        "version": ["g++", "--version"],
        "compile": ["g++", "-std=c++17", "-O2", "-Wall", "-Wextra", "-static", "-o", "test_runner.exe", "test_runner.cpp"],
        "run": [str(Path("test_runner.exe"))],
        # On the measurement machine another program's bin folder (PostgreSQL)
        # is earlier on PATH and shadows DLLs the compiler needs, so the
        # compiler's own folder is put first for the g++ processes only.
        "path_first": "C:/msys64/ucrt64/bin",
    },
]


def source_loc(path):
    # Non-blank lines that are not comment-only lines (// or #).
    count = 0
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped and not stripped.startswith(("//", "#!")) and not (
            stripped.startswith("#") and path.suffix == ".py"
        ):
            count += 1
    return count


def run(command, cwd, path_first=None):
    if command[0].endswith(".exe"):
        command = [str(cwd / command[0])] + command[1:]
    env = None
    if path_first and Path(path_first).is_dir():
        env = dict(os.environ, PATH=str(Path(path_first)) + os.pathsep + os.environ["PATH"])
    return subprocess.run(command, cwd=cwd, capture_output=True, text=True, env=env)


def timed_run(command, cwd):
    start = time.perf_counter()
    result = run(command, cwd)
    return (time.perf_counter() - start) * 1000.0, result


def main():
    failed = False
    for language in LANGUAGES:
        cwd = HERE / language["name"]
        version = run(language["version"], cwd, language.get("path_first"))
        version_text = (version.stdout + version.stderr).strip().splitlines()[0]

        if language["compile"]:
            compiled = run(language["compile"], cwd, language.get("path_first"))
            if compiled.returncode != 0:
                print(f"{language['name']}: COMPILE FAILED (exit code {compiled.returncode})\n{compiled.stdout}{compiled.stderr}")
                failed = True
                continue
            compile_output = (compiled.stdout + compiled.stderr).strip()
        else:
            compile_output = ""

        # The run whose output is saved as the test result.
        result = run(language["run"], cwd)
        (cwd / "test_result.txt").write_text(result.stdout + result.stderr, encoding="utf-8")
        failed = failed or result.returncode != 0

        for _ in range(WARMUP_RUNS):
            timed_run(language["run"], cwd)
        timings = []
        exit_codes = []
        for _ in range(MEASURED_RUNS):
            elapsed, timed = timed_run(language["run"], cwd)
            timings.append(elapsed)
            exit_codes.append(timed.returncode)
        median = statistics.median(timings)

        lines = [
            f"language: {language['name']}",
            f"tool: {version_text}",
            f"command: {' '.join(language['run'])}",
            f"working folder: {cwd.relative_to(HERE.parent.parent).as_posix()}",
            "method: wall-clock time of the whole test runner process, measured with",
            "        time.perf_counter() around subprocess.run() in measure.py.",
            "        Includes process start-up (JVM / interpreter / runtime load) and",
            "        the cost of creating the child process and capturing its output.",
            f"        Compiled first, {WARMUP_RUNS} warm-up run discarded, {MEASURED_RUNS} measured runs.",
            "",
        ]
        lines += [f"run {i}: {t:.3f} ms (exit code {c})" for i, (t, c) in enumerate(zip(timings, exit_codes), 1)]
        lines += ["", f"median: {median:.3f} ms", f"min: {min(timings):.3f} ms", f"max: {max(timings):.3f} ms"]
        (cwd / "timings.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")

        print(f"== {language['name']}")
        print(f"tool: {version_text}")
        if compile_output:
            print(f"compiler output: {compile_output}")
        print(f"source_loc({language['snippet']}): {source_loc(cwd / language['snippet'])}")
        print(f"exit code: {result.returncode}")
        print(result.stdout.strip())
        print(f"median_process_ms: {median:.3f}   raw: {[round(t, 3) for t in timings]}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
