# 23CSE455 Design Patterns — Case Study, Team B6

**Design Pattern Identification and Evolution in an ASP.NET Core-Based Vehicle Rental Application**

| Member | Roll number | Part 2 change |
|---|---|---|
| Samridhi Singh | AM.SC.U4CSE23251 | Change1 — fleet-maintenance scheduling (State) |
| Bhavana P H | AM.SC.U4CSE23211 | Change2 — new vehicle categories (Factory Method) |
| Neema Vinod | AM.SC.U4CSE23238 | Change3 — interchangeable pricing policies (Strategy) |
| Vani Sugovind S R | AM.SC.U4CSE23266 | Change4 — booking lifecycle notifications (Observer) |

## 1. Application and source

| | |
|---|---|
| Application | WanderCar / Fleman vehicle rental and fleet management system |
| Source URL | https://github.com/Naveen-verma-7697/FleetManagment (branch `main`) |
| Part analysed | The .NET backend only: `WanderCar_DotNet/src/FlemanApi` and its tests `WanderCar_DotNet/src/FlemanApi.Tests`. The React frontend and the Java backend are out of scope. |
| Stack | ASP.NET Core 8, EF Core 8 with Pomelo MySQL, AutoMapper, FluentValidation, MailKit, QuestPDF; tests with NUnit 3, Moq, FluentAssertions, EF Core InMemory |

## 2. Repository layout

```
README.md                         this file
B6/
├── README.md                     team members
├── data/
│   ├── code/WanderCar_DotNet/    original code (dfc61ab8)
│   └── test_result/              build and test evidence for the original code
├── Part1/                        pattern discovery in the original code
│   ├── part1_patterns.csv        9 confirmed pattern instances
│   ├── part1_pattern_count.csv
│   ├── baseline_commit.txt, inspected_modules.txt
│   ├── Part1_prompt.txt, llm.csv, llm/ (LLM input, raw output, verification template)
│   └── pattern_architecture/     6 diagrams (.puml, .dot, .png)
├── Part2/                        pattern evolution
│   ├── code/WanderCar_DotNet/    evolved application source (all four changes)
│   ├── code/test_result.txt      build and test log of the evolved code
│   ├── changes/Change1 … Change4 requirement, pattern effects, refactoring, metrics, test results
│   ├── changes/integration/      how the four changes were combined; one patch per step
│   ├── diagrams/                 9 updated UML diagrams (.puml, .png)
│   ├── pattern_evaluation.csv    pattern evolution rows (original code and after each change)
│   ├── Pattern_count.csv         pattern counts per version
│   ├── part2_prompt.txt, llm.csv
└── Part3/                        the same pattern in Java, Python, JavaScript and C++
    ├── Factory_Method/, Observer_Pattern/, Dependency_Injection_Pattern/
    ├── repository_pattern/, strategy_pattern/
    └── llm/ (pattern_crosslanguage.csv, part3_prompt.txt, llm.csv)
```

## 3. Environment

Used for the original-code evidence in `B6/data/test_result` and for the evolved code in `B6/Part2/code` (both on 2026-10-11):

| | |
|---|---|
| OS | Windows 11 Pro 10.0.26300 |
| .NET SDK / runtime | 8.0.425 / 8.0.31 |
| Test libraries | NUnit 3.14.0, NUnit3TestAdapter 4.5.0, FluentAssertions 6.12.1, Moq 4.20.72, Microsoft.EntityFrameworkCore.InMemory 8.0.2 |
| Database for tests | None. Tests use mocks and the EF Core in-memory provider. |
| Database to run the application | MySQL 8 (connection string in `FlemanApi/appsettings.json`) |
| Diagrams | PlantUML 1.2024.8 with the built-in Smetana layout (`!pragma layout smetana`), so Graphviz is not needed |

The Part 3 snippets were run by different members on different machines. The exact tool version of every run is in `B6/Part3/llm/pattern_crosslanguage.csv` (column `runtime_compiler`) and in each language README. Versions used: JDK 17, 24 and 26; CPython 3.13 and 3.14; Node.js 22 and 24; g++ 15.2 and clang 21.1.

## 4. Run and test commands

### Original code

```powershell
cd B6\data\code\WanderCar_DotNet
dotnet test src\FlemanApi.Tests\FlemanApi.Tests.csproj
```

