# JavaScript - Factory Method (parameterized): Vehicle Category Rules Creation

| | |
|---|---|
| Tool | Node.js `v24.14.1` |
| Working folder | `Part3/FactoryMethod_VehicleCategoryRules/javascript` |
| Snippet | `vehicleCategoryRules.js` |
| Test runner | `testRunner.js` |
| Libraries | none (CommonJS `require` of the snippet only; no `package.json`, no npm packages) |

## Compile and run

There is no compile step.

```
node testRunner.js
```

Exit code 0 means all seven cases passed. The recorded output is `test_result.txt` (PASS 7/7); raw timings are in `timings.txt`.

## How the pattern is expressed

- Abstraction: JavaScript has no interface construct. The contract is the three method names (duck typing). A base class `VehicleCategoryRules` whose methods throw "not implemented" documents the contract and makes a missing override fail when it is called.
- Enumeration: a frozen object of string constants (`Object.freeze`), since the language has no `enum`.
- Factory: an instance method `create(category)` with a `switch`; the `default` branch throws.
- Optional rates: `null` (or `undefined`) means "no value"; the check is `weeklyRate == null`.
- Unknown category: any value can be passed. The test passes the string `'HOVERCRAFT'`, which is rejected with `RangeError`.

## Generated files

None.
