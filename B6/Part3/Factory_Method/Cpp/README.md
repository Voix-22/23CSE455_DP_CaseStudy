# C++ - Factory Method (parameterized): Vehicle Category Rules Creation

| | |
|---|---|
| Tool | `g++ (Rev13, Built by MSYS2 project) 15.2.0`, target `x86_64-w64-mingw32`, C++17 |
| Working folder | `Part3/FactoryMethod_VehicleCategoryRules/cpp` |
| Snippet | `vehicle_category_rules.hpp` (header-only) |
| Test runner | `test_runner.cpp` |
| Libraries | C++ standard library only |

## Compile and run

```
g++ -std=c++17 -O2 -Wall -Wextra -static -o test_runner.exe test_runner.cpp
test_runner.exe
```

(`./test_runner.exe` in PowerShell or a POSIX shell.) The compile produced no warnings. Exit code 0 means all seven cases passed. The recorded output is `test_result.txt` (PASS 7/7); raw timings are in `timings.txt`.

`-static` links the GCC runtime into the executable so that it does not depend on MinGW DLLs being found at run time.

### Note for the machine the results were produced on

On that machine `g++` exited with code 1 and printed nothing, even for a hello-world program. The cause was not the code: `C:\Program Files\PostgreSQL\18\bin` is earlier on `PATH` than `C:\msys64\ucrt64\bin` and contains its own `libzstd.dll`, `zlib1.dll`, `libwinpthread-1.dll` and `libiconv-2.dll`, which the compiler back end (`cc1plus.exe`) loaded instead of its own and failed with `0xC0000139` (entry point not found). The compile was therefore run with the compiler's folder first on `PATH` for that one process:

```
$env:PATH = "C:\msys64\ucrt64\bin;$env:PATH"     # PowerShell, current window only
```

`measure.py` does the same for the `g++` processes. Nothing was changed in the system settings.

## How the pattern is expressed

- Abstraction: an abstract class with pure virtual member functions and a virtual destructor.
- Factory: a `const` member function `create(VehicleCategory)` with a `switch` over an `enum class`, returning `std::unique_ptr<VehicleCategoryRules>` so that ownership of the product is explicit.
- Optional rates: `std::optional<double>`; `std::nullopt` means "no value".
- Unknown category: an `enum class` can hold any value of its underlying type, so the test passes `static_cast<VehicleCategory>(999)`. It falls through the `switch` and is rejected with `std::invalid_argument`.

## Generated files (not source; exclude from the upload if required)

`test_runner.exe` (about 3 MB because of `-static`)
