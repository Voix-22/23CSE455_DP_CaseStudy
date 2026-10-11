# Prompts: Samridhi Singh (Team B6, 23CSE455 Design Patterns case study)


---

## Evolution (CH01, fleet-maintenance scheduling)

### 1. Implement the change from the prompt file
```text
ROLE
You are a senior .NET engineer working on a university design-patterns case study.

CONTEXT
- The full change specification is in part2_prompt_Samridhi_maintenance_scheduling.md. Treat it as the source of truth.
- Codebase: FleetManagment/WanderCar_DotNet/src/FlemanApi (ASP.NET Core 8, EF Core 8, FluentValidation), tests in FlemanApi.Tests (NUnit, Moq, FluentAssertions).
- The baseline is commit dfc61ab8. It has 183 NUnit tests, which are the behaviour-preservation evidence.

TASK
1. Read the specification and every source file it names before writing any code.
2. Create a branch from dfc61ab8.
3. Implement the change. Use the State pattern for the maintenance workflow, so that the service contains no switch or if-chain on Status.
4. Write tests for each state's three operations, the service rules (including the due/overdue boundary day with a fixed clock) and the validators.
5. Build and run the full test suite.
6. Draft the evidence files for Part2/changes/<change>/: requirement.md, pattern_effect.md, refactoring.md and metrics.csv.

CONSTRAINTS
- Inject TimeProvider; never call DateTime.Now in new code.
- Register new services in one commented block in Program.cs, after the existing registrations.
- Do not edit, delete or weaken any existing test.
- Report only results you actually obtained. If a test already fails on the baseline, run the baseline to prove it and say so; do not hide it.
- Do not hand-write the EF Core migration snapshot.

OUTPUT
- A short summary of what was built.
- The real build and test totals (total / passed / failed) for both the baseline and after the change.
- A pattern-role table: role -> class/method -> file.
- The exact `dotnet ef migrations add` command for me to run.

DONE WHEN
- Every acceptance criterion in the specification is covered by a passing test.
- The existing tests give the same result as on the baseline.
```

### 2. List the new pattern types
```text
ROLE
You are a design-pattern reviewer.

CONTEXT
My Part 2 change (fleet-maintenance scheduling) and its pattern_effect.md, which records each pattern instance as preserved / modified / extended / replaced / removed / newly introduced.

TASK
List the pattern instances whose status is "newly introduced" by my change.

CONSTRAINTS
- Include only instances introduced by my change, not extended or preserved ones.
- Base the answer on the recorded pattern effects, not on general knowledge.

OUTPUT
A bulleted list of "Pattern type: instance name". No explanation.
```

### 3. Create the Part 2 LLM log
```text
ROLE
You are documenting LLM 

CONTEXT
- The work is my Part 2 change.
- Its evidence is in Part2/changes/<change>/: requirement.md, pattern_effect.md, test_result.txt and test_result_baseline.txt.

TASK
Create Part2/llm.csv with one row per LLM interaction that produced or analysed the newly introduced pattern(s) of my change.

CONSTRAINTS
- Use exactly these columns, in this order:
  id, date, part, pattern, tool_model, purpose, prompt_file, output_files, llm_reported_result, changes_needed_after_first_run, verified_by, verification_date, verdict, verdict_notes
- Take every number in llm_reported_result from the saved logs, and cross-check test counts against the log before writing them.
- changes_needed_after_first_run must state honestly what was changed and what is still outstanding.
- Set verified_by, verification_date, verdict and verdict_notes to PENDING; never fill in a verdict on your own work.
- Quote fields correctly so the file opens in Excel.

OUTPUT
The CSV file, plus a one-line summary of each row in the chat.

DONE WHEN
- Every row has exactly 14 fields.
- Every number matches a log file.
```

