# Java - Factory Method (parameterized): Vehicle Category Rules Creation

| | |
|---|---|
| Tool | `javac 17.0.16`; runtime OpenJDK 17.0.16 (Temurin-17.0.16+8, 64-Bit Server VM) |
| Working folder | `Part3/FactoryMethod_VehicleCategoryRules/java` |
| Snippet | `VehicleCategoryRulesFactory.java` |
| Test runner | `TestRunner.java` |
| Libraries | Java standard library only |

## Compile and run

```
javac VehicleCategoryRulesFactory.java TestRunner.java
java -cp . TestRunner
```

Exit code 0 means all seven cases passed. The recorded output is `test_result.txt` (PASS 7/7); raw timings are in `timings.txt`.

## How the pattern is expressed

- Abstraction: an `interface` (`VehicleCategoryRules`), implemented by three `final` classes.
- Factory: an instance method `create(VehicleCategory)` using a `switch` expression over the `enum`. The compiler checks that every enum constant has an arm, so adding a category without a rules class is a compile error.
- Optional rates: boxed `Double`, where `null` means "no value".
- Unknown category: a Java `enum` cannot hold a value outside its constants, so the only unknown value is `null`. `create(null)` is rejected with `NullPointerException` from `Objects.requireNonNull`.

## Generated files (not source; exclude from the upload if required)

`CommercialCategoryRules.class`, `ElectricCategoryRules.class`, `StandardCategoryRules.class`, `TestRunner.class`, `VehicleCategory.class`, `VehicleCategoryRules.class`, `VehicleCategoryRulesFactory.class`, `VehicleCategoryRulesFactory$1.class`