Expected: 183 total, 182 passed, 1 failed. The failing test, `StaffServiceTests.ProcessReturnAsync_CreatesInvoiceHeaderAndDetails`, depends on the date it is run: the fixture hands the car over on 2026-08-01 and `StaffService` bills up to `DateTime.Now`. Details: `B6/data/test_result/README.md`.

### Evolved code (all four changes)

```powershell
cd B6\Part2\code\WanderCar_DotNet
dotnet test src\FlemanApi.Tests\FlemanApi.Tests.csproj
```

Result of the run on 2026-10-11: build 0 warnings, 0 errors; **297 total, 297 passed, 0 failed**. Log: `B6/Part2/code/test_result.txt`.

Notes for both:

- Name the test project, not the solution. `DotnetBackend.slnx` needs SDK 9.0.200 or later and fails on SDK 8 with `MSB4068`.
- If the machine has no NuGet source configured, add `--source https://api.nuget.org/v3/index.json` to a first `dotnet restore`.
- Add `--artifacts-path <temp folder>` to keep `bin/` and `obj/` out of the repository. The stored logs were made this way.

To run the application itself (not needed for the tests): start MySQL 8, set `ConnectionStrings:Default` in `src/FlemanApi/appsettings.json`, then `dotnet run --project src/FlemanApi`. The application applies the EF Core migrations and seeds data at start-up. Swagger is at `/swagger` in the Development environment.

### Part 3 snippets

Each language folder has a README with its own compile and run commands. `Factory_Method/`, `Observer_Pattern/` and `Dependency_Injection_Pattern/` also contain the test runners and the real outputs (`test_result.txt`). For the two small folders `repository_pattern/` and `strategy_pattern/`, the commands are the same in every language folder:

| Language | Folder | Command |
|---|---|---|
| Java | `java` | `javac Main.java` then `java Main` |
| Python | `python` | `python main.py` |
| JavaScript | `javascript` | `node main.js` |
| C++ | `cpp` | `g++ -std=c++17 main.cpp -o main` then `./main` |

Checked on 2026-10-11 with JDK 17.0.16, CPython 3.14.3, Node.js 24.14.1 and g++ 15.2.0: all four `repository_pattern` programs print `Repository CRUD: PASS`, and all four `strategy_pattern` programs end with `Weekend: 360.00`.

## 5. Part 1 — patterns in the original code

Inspected modules: the eight functional areas of the handout (vehicle management, customer management, reservation/booking, pricing, payment/billing, maintenance, notification, authentication) in `WanderCar_DotNet/src/FlemanApi`. That is the folders `Controllers`, `Service`, `Repository`, `Data`, `Models`, `DTO`, `Middleware`, `Security`, `Util`, `Validators`, `AutoMapper` and `Program.cs`. The file-by-file list is `B6/Part1/inspected_modules.txt`.

Confirmed instances (`B6/Part1/part1_patterns.csv`): 9 pattern types, 9 instances.

| Pattern | Instance |
|---|---|
| Repository | Generic Repository |
| Dependency Injection / IoC | ASP.NET Core DI Composition |
| Service Layer | Booking Service Layer |
| Middleware Pipeline / Chain of Responsibility | ASP.NET Core Request Middleware Chain |
| Producer–Consumer | Email Background Work Queue |
| Adapter | Java Microservice Adapter |
| Proxy | Legacy API Proxy |
| Strangler Fig | Legacy Java Integration |
| DTO | API Request/Response DTO Boundary |

The LLM output listed 9 confirmed, 3 uncertain and 9 rejected candidates (`B6/Part1/llm.csv`). Only the confirmed ones are in the catalogue.

## 6. Part 2 — the four changes

| Folder | Requirement | Pattern introduced | Tests added | Refactoring of existing code |
|---|---|---|---|---|
| `Change1` | Staff schedule, start, complete and cancel maintenance for a car; due and overdue lists | State — "Maintenance Schedule Workflow" | 50 | None |
| `Change2` | Vehicle categories STANDARD, ELECTRIC, COMMERCIAL with their own deposit and rate rules | Factory Method (parameterized) — "Vehicle Category Rules Creation" | 24 | None |
| `Change3` | Standard, weekend and seasonal pricing, chosen by date | Strategy — "Rental Pricing Policy" | 22 | Booking and return invoice call a pricing policy instead of the static calculator; `StaffService` gets an injected clock |
| `Change4` | Notify the customer and write an audit entry when a booking is created, modified or cancelled | Observer — "Booking Lifecycle Notification" | 18 | The confirmation e-mail moved out of `BookingService.CreateBookingAsync` (Extract Class, Move Method) |