### 4. Pattern-count and pattern-evolution CSVs
```text
ROLE
You are a design-pattern analyst producing structured evidence for a case-study report.

CONTEXT
- Snapshots: the baseline (dfc61ab8) and the code after my maintenance change (State pattern).
- Use the team's Part 1 instance names, as listed in casestudy.tex.

TASK
1. Create Part2/Pattern_count_rows_change1.csv with columns:
   source_version, summary, pattern_name, count
   Include for each snapshot: one row per pattern type with its instance count, and the totals of pattern types and instances. Also add rows for what the change introduced, replaced or removed, and rejected.
2. Create Part2/pattern_evolution_rows_baseline_and_change1.csv with columns:
   source_version, pattern_name, pattern_type, instance_name, participant_roles, source_locations, intent_and_evidence, status_after_change, change_description
   Write one row per instance at the baseline and one per instance after the change.

CONSTRAINTS
- Map every role to a real class or method, with file:line ranges read from the code at that snapshot (use `git show <commit>:<path>` for the baseline).
- status_after_change must be one of: preserved, modified, extended, replaced, removed, newly introduced.
- change_description must say concretely what my change added (files, Program.cs lines), or "Not touched."
- Do not invent participants. If a role is uncertain, say so in intent_and_evidence.
- Save the files in my local Part2 folder, outside the team repository.

OUTPUT
The two CSV files, plus a short summary in the chat of counts and status changes.

DONE WHEN
- The counts in file 1 agree with the rows in file 2.
- Every file:line reference points at the named class.
```

---

## Part 3: Cross-language comparison

### 5. Start Part 3
```text
ROLE
You are a software engineer comparing how one design pattern is expressed in several programming languages.

CONTEXT
- The source instances are the confirmed pattern instances of the WanderCar/Fleman backend (Part 1, plus those introduced in Part 2).
- Use the free toolchains available on this machine.

TASK
1. For each confirmed pattern type, choose one representative verified instance and give the reason for choosing it.
2. Write a common specification: inputs, outputs, rules, error cases (with exact messages) and shared fixtures.
3. Implement that specification in Java, Python, JavaScript and C++, preserving the same pattern roles and behaviour.
4. Build and run every implementation, and save the real output logs.
5. Measure for each implementation:
   - source LOC;
   - maximum function cyclomatic complexity;
   - runtime/compiler version;
   - test command and result;
   - median process time.
   Record them in cross_language.csv.
6. Write the report sections: comparison (participant structure, abstraction, inheritance/composition, language features) and a short, sourced technology discussion.

CONSTRAINTS
- Never report a result, timing or measurement you did not obtain. Mark anything not run as "not run" and give the reason.
- If a compiler or tool is missing, stop and tell me rather than simulating output.
- Use each language's standard library only, unless a test framework is unavoidable; if one is needed, record its version.

OUTPUT
A Part3/ folder containing one subfolder per language, logs/, cross_language.csv and the report.

DONE WHEN
Every number in the report can be traced to a log file or to the CSV.
```

### 6. Check scope and completeness
```text
ROLE
You are a project checker.

CONTEXT
My share of Part 3 is the JavaScript implementation only.

TASK
Compare the contents of Part3/ with the Part 3 requirements: selected instances, common specifications, implementations, actual results, static and dynamic comparison (cross_language.csv), comparison, and technology discussion.

CONSTRAINTS
Check files on disk; do not assume a file exists because it was mentioned earlier.

OUTPUT
A two-column checklist (requirement -> present / missing, with the file path), then the questions you need answered to finish.
```

### 7. Implement every confirmed pattern type in JavaScript
```text
ROLE
You are a design-pattern analyst and a JavaScript engineer.

CONTEXT
- Scope: every confirmed pattern type, implemented in JavaScript only (Node.js, no npm dependencies).
- The source is the WanderCar/Fleman backend, including the State pattern added in Part 2.

TASK
1. Confirm the pattern types. For each candidate, read its participant classes and verify the collaboration in the code, not just the class name.
2. Record the rejected or uncertain candidates with evidence. Examples: dead code, a second implementation that is commented out, or a framework idiom.
3. For each confirmed type, pick one representative instance and implement it in JavaScript, preserving its roles and its observable behaviour (including exact error messages).
4. Write node:test tests for each pattern.
5. Run a mutation check: inject one deliberate bug per pattern and confirm the tests catch it.
6. Produce the test log, the timing log, cross_language.csv and the Part 3 report.

CONSTRAINTS
- Give file:line evidence for every role.
- Report only real results; mark the Java, Python and C++ columns "not run" with the reason.

OUTPUT
- Part3/javascript/ (src, test, fixtures, package.json).
- Part3/logs/, Part3/cross_language.csv and Part3/part3_report.md.

DONE WHEN
- `node --test` passes.
- Every deliberate bug is caught.
- Every confirmed type has a role table with file:line references.
```

