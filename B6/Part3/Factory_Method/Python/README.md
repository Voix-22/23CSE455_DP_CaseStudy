# Python - Factory Method (parameterized): Vehicle Category Rules Creation

| | |
|---|---|
| Tool | CPython `Python 3.14.3` |
| Working folder | `Part3/FactoryMethod_VehicleCategoryRules/python` |
| Snippet | `vehicle_category_rules.py` |
| Test runner | `test_runner.py` |
| Libraries | Python standard library only (`abc`, `enum`, `sys`) |

## Compile and run

There is no separate compile step; CPython compiles the imported module to bytecode on the first run.

```
python test_runner.py
```

Exit code 0 means all seven cases passed. The recorded output is `test_result.txt` (PASS 7/7); raw timings are in `timings.txt`.

## How the pattern is expressed

- Abstraction: an abstract base class (`abc.ABC` with `@abstractmethod`). Instantiating a subclass that misses a method fails at construction time, not at compile time.
- Factory: an instance method `create(category)` that looks the class up in a dictionary keyed by the `Enum` member and calls it. Classes are first-class objects, so no `switch` is needed.
- Optional rates: `None` means "no value".
- Unknown category: Python does not check argument types, so any object can be passed. The test passes the string `'HOVERCRAFT'`, which is rejected with `ValueError`.
- Method names follow PEP 8 (`calculate_security_deposit`, `validate_rates`).

## Generated files (not source; exclude from the upload if required)

`__pycache__/vehicle_category_rules.cpython-314.pyc`
