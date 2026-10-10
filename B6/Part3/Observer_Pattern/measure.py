# Compiles and runs the four test runners, saves each real output as
# <Language>/test_result.txt, the raw timings as <Language>/timings.txt, and
# prints source LOC of each snippet. Standard library only.
#
# Usage (from this folder):  python measure.py
# The C++ compiler is taken from the CXX environment variable (default g++),
# for example:  CXX="python -m ziglang c++" python measure.py
import os
import shlex
import statistics
import subprocess
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
WARMUP_RUNS = 1
MEASURED_RUNS = 5
CXX = shlex.split(os.environ.get("CXX", "g++"), posix=True)

LANGUAGES = [
    {
        "name": "Java",
        "snippet": "BookingEventPublisher.java",
        "version": ["javac", "-version"],
        "compile": ["javac", "BookingEventPublisher.java", "TestRunner.java"],
        "run": ["java", "-cp", ".", "TestRunner"],
    },
    {
        "name": "Python",
        "snippet": "booking_observer.py",
        "version": ["python", "--version"],
        "compile": None,
        "run": ["python", "test_runner.py"],
    },
    {
        "name": "JavaScript",
        "snippet": "bookingObserver.js",
        "version": ["node", "--version"],
        "compile": None,
        "run": ["node", "testRunner.js"],
    },
    {
        "name": "Cpp",
        "snippet": "booking_observer.hpp",
        "version": CXX + ["--version"],
        "compile": CXX + ["-std=c++17", "-O2", "-Wall", "-Wextra", "-o", "test_runner.exe", "test_runner.cpp"],
        "run": ["test_runner.exe"],
    },
]


def source_loc(path):
    # Non-blank lines that are not comment-only lines (// or #, but not C++ #include / #pragma).
    count = 0
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("//"):
            continue
        if path.suffix == ".py" and stripped.startswith("#"):
            continue
        count += 1
    return count


def run(command, cwd):
    if command[0].endswith(".exe") and not Path(command[0]).is_absolute():
        command = [str(cwd / command[0])] + command[1:]
    return subprocess.run(command, cwd=cwd, capture_output=True, text=True)


def timed_run(command, cwd):
    start = time.perf_counter()
    result = run(command, cwd)
    return (time.perf_counter() - start) * 1000.0, result


def main():
    failed = False
    for language in LANGUAGES:
        cwd = HERE / language["name"]
        version = run(language["version"], cwd)
        version_text = (version.stdout + version.stderr).strip().splitlines()[0]

        compile_output = ""
        if language["compile"]:
            compiled = run(language["compile"], cwd)
            if compiled.returncode != 0:
                print(f"{language['name']}: COMPILE FAILED (exit code {compiled.returncode})\n{compiled.stdout}{compiled.stderr}")
                failed = True
                continue
            compile_output = (compiled.stdout + compiled.stderr).strip()

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
            f"compile: {' '.join(language['compile']) if language['compile'] else '(none)'}",
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
        print(f"compiler output: {compile_output or '(none)'}")
        print(f"source_loc({language['snippet']}): {source_loc(cwd / language['snippet'])}")
        print(f"exit code: {result.returncode}")
        print(result.stdout.strip())
        print(f"median_process_ms: {median:.3f}   raw: {[round(t, 3) for t in timings]}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