### 8. Port Dependency Injection to the other three languages
```text
ROLE
You are a polyglot engineer (Java, Python, C++, JavaScript).

CONTEXT
- The reference implementation is Part3/javascript/src/dependency_injection.mjs, with its test file.
- The specification has 10 cases (D01-D09, D04b), including exact .NET error messages.

TASK
1. Implement the same Dependency Injection container in Java, Python and C++, in Part3/java, Part3/python and Part3/cpp.
2. Port the same 10 test cases to each language.
3. Write a README.md for each language folder, and one for the JavaScript folder, covering:
   - what is modelled, with the C# file:line it comes from;
   - the files;
   - required versions;
   - exact build/run commands for Windows and Linux;
   - expected output;
   - the list of test cases;
   - language-specific notes.
4. Build and run all four test suites and save one log per language.
5. Run the mutation check in every language, and update cross_language.csv and the report.

CONSTRAINTS
- Use the same token names and the same error messages in every language.
- Use each language's idiomatic way of building an object from its dependencies, and explain it in the README.
- Never claim a result you did not get. If a compiler is missing, tell me how you will obtain one before using it.

OUTPUT
- The four language folders, with code, tests and README.
- One log per language and the updated CSV and report.
- A table in the chat: language -> tests passed, LOC, max CC, median time.

DONE WHEN
- 10/10 pass in all four languages.
- Every deliberate bug is caught.
```

### 9. Locate the Java file
```text
TASK
Give the path of the Dependency Injection source file(s) in Part3/java.

OUTPUT
The path or paths only, as clickable links. No explanation.
```

### 10. Explain the Java file names
```text
ROLE
You are a Java instructor.

TASK
Explain why the Java Dependency Injection code is split into ServiceProvider.java, ServiceCollection.java and Lifetime.java rather than being in one file named after the pattern.

CONSTRAINTS
- Cite the Java language rule involved and the .NET names these classes mirror.
- Keep it to a short paragraph and a small table.

OUTPUT
The explanation, followed by one sentence offering to restructure the code if I want.
```

### 11. Merge the Java classes into one file
```text
ROLE
You are a Java engineer refactoring code without changing its behaviour.

TASK
1. Move Lifetime, ServiceCollection and ServiceProvider into a single file, Part3/java/src/main/java/di/DependencyInjection.java, as nested classes of one public class DependencyInjection.
2. Delete the three old files.
3. Update DependencyInjectionTest.java so that only its imports change.
4. Re-run the tests and the mutation check, and update the README, the measurement script, cross_language.csv and the report to match.

CONSTRAINTS
- Behaviour must stay identical: the same 10 tests, with no test logic changed.
- Name the file in the Java convention (PascalCase) and explain why it is not named dependency_injection.java.

OUTPUT
The new file path, the re-run results, and every number that changed.

DONE WHEN
- 10/10 pass.
- All deliberate bugs are still caught.
```

### 12. Add the DI row to the team LLM log
```text
ROLE
You are maintaining a shared team record; preserving teammates' content is critical.

CONTEXT
- File: dp_case/23CSE455_DP_CaseStudy/B6/Part3/llm/llm.csv.
- The existing rows belong to my teammates.

TASK
1. Read the file and copy its column order, ID scheme, wording style and line endings.
2. Append one row describing my Dependency Injection work.

CONSTRAINTS
- Append only; do not modify, reorder or reformat any existing byte. Verify this with a byte comparison afterwards.
- Every result in the row must come from real runs. If the files in the team repository differ from what was tested, or cannot run there, say so in the row.
- Leave the verification fields as PENDING.

OUTPUT
The appended row, the proof that the existing rows are unchanged, and any problem found in the team folder.
```