The documents inside the folders use the branch labels of their authors: `CH03` in `Change1`, `CH02` in both `Change2` and `Change3`, `CH04` in `Change4`. The same labels appear in code comments in `Part2/code`.

### Versions and metrics

The changes are applied in folder order. Every figure in this table was measured on 2026-10-11 on the cumulative code of that step.

| Version | Source LOC | Source files | Test LOC | Tests total | Passed | Failed |
|---|---:|---:|---:|---:|---:|---:|
| Original code | 3,599 | 107 | 2,136 | 183 | 182 | 1 |
| Code after Change1 | 3,956 | 122 | 2,758 | 233 | 232 | 1 |
| Code after Change2 | 4,151 | 133 | 3,005 | 257 | 256 | 1 |
| Code after Change3 | 4,360 | 141 | 3,225 | 279 | 279 | 0 |
| Code after Change4 (`Part2/code`) | 4,500 | 147 | 3,603 | 297 | 297 | 0 |

Original code to final code: 67 files changed (58 added, 9 modified), 4,545 lines added, 52 removed, including tests and EF Core migrations.

The one failing test disappears at Change3 because that change makes `StaffService` take its time from an injected `TimeProvider`, and the test now uses a fixed clock. It is the date-dependent test described in section 4, not a behaviour that any change broke or repaired by accident.

### Pattern counts (`B6/Part2/Pattern_count.csv`)

| Version | Pattern types | Pattern instances | Added |
|---|---:|---:|---|
| Original code | 9 | 9 | — |
| Code after Change1 | 10 | 10 | State |
| Code after Change2 | 11 | 11 | Factory Method |
| Code after Change3 | 12 | 12 | Strategy |
| Code after Change4 | 13 | 13 | Observer |

No instance is removed or replaced. `B6/Part2/pattern_evaluation.csv` has one row per instance per version (55 rows): the nine Part 1 rows unchanged, then the same nine instance names after every change with updated participants, line ranges, evidence, status and change description, plus the new instance of each change.

### Diagrams (`B6/Part2/diagrams`)

| File | Shows |
|---|---|
| `01_architecture_after_changes` | Controllers, services, repositories and the four new collaborations, coloured by change |
| `02_change1_state_class`, `03_change1_state_machine` | State roles; the transitions and what each does to the car |
| `04_change2_factory_method_class` | Factory Method roles |
| `05_change3_strategy_class` | Strategy roles, the policy selector and the two calling services |
| `06_change4_observer_class`, `07_change4_observer_sequence` | Observer roles; creating a booking from request to queued e-mail |
| `08_repository_updated`, `09_email_producer_consumer_updated` | The two Part 1 diagrams whose participants changed |

Each has a `.puml` source and a `.png`. To render again: `java -jar plantuml.jar -tpng *.puml`.

## 7. Part 3 — cross-language comparison

Results are in `B6/Part3/llm/pattern_crosslanguage.csv`. Rows exist for three patterns:

| Pattern (instance) | Java | Python | JavaScript | C++ |
|---|---|---|---|---|
| Factory Method (Vehicle Category Rules Creation) — LOC / max CC / tests / median ms | 65 / 4 / 7 of 7 / 109.8 | 51 / 2 / 7 of 7 / 56.6 | 77 / 4 / 7 of 7 / 70.1 | 71 / 4 / 7 of 7 / 14.6 |
| Observer (Booking Lifecycle Notification) | 71 / 9 / 8 of 8 / 238.7 | 69 / 3 / 8 of 8 / 65.5 | 63 / 3 / 8 of 8 / 71.0 | 102 / 6 / 8 of 8 / 16.9 |
| Dependency Injection (Service Registration and Constructor Injection) | 110 / 7 / 10 of 10 / 852.8 | 81 / 8 / 10 of 10 / 132.0 | 57 / 8 / 10 of 10 / 153.8 | 122 / 7 / 10 of 10 / 9.9 |

`repository_pattern/` and `strategy_pattern/` contain a small runnable program per language but have no rows in `pattern_crosslanguage.csv` and no stored measurements.

## 8. Measurement rules

### C# application (Parts 1 and 2)

