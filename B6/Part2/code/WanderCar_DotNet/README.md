# Fleman / WanderCar — .NET Backend

This is a full ASP.NET Core 8 port of the Java Spring Boot backend at `C:\fleman\fleman-backend` (package `com.fleman`). It replicates the same APIs and business logic — booking, staff hand-over/return, invoicing, auth — while following .NET idioms and the architectural requirements below. `fleman-backend` and `frontend 2` were left untouched (aside from three small frontend validation tweaks — see "Frontend validation" below); this is a brand-new, separate project.

## Requirements covered

| # | Requirement | Where |
|---|---|---|
| 1 | Structured logging | Serilog (console + rolling file under `logs/`), `ILogger<T>` throughout, `RequestLoggingMiddleware` |
| 2 | JWT auth | `Security/JwtTokenService.cs`, `AddJwtBearer` in `Program.cs`; same claims (`sub`, `email`, `role`) as the Java app. Google OAuth2 also ported — see `Controllers/GoogleAuthController.cs` |
| 3 | Microsoft.Extensions.AI | `Service/AiInsightsService.cs` — natural-language staff dashboard summary, `GET /api/staff/dashboard/summary` |
| 4 | Global exception middleware | `Middleware/ExceptionHandlingMiddleware.cs` — same `{ message, status, timestamp }` shape as the Java app's `GlobalExceptionHandler` |
| 5 | NUnit tests | `src/FlemanApi.Tests` |
| 6 | Validation | FluentValidation server-side (`Validators/`); 3 frontend forms in `frontend 2` aligned to match (see below) |
| 7, 8 | Generic CRUD interface + implementation | `Repository/IGenericRepository.cs` + `GenericRepository.cs`, `Service/IGenericService.cs` + `GenericService.cs` |
| 9 | "Employee" service pattern | `Service/CustomerService.cs` — the closest analogue this domain has to an Employee entity: a person record with real business logic (login/register/guest-upgrade/staff-login) layered on generic CRUD |
| 10 | AutoMapper | `AutoMapper/MappingProfile.cs` |
| 11 | Java microservice integration | `Service/JavaMicroserviceClient.cs` + `Controllers/LegacyProxyController.cs` — calls the still-running Java backend over HTTP |

## Project layout

```
dotnet-backend/
  DotnetBackend.slnx
  src/
    FlemanApi/            ASP.NET Core Web API
    FlemanApi.Tests/      NUnit test project
```

## Running it

### Prerequisites
- .NET 8 SDK (or later, targeting `net8.0`)
- MySQL Server running locally (the Java app already assumes this — same server works for both)
- Node/npm (for the frontend, unchanged)

### 1. Database
No manual step needed — `Program.cs` calls `db.Database.Migrate()` on startup, which creates the `fleetmanagement_dotnet_dev` database (see `appsettings.Development.json`) and applies the EF Core migration. `DataSeeder` then populates it with the same India-fixture dataset the Java app seeds (states/cities/hubs/airports/car types/cars/addons/one sample booking), gated on the database being empty.

If you'd rather run migrations by hand:
```bash
cd src/FlemanApi
dotnet ef database update
```

### 2. Secrets
`appsettings.json`/`appsettings.Development.json` intentionally leave `Mail:Username`/`Mail:Password` and `GoogleOAuth:ClientId`/`ClientSecret` blank — copy your own values in (or the ones from `fleman-backend/src/main/resources/config/application-dev.properties`) via `appsettings.Development.json`, user-secrets, or environment variables. Without them:
- Email sending logs "Mail not configured — skipping send" instead of throwing (the app stays fully usable).
- Google login isn't reachable, but username/password JWT auth works regardless.

The AI dashboard summary (`Ai:Provider`/`Ai:ApiKey` in appsettings) is optional too — with no key configured it returns a plain templated summary instead of calling out to a provider.

### 3. Run the .NET backend
```bash
cd src/FlemanApi
dotnet run
```
Listens on `http://localhost:5180` / `https://localhost:7156` by default (see `Properties/launchSettings.json`). Swagger UI is at `/swagger` in Development.

### 4. Run the Java backend alongside it (optional, for the legacy-proxy endpoints)
```bash
cd C:\fleman\fleman-backend
mvn spring-boot:run
```
Runs on port 8080 (unchanged). With it running, `GET /api/legacy/states` and `GET /api/legacy/health` on the .NET API will successfully forward to it. Everything else in the .NET API works standalone regardless of whether the Java app is running.

### 5. Run the frontend against the .NET backend
```bash
cd "C:\fleman\frontend 2"
npm install
npm run dev
```
The frontend talks to whatever `VITE_API_BASE_URL` in `.env` points at (`/api`, proxied) — point its dev-server proxy or a reverse proxy at the .NET API's port instead of the Java one to switch backends. Both backends expose the same route shapes (`/api/...`), so no frontend code changes are needed to swap between them, beyond the three validation-alignment tweaks below.

### 6. Run the tests
```bash
cd C:\fleman\dotnet-backend
dotnet test
```

## Frontend validation alignment

Per the brief, server-side validation (FluentValidation) is the primary implementation; three existing frontend files were reviewed against the new server rules and found to already closely match (native `required`/`type="email"`/`minLength` attributes, or equivalent inline JS checks):
- `frontend 2/src/components/layout/AuthModal.jsx` — matches `LoginRequestValidator`/`RegisterRequestValidator`.
- `frontend 2/src/components/layout/StaffLoginModal.jsx` — matches `StaffLoginRequestValidator`.
- `frontend 2/src/pages/CustomerInfoPage.jsx` — one field tightened (`required` added to the email input) to fully match `GuestCustomerRequestValidator`'s required-email rule; everything else already matched or was intentionally stricter than the server (e.g. requiring phone/licence client-side for a smoother booking flow, even though the server DTO leaves them optional).

## Design notes / deliberate differences from the Java app

- **No EF Core navigation properties** — mirrors the Java entities exactly, which have zero `@ManyToOne`/`@OneToMany` anywhere; every cross-table reference is a plain scalar id column, validated at the service layer.
- **`StaffLoginAsync`'s hardcoded credential** (`Team2@gmail.com` / `123456789`) is ported as-is — the Java app has no staff table, just this one fixed pair; that's genuinely today's behavior, not a shortcut taken during the port.
- **Cross-cutting logging**: the Java app used Spring AOP (`LoggingAspect`, `BookingAspect`) to wrap every service method. .NET has no equivalent AOP mechanism out of the box, so this became `RequestLoggingMiddleware` (HTTP-boundary timing) plus explicit `ILogger` calls in `BookingService.CreateBookingAsync`.
- **Email sending** is queued onto an in-process `Channel<T>`-backed background worker (`EmailBackgroundQueue` + `EmailQueueHostedService`), the .NET equivalent of the Java app's `@Async("emailExecutor")` thread pool.