### 13. Shorten the DI row (shown in chat)
```text
TASK
Shorten my Dependency Injection row in the team llm.csv.

CONSTRAINTS
- Do not edit the file yet.
- Keep each value to one or two lines.
- Keep the key numbers (tests passed, LOC, max CC, median time) and the essential caveat.

OUTPUT
A table in the chat: column title -> short value.
```

### 14. Apply the short version
```text
TASK
Write the shortened Dependency Injection row into dp_case/23CSE455_DP_CaseStudy/B6/Part3/llm/llm.csv.

CONSTRAINTS
- Re-read the file first. If its format has changed (different columns), adapt the row to the current columns rather than the old ones, and tell me.
- Leave every other row byte-for-byte unchanged, and keep the file's line endings.

OUTPUT
The final value under each column, and a confirmation that the other rows are unchanged.
```

### 15. Draft the DI prompt
```text
ROLE
You are a prompt engineer.

CONTEXT
dp_case/23CSE455_DP_CaseStudy/B6/Part3/llm/part3_prompt.txt is the team's model prompt for a Part 3 pattern.

TASK
Draft the equivalent prompt for the Dependency Injection pattern that I should have used.

CONSTRAINTS
Copy the model prompt's structure and conventions, and include:
- an honesty rule (no unreported results; stop if a tool is missing);
- the exact source instance and its C# file:line references;
- the folder layout, with self-contained folders;
- the 10 test cases with their exact expected error messages;
- the required README contents;
- a deliberate-bug check;
- the same measurement method as the other rows;
- the exact CSV columns.

OUTPUT
The prompt in one copyable code block, followed by one line saying where to save it.
```

### 16. Add DI rows to the cross-language CSV
```text
ROLE
You are maintaining a shared team record.

CONTEXT
dp_case/23CSE455_DP_CaseStudy/B6/Part3/llm/pattern_crosslanguage.csv. The existing rows are my teammates'.

TASK
Append four short Dependency Injection rows (Java, Python, JavaScript, C++) under the existing column titles.

CONSTRAINTS
- Append only, and keep the line endings. Verify byte-for-byte that the existing rows are unchanged.
- Take the values from my real measurements.
- Keep language_notes to one or two sentences: how the language builds an object from its dependencies, plus any pitfall.
- If my measurement method differs from the existing rows, tell me in the chat.

OUTPUT
A summary table of the four rows (language, LOC, max CC, result, median ms).
```

---

## Report (`casestudy.tex`)

### 17. Add my work to the report
```text
ROLE
You are a technical writer contributing one member's work to a shared LaTeX report.

CONTEXT
- File: casestudy.tex.
- The existing teammate sections (CH02, Bhavana's Part 3) are the style reference.
- My work: the maintenance change (State pattern), and Dependency Injection in four languages.

TASK
1. Add my Part 2 change as CH01, following the structure of the CH02 record exactly:
   - change and acceptance criteria (as a table with the observed result for each criterion);
   - before and after;
   - implementation and tests;
   - pattern effects (using the team's Part 1 instance names);
   - refactoring;
   - before/after metrics (using the same LOC rule as CH02);
   - interpretation.
2. Add a CH01 row to the "Updated pattern counts" table.


CONSTRAINTS
- Do not change any teammate text. Insert my sections after theirs, each with its own "adding my part below (samridhi):" marker.
- Take every number from my logs and re-measure where the CH02 record uses a metric I have not yet computed.
- Use the macros and column types already defined in the file.
- Escape LaTeX special characters, and check that environments and braces balance.

OUTPUT
The updated file, plus a summary of what was inserted and where.

DONE WHEN
- The file's structure checks out (all environments closed, braces balanced).
- Every figure matches a log or a CSV.
```

---


# CH01 prompt - Fleet-maintenance scheduling (State)

Owner: Samridhi Singh. 

**Files to attach from `WanderCar_DotNet/src/`:** `FlemanApi/Models/Car.cs`, `FlemanApi/Data/FlemanDbContext.cs`, `FlemanApi/Repository/*.cs`, `FlemanApi/Service/StaffService.cs`, `FlemanApi/Service/VehicleService.cs`, `FlemanApi/Controllers/StaffController.cs`, `FlemanApi/Validators/AirportRequestValidator.cs`, `FlemanApi/Exceptions/*.cs`, `FlemanApi/Program.cs`, `FlemanApi.Tests/Services/StaffServiceTests.cs`, `FlemanApi.Tests/Repository/AvailabilityRepositoryTests.cs`, `FlemanApi.Tests/TestHelpers/*.cs`

