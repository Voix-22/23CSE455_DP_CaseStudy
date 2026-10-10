# CH02 - Effect on pattern instances

Source version: code after Change2 (`7af8d229edfaa1d5c933a0a56aa267a443bece9f`, tag `ch02`), compared with the baseline `dfc61ab8dd6f9bbb2301171457dc03946a249576`. Baseline instance names are taken from the team's `part1_patterns_updated.csv`. Paths are relative to `WanderCar_DotNet/src/FlemanApi/` and line numbers refer to the `ch02` commit.

## Newly introduced

### Factory Method (parameterized) - Vehicle Category Rules Creation

Category: GoF Creational. Origin: student-added in CH02. No baseline collaboration is replaced: at baseline there was no category concept, so this instance has no predecessor.

| Role | Actual symbol | File and lines |
|---|---|---|
| Creator (interface) | `FlemanApi.Service.Categories.IVehicleCategoryRulesFactory` | `Service/Categories/VehicleCategoryRulesFactory.cs:7-10` |
| ConcreteCreator | `FlemanApi.Service.Categories.VehicleCategoryRulesFactory` | `Service/Categories/VehicleCategoryRulesFactory.cs:15-24` |
| Factory method | `VehicleCategoryRulesFactory.Create(VehicleCategory)` | `Service/Categories/VehicleCategoryRulesFactory.cs:17-23` |
| Product | `FlemanApi.Service.Categories.IVehicleCategoryRules` | `Service/Categories/IVehicleCategoryRules.cs:8-17` |
| ConcreteProduct | `StandardCategoryRules` | `Service/Categories/StandardCategoryRules.cs:6-14` |
| ConcreteProduct | `ElectricCategoryRules` | `Service/Categories/ElectricCategoryRules.cs:5-15` |
| ConcreteProduct | `CommercialCategoryRules` | `Service/Categories/CommercialCategoryRules.cs:5-20` |
| Client | `FlemanApi.Service.CarTypeService.CreateCarTypeAsync` (calls `Create` at `Service/CarTypeService.cs:36`) | `Service/CarTypeService.cs:27-59` |
| Binding | `AddSingleton<IVehicleCategoryRulesFactory, VehicleCategoryRulesFactory>` | `Program.cs:190-191` |

Evidence: `CarTypeService` never names a concrete rules class. It asks the factory for the rules of the requested category and then uses only the `IVehicleCategoryRules` interface to validate rates and compute the deposit. The `switch` in `Create` is the single place that maps a category to a class. Executed evidence: `VehicleCategoryRulesTests` (7 tests) and `CarTypeServiceTests` (7 tests) pass; at run time the three categories produce deposits 0, 120 and 400 (`http_evidence.txt` B3, B5, B6).

Classification caveat for the reviewer: this is the *parameterized* factory method variant (one creator method that takes a parameter identifying the product; GoF, Factory Method, Implementation). There is one concrete creator, not a creator class hierarchy, so some textbooks call this structure Simple Factory. Concrete participant classes: 3 products and 1 creator.

## Effect on baseline instances

| Baseline pattern | Instance name | Status after CH02 | Reason |
|---|---|---|---|
| Repository | Generic Repository | extended | New client `CarTypeService` uses `IGenericRepository<CarType, long>` (`Service/CarTypeService.cs:13`; calls at `Service/CarTypeService.cs:31`, `:56`, `:57`, `:63`). `IGenericRepository` and `GenericRepository` are unchanged. |
| Dependency Injection / IoC | ASP.NET Core DI Composition | extended | Two registrations added in `Program.cs` (`Program.cs:189-192`); `CarTypeService` and `CarTypeController` receive their collaborators by constructor. |
| Service Layer | Booking Service Layer | preserved | `BookingController`, `IBookingService` and `BookingService` are not touched. A parallel controller-service pair (`CarTypeController` -> `ICarTypeService` -> `CarTypeService`) follows the same layering but is not part of this instance. |
| Middleware Pipeline / Chain of Responsibility | ASP.NET Core Request Middleware Chain | preserved | No middleware or pipeline change; the new endpoints are served through the same chain (the 400 and 409 bodies in `http_evidence.txt` are produced by the existing `ExceptionHandlingMiddleware`). |
| Producer-Consumer | Email Background Work Queue | preserved | Not touched. |
| Adapter | Java Microservice Adapter | preserved | Not touched. |
| Proxy | Legacy API Proxy | preserved | Not touched. |
| Strangler Fig | Legacy Java Integration | preserved | Not touched. The Java backend has no knowledge of the new category columns; the invoice PDF payload does not include them. |
| DTO | API Request/Response DTO Boundary | modified | `CarTypeDTO` gains `Category` and `SecurityDeposit` (`DTO/CarTypeDTO.cs:16-17`); new request DTO `CreateCarTypeRequest` (`DTO/CreateCarTypeRequest.cs:5-13`). `MappingProfile` is unchanged: the existing `CarType <-> CarTypeDTO` map converts the enum to its name (confirmed by the passing test `Mapping_CarTypeWithoutExplicitCategory_IsReportedAsStandard` and by `http_evidence.txt` B1). |

## Counts (baseline -> after CH02, CH02 applied directly on the baseline)

| Measure | Baseline | After CH02 |
|---|---|---|
| Distinct pattern types | 9 | 10 (Factory Method added) |
| Pattern instances | 9 | 10 |
| Concrete participant classes of the new instance | 0 | 4 (1 concrete creator, 3 concrete products) plus 2 interfaces |
| Instances removed or replaced | - | 0 |
| Rejected / uncertain observations | - | 1: the Factory Method label is uncertain (Simple Factory in some texts); the `CarTypeController`/`CarTypeService` pair was considered as a second Service Layer instance and rejected as a separate instance, because it only repeats the existing layering |

If another team change is merged before this one, add its instances to both columns; CH02 itself adds exactly one type and one instance.
