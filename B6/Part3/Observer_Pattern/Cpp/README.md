# Observer in C++

This folder is the C++17 port of the **Observer** pattern instance **"Booking Lifecycle Notification"**, introduced by CH04 in the WanderCar / Fleman .NET backend (`FlemanApi/Service/Notifications/`, commit `9be38a6f`). It implements [`../common_spec.md`](../common_spec.md) and nothing else.

## Files

```
Cpp/
├── booking_observer.hpp   the pattern, header-only, namespace booking (snippet measured for LOC)
├── test_runner.cpp        the 8 cases of common_spec.md, no framework
├── test_result.txt        real output of the logged run
└── timings.txt            raw timings from ../measure.py
```

## Participants

| Role | C++ | C# original |
|---|---|---|
| Subject | abstract class `BookingEventSubject` (pure virtual functions, virtual destructor) | `IBookingEventPublisher` |
| ConcreteSubject | `BookingEventPublisher`, which holds `std::vector<std::shared_ptr<BookingEventObserver>>` | `BookingEventPublisher` |
| Observer | abstract class `BookingEventObserver` | `IBookingEventObserver` |
| ConcreteObserver | `EmailBookingObserver`, `AuditLogBookingObserver` | the same names |
| Event | `struct BookingEvent`, `enum class BookingEventKind` | `record BookingEvent`, `enum BookingEventKind` |

## Tool, working folder and commands

- Tool: **clang 21.1.0**, provided by `zig 0.16.0` (`pip install ziglang`), because this Windows machine has no g++ or MSVC. This is the same set-up as the Dependency Injection C++ port. Any C++17 compiler works.
- Working folder: `Part3/Observer_Pattern/Cpp`

```bash
# zig's bundled clang, as used for the logged run
pip install ziglang
python -m ziglang c++ -std=c++17 -O2 -Wall -Wextra -o test_runner.exe test_runner.cpp
./test_runner.exe

# g++ / clang
g++ -std=c++17 -O2 -Wall -Wextra -o test_runner test_runner.cpp && ./test_runner

# MSVC (Developer Command Prompt)
cl /std:c++17 /EHsc /O2 /W4 test_runner.cpp && test_runner.exe
```

**Expected result:** eight `PASS` lines, then `PASS 8/8`, with exit code 0, and no compiler warnings from the project files. The real output is in [`test_result.txt`](test_result.txt).

> **About the first zig build:** zig compiles its bundled libc++ the first time. That first build printed 3,687 warnings, all from libc++'s own headers and none from `booking_observer.hpp` or `test_runner.cpp`. The logged run (`../measure_output.txt`) is the second build, with a warm cache, and it printed no compiler output at all.

## C++-specific notes

- **Ownership is explicit.** Observers are held as `std::shared_ptr`, so the subject keeps a subscribed observer alive. This is the closest match to the garbage-collected references in C#.
  - The alternative is `std::weak_ptr`, where the subject does not keep observers alive. It would avoid the classic "lapsed listener" leak, but it needs a `lock()` and an expired-entry clean-up in `publish`.
- **The observers keep references to the outbox and audit log** (`std::vector<...>&`). The caller must keep those containers alive for as long as the observers exist.
- **Identity:** `std::find` and `std::remove` compare the `shared_ptr`s, which means comparing the pointed-to addresses.
- **Exceptions:** `publish` catches `const std::exception&` and records `what()`. An exception type that does not derive from `std::exception` would escape. The C# version catches every `Exception`.
- **`enum class`:** it needs its own `to_string`, because C++ has no built-in enum-to-string conversion. Java, Python and C# all have one.
- **Build outputs:** `test_runner.exe` and `test_runner.pdb`. `../.gitignore` excludes them from the upload.