---

You are helping me implement one change in an existing ASP.NET Core 8 application for a university design-patterns case study.

## Project context (same for every change)

- Course: 23CSE455 Design Patterns, team B8. Case study: "Design Pattern Identification and Evolution in an ASP.NET Core-Based Vehicle Rental Application".
- Application: WanderCar / Fleman, repository https://github.com/Naveen-verma-7697/FleetManagment, baseline commit `dfc61ab8dd6f9bbb2301171457dc03946a249576`.
- Only the .NET backend is in scope: `WanderCar_DotNet/src/FlemanApi` (ASP.NET Core 8, EF Core 8 with Pomelo MySQL, AutoMapper, FluentValidation) and its tests in `WanderCar_DotNet/src/FlemanApi.Tests` (NUnit 3, Moq, FluentAssertions, EF Core InMemory). Do not touch the React frontend or the Java backend.
- Architecture: Controller -> Service (interface + one implementation, registered in `Program.cs`) -> `IGenericRepository<TEntity,long>` / `IAvailabilityRepository` -> `FlemanDbContext`. Entities use plain id columns, no navigation properties. Enums are stored as strings (`HasConversion<string>()` in `FlemanDbContext.OnModelCreating`). Errors are thrown as `ApiException(message, HttpStatusCode)` or `ResourceNotFoundException` and turned into JSON by `ExceptionHandlingMiddleware`.
- Tests build services by hand with `MockRepositoryFactory.Create<TEntity,long>(list, keySelector, keySetter)` and `TestMapperFactory.Create()`.
- The baseline has 183 NUnit tests. They are the behaviour-preservation evidence for this assignment.

## Rules you must follow

1. Work only from the source files I give you. Do not invent classes, methods, files or line numbers. If you need a file I have not supplied, ask for it.
2. Follow the existing code style: file-scoped namespaces, constructor injection, async methods ending in `Async`, DTOs in `DTO/`, validators in `Validators/`, one interface per service.
3. Existing public behaviour must not change unless the requirement says so. Do not delete or weaken an existing test. If a constructor signature changes, the only allowed edit to an existing test is passing the new argument in its `SetUp`.
4. Give me complete file contents for every new file and exact before/after snippets for every edited file. Tell me which file each snippet belongs to.
5. Never state that the code compiles or that tests pass. I will run `dotnet build` and `dotnet test` myself and paste the real output back to you if something fails.
6. Register new services in `Program.cs` in one clearly commented block for this change, placed after the existing service registrations, so the four changes merge cleanly.
7. Use the pattern named in the requirement properly, with every role mapped to a real class. After the code, give me a table: pattern role -> class/interface/method -> file.

## Team workflow

- One shared fork of the repository. Tag `baseline` = `dfc61ab8`. Each change is developed on its own branch cut from `baseline`.
- Changes are integrated in order on the `evolution` branch: CH01 vehicle categories -> CH02 pricing policies -> CH03 maintenance scheduling -> CH04 booking notifications. Before your change is merged, rebase your branch on the previous change's tag and re-run all tests.
- Each change ends as one commit on `evolution`, tagged `ch01`, `ch02`, `ch03`, `ch04`. That commit is the "source version after ChangeN".

## Evidence I must hand in (folder `Part2/changes/ChangeN/`)

Help me produce the text for these; the numbers come from my own machine.

| File | Content |
|---|---|
| `requirement.md` | The requirement and acceptance criteria below, plus any business values chosen |
| `before_commit.txt` / `after_commit.txt` | `git rev-parse` of the commit before and after the change on `evolution` |
| `changed_files.txt` | `git diff --name-status <before> <after>` |
| `diff.patch` | `git diff <before> <after> -- WanderCar_DotNet` |
| `test_result.txt` | Full real output of `dotnet test` after the change |
| `pattern_effect.md` | For every baseline pattern instance: preserved / modified / extended / replaced / removed, and any newly introduced instance, each with a reason and participant file:line |
| `refactoring.md` | Real code smell, refactoring technique, the diff hunk, and the test evidence that behaviour did not change. If no existing code was refactored, say so plainly |
| `metrics.csv` | Source LOC and test totals before and after, same counting rule both times |