- **Source LOC.** Files `FlemanApi/**/*.cs`, excluding `Migrations`, `bin` and `obj`. A line counts if it is not blank and does not start with `//`. A line with code followed by a comment counts once. Test LOC uses the same rule on `FlemanApi.Tests/**/*.cs`.
- **Tests.** `dotnet test` on the test project, .NET SDK 8.0.425. Totals are copied from the console summary.
- **Complexity, coverage, coupling.** Not measured for the C# application at any version. No value is reported.
- **Timing.** Test duration is printed by `dotnet test` but is not used as a metric: it is one run on one machine and mostly build and start-up time.

### Cross-language snippets (Part 3)

- **Source LOC.** The snippet file only; test runner, README and measurement script are excluded. A line counts if it is not blank and is not a comment-only line (`//` in Java, JavaScript and C++; `#` in Python). C++ preprocessor lines count as code.
- **Maximum cyclomatic complexity of a function.** For each function: 1, plus one for each `if`, loop, `case`, `catch`/`except`, `&&`/`and` and `||`/`or`. `else` and `default` are not counted. The reported value is the largest of any function in the snippet, counted by hand.
- **Elapsed time.** Wall-clock time of the whole test-runner process, so it **includes process start-up** (JVM, Python interpreter, Node.js). Compile first, discard one warm-up run, then five runs; the median is reported. Raw values are in each folder's `timings.txt`. Tool: `time.perf_counter()` around `subprocess.run()` in `measure.py`.
- **Test result.** Each language runs the same cases from the pattern's `common_spec.md`. `test_result.txt` is the unedited output of a real run.

## 9. LLM use

Every LLM-assisted step is recorded in an `llm.csv` (`B6/Part1`, `B6/Part2`, `B6/Part3/llm`) with model, date, prompt file, input, output, evaluation and verification file. Prompts: `B6/Part1/Part1_prompt.txt`, `B6/Part2/part2_prompt.txt`, `B6/Part2/changes/Change4/prompt.md`, `B6/Part3/llm/part3_prompt.txt`.

## 10. Limitations

1. **Change1 code is a reconstruction.** `Part2/changes/Change1` has documents and test logs but no diff, and the original source is not in this repository. The maintenance code in `Part2/code` was written again from `requirement.md`, `pattern_effect.md` and the 50 test names in the original test log. Names, routes, rules and test names follow those documents; method bodies and line numbers may differ. The line numbers in `Change1/pattern_effect.md` refer to the original source, not to `Part2/code`.
2. **The evolved application was not run against MySQL.** It builds, all tests pass, and start-up gets past dependency-injection validation, then stops at the database connection because no MySQL server was available. No HTTP request and no migration was executed on the evolved code. The `AddMaintenanceSchedules` migration was written by hand and only compared with the EF Core model (no difference found).
3. **The four changes were developed separately.** Each change's own documents (metrics, line numbers, test totals) describe that change applied alone to the original code. Combined figures are in section 6 and in `Part2/changes/integration/README.md`, which also lists the edits needed to make the changes work together.
4. **Change3 evidence is partly second-hand.** `Change3/test_result.txt` is a transcription of terminal output, and no commit hash was recorded. The cumulative run in this repository (279 of 279 after Change3) is a real run.
5. **Commit hashes of Change2 and Change4 are local.** They cannot be checked against the public source repository; the diffs in the change folders are the evidence.
6. **Pattern catalogue.** Counts follow the nine instances confirmed in Part 1. Collaborations that some change documents mention but Part 1 did not confirm (Command, SMTP Adapter, Java Invoice PDF Adapter, Fleet Availability Query Repository) are not counted. The Factory Method instance is the parameterized variant with one concrete creator, which some texts call Simple Factory. Role mapping and status words in `pattern_evaluation.csv` were produced with an LLM and need a human check.
7. **Metrics.** No complexity, coverage or coupling figure exists for the C# application. Part 3 timings include process start-up, come from different machines for different patterns, and are not comparable across patterns.
8. **Prompt record is incomplete.** `Part2/part2_prompt.txt` has the exact text only for the integration prompt and points to `Change4/prompt.md`. The prompts for Change1, Change2 and Change3 are not in the repository and are marked there for their owners to add.
9. **Files that are referenced but missing.** `Part1/llm.csv` names `llm/human_verification.txt` (the folder has `human_verification_template.txt`); `Part2/llm.csv` names `Part2/llm_output_ch04.md`; `Part3/llm/llm.csv` names `Part3/llm/part3_observer_prompt.txt`.
10. **Generated files in Part 3.** `.class` files, `__pycache__` and `test_runner.exe` are build outputs, not source.