LOC rule used by the team (Git Bash, run in `WanderCar_DotNet/src/FlemanApi`):
`find . -name '*.cs' -not -path './obj/*' -not -path './bin/*' -not -path './Migrations/*' | xargs cat | grep -vE '^\s*$' | grep -vE '^\s*//' | wc -l`

Also save this exact prompt, the files supplied, and your raw answer: they are logged in `Part2/llm.csv` and a teammate will verify your claims against the code.


## The change: CH03 - Fleet-maintenance scheduling

**Baseline behaviour.** There is no maintenance feature. `CarStatus` has a value `UNDER_MAINTENANCE`; `AvailabilityRepository.CountCapacityAsync` excludes such cars from capacity and `CountByCarTypeAsync` counts them for the staff dashboard; `StaffService.HandoverVehicleAsync` refuses a car that is not `AVAILABLE`. Nothing in the application ever sets a car to `UNDER_MAINTENANCE` except seed data.

**Requirement.** Staff can schedule maintenance for a car, start it, complete it or cancel it, and can list what is due or overdue. Use the **State** pattern for the schedule's workflow.

**Design to implement.**
- Entity `MaintenanceSchedule` (table `maintenance_schedules`): id, `CarId` (plain id, no navigation property), `MaintenanceType` enum (SERVICE, INSPECTION, REPAIR), `ScheduledDate`, `Status` enum (SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED), `StartedAt`, `CompletedAt`, `Notes`. Enums stored as strings.
- State pattern: `IMaintenanceState` with `Start`, `Complete` and `Cancel` operations; concrete states `ScheduledState`, `InProgressState`, `CompletedState`, `CancelledState`; a context that picks the state object from the entity's `Status`. An operation that is illegal in a state throws `ApiException` with 409 Conflict. There must be no `switch` or `if` chain on `Status` in the service.
- `IMaintenanceService` / `MaintenanceService`:
  - Schedule: the car must exist; the date must not be in the past.
  - Start: allowed only from SCHEDULED and only when the car is `AVAILABLE` (a `BOOKED` car is out with a customer); sets the car to `UNDER_MAINTENANCE` and `IsAvailable = false`.
  - Complete: allowed only from IN_PROGRESS; sets the car back to `AVAILABLE` and `IsAvailable = true`.
  - Cancel: allowed from SCHEDULED; from IN_PROGRESS it also releases the car.
  - Due list: SCHEDULED items whose date is today. Overdue list: SCHEDULED items whose date is before today.
- Inject .NET 8 `TimeProvider` for "now" so due and overdue can be tested with a fixed clock. Do not call `DateTime.Now` in new code.
- `MaintenanceController` at `api/staff/maintenance` with `[Authorize(Roles = "STAFF")]`, request DTOs and a FluentValidation validator.
- `DbSet` in `FlemanDbContext`, enum conversions, and one EF Core migration. Generate the migration only after rebasing on the `ch01` tag, because CH01 also adds a migration and the two would conflict in `FlemanDbContextModelSnapshot.cs`. Tell me the exact `dotnet ef migrations add` command; do not hand-write the snapshot.

**Acceptance criteria.**
1. Starting maintenance removes the car from `CountCapacityAsync` and from the handover picker without any change to that existing code.
2. Every illegal transition (for example Complete from SCHEDULED, Start from COMPLETED) returns 409 and changes nothing.
3. Due and overdue lists are correct on the boundary day with a fixed clock.
4. All existing tests pass unchanged.
5. New tests cover each state's three operations, the service rules above, and the validator.

**Refactoring to document.** This change is additive. If you find a real smell in the code you touch, name it and fix it with evidence; otherwise `refactoring.md` says that no existing code was refactored.

**Pattern effects to expect.** Newly introduced: State, "Maintenance Schedule Workflow". The Generic Entity Repository and Dependency Injection instances are extended with new clients and registrations. The Fleet Availability Query Repository is preserved and now has real data to exclude.
