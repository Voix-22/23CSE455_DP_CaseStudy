// Same ten cases (D01-D09, D04b) as Part3/javascript/test/dependency_injection.test.mjs.
// Self-contained: a ~25-line test harness instead of a framework download.
#include <exception>
#include <functional>
#include <iostream>
#include <memory>
#include <string>
#include <utility>
#include <vector>

#include "dependency_injection.hpp"

// ---- Tiny test harness ---------------------------------------------------------

namespace harness {
struct Failure { std::string what; };
std::vector<std::pair<std::string, std::function<void()>>>& tests() {
    static std::vector<std::pair<std::string, std::function<void()>>> all;
    return all;
}
struct Register {
    Register(const char* name, std::function<void()> fn) { tests().emplace_back(name, std::move(fn)); }
};
}  // namespace harness

#define TEST(id, name) \
    static void id(); \
    static harness::Register id##_reg(name, id); \
    static void id()
#define CHECK(cond) \
    do { if (!(cond)) throw harness::Failure{std::string(#cond) + " (line " + std::to_string(__LINE__) + ")"}; } while (0)
#define CHECK_THROWS_MESSAGE(expr, msg) \
    do { \
        bool threw_ = false; \
        try { expr; } catch (const std::exception& e_) { threw_ = true; CHECK(std::string(e_.what()) == (msg)); } \
        CHECK(threw_); \
    } while (0)

// ---- Minimal stand-ins for the Program.cs shape --------------------------------

using di::Lifetime;
using di::ServiceCollection;
using di::ServiceProvider;

// One unit of work: added rows become visible only after saveChanges().
struct FlemanDbContext {
    std::vector<std::pair<std::string, std::string>> pending, saved;
    void saveChanges() {
        saved.insert(saved.end(), pending.begin(), pending.end());
        pending.clear();
    }
};

struct GenericRepository {
    GenericRepository(std::shared_ptr<FlemanDbContext> c, std::string e) : context(std::move(c)), entity(std::move(e)) {}
    void add(const std::string& row) { context->pending.emplace_back(entity, row); }
    void saveChanges() { context->saveChanges(); }
    long count() const {
        long n = 0;
        for (const auto& r : context->saved) n += r.first == entity;
        return n;
    }
    std::shared_ptr<FlemanDbContext> context;
    std::string entity;
};

struct TimeProvider {
    virtual ~TimeProvider() = default;
    virtual std::string now() const = 0;
};
struct SystemClock final : TimeProvider {
    std::string now() const override { return "2026-10-08T09:30:00"; }
};
const auto SYSTEM_CLOCK = std::make_shared<SystemClock>();

struct IMaintenanceService {
    virtual ~IMaintenanceService() = default;
};
struct MaintenanceService final : IMaintenanceService {
    MaintenanceService(std::shared_ptr<GenericRepository> s, std::shared_ptr<GenericRepository> c,
                       std::shared_ptr<TimeProvider> t)
        : schedules(std::move(s)), cars(std::move(c)), timeProvider(std::move(t)) {}
    std::shared_ptr<GenericRepository> schedules, cars;
    std::shared_ptr<TimeProvider> timeProvider;
};

ServiceCollection buildServices() {
    ServiceCollection services;
    services.addScoped<FlemanDbContext>("FlemanDbContext")
        .addOpenGeneric<GenericRepository>("IGenericRepository", Lifetime::Scoped,
            [](const std::string& entity, ServiceProvider& sp) {
                return std::make_shared<GenericRepository>(sp.getRequiredService<FlemanDbContext>("FlemanDbContext"), entity);
            })
        .addSingletonInstance<TimeProvider>("TimeProvider", SYSTEM_CLOCK)
        .addScoped<IMaintenanceService, MaintenanceService, GenericRepository, GenericRepository, TimeProvider>(
            "IMaintenanceService", {"IGenericRepository<MaintenanceSchedule>", "IGenericRepository<Car>", "TimeProvider"});
    return services;
}

std::shared_ptr<MaintenanceService> maintenance(ServiceProvider& scope) {
    return std::dynamic_pointer_cast<MaintenanceService>(scope.getRequiredService<IMaintenanceService>("IMaintenanceService"));
}

// ---- Tests -----------------------------------------------------------------------

TEST(d01, "D01 constructor injection resolves the whole dependency graph") {
    auto root = buildServices().build();
    auto scope = root.createScope();
    auto service = maintenance(scope);

    CHECK(service != nullptr);
    CHECK(service->schedules->entity == "MaintenanceSchedule");
    CHECK(service->cars->entity == "Car");
    CHECK(service->timeProvider == SYSTEM_CLOCK);
}

TEST(d02, "D02 scoped: one instance per scope, a new one in each new scope") {
    auto root = buildServices().build();
    auto scopeA = root.createScope();
    auto scopeB = root.createScope();

    CHECK(scopeA.getRequiredService<FlemanDbContext>("FlemanDbContext") == scopeA.getRequiredService<FlemanDbContext>("FlemanDbContext"));
    CHECK(scopeA.getRequiredService<FlemanDbContext>("FlemanDbContext") != scopeB.getRequiredService<FlemanDbContext>("FlemanDbContext"));
}

TEST(d03, "D03 services in one scope share that scope's DbContext (one unit of work per request)") {
    auto root = buildServices().build();
    auto scope = root.createScope();
    auto service = maintenance(scope);
    service->cars->add("MH01AA1111");
    service->schedules->saveChanges();  // saved through the *other* repository

    CHECK(service->cars->count() == 1);
}

TEST(d04, "D04 singleton: the same instance in every scope and on the root") {
    auto root = buildServices().build();
    auto scopeA = root.createScope();
    auto scopeB = root.createScope();

    CHECK(root.getRequiredService<TimeProvider>("TimeProvider") == SYSTEM_CLOCK);
    CHECK(scopeA.getRequiredService<TimeProvider>("TimeProvider") == scopeB.getRequiredService<TimeProvider>("TimeProvider"));
}

TEST(d04b, "D04b a class-registered singleton is built once and shared by all scopes") {
    struct EmailBackgroundQueue {};
    ServiceCollection services;
    services.addSingleton<EmailBackgroundQueue>("IEmailBackgroundQueue");
    auto root = services.build();
    auto scopeA = root.createScope();
    auto scopeB = root.createScope();
    auto fromScopeA = scopeA.getRequiredService<EmailBackgroundQueue>("IEmailBackgroundQueue");

    CHECK(fromScopeA != nullptr);
    CHECK(fromScopeA == scopeB.getRequiredService<EmailBackgroundQueue>("IEmailBackgroundQueue"));
    CHECK(fromScopeA == root.getRequiredService<EmailBackgroundQueue>("IEmailBackgroundQueue"));
}

TEST(d05, "D05 transient: a new instance on every resolve") {
    struct Stopwatch {};
    ServiceCollection services;
    services.addTransient<Stopwatch>("Stopwatch");
    auto root = services.build();
    auto scope = root.createScope();

    CHECK(scope.getRequiredService<Stopwatch>("Stopwatch") != scope.getRequiredService<Stopwatch>("Stopwatch"));
}

TEST(d06, "D06 open generic closes per type argument") {
    auto root = buildServices().build();
    auto scope = root.createScope();
    auto cars = scope.getRequiredService<GenericRepository>("IGenericRepository<Car>");

    CHECK(cars == scope.getRequiredService<GenericRepository>("IGenericRepository<Car>"));
    CHECK(cars != scope.getRequiredService<GenericRepository>("IGenericRepository<Hub>"));
}

TEST(d07, "D07 an unregistered service fails with the .NET message") {
    auto root = buildServices().build();
    auto scope = root.createScope();

    CHECK_THROWS_MESSAGE(scope.getRequiredService<IMaintenanceService>("IBookingService"),
                         "No service for type 'IBookingService' has been registered.");
}

TEST(d08, "D08 a scoped service cannot be resolved from the root provider") {
    auto root = buildServices().build();

    CHECK_THROWS_MESSAGE(root.getRequiredService<IMaintenanceService>("IMaintenanceService"),
                         "Cannot resolve scoped service 'IMaintenanceService' from root provider.");
}

TEST(d09, "D09 the last registration wins (swap JavaInvoicePdfService for InvoicePdfService)") {
    struct IInvoicePdfService { virtual ~IInvoicePdfService() = default; };
    struct InvoicePdfService final : IInvoicePdfService {};
    struct JavaInvoicePdfService final : IInvoicePdfService {};
    ServiceCollection services;
    services.addScoped<IInvoicePdfService, InvoicePdfService>("IInvoicePdfService")
        .addScoped<IInvoicePdfService, JavaInvoicePdfService>("IInvoicePdfService");
    auto root = services.build();
    auto scope = root.createScope();

    auto resolved = scope.getRequiredService<IInvoicePdfService>("IInvoicePdfService");
    CHECK(std::dynamic_pointer_cast<JavaInvoicePdfService>(resolved) != nullptr);
}

// ---- Runner (TAP-style output, like node --test) ------------------------------

int main() {
    int passed = 0, number = 0;
    std::cout << "# Dependency Injection: Service Registration and Constructor Injection\n";
    for (const auto& [name, fn] : harness::tests()) {
        ++number;
        try {
            fn();
            ++passed;
            std::cout << "ok " << number << " - " << name << "\n";
        } catch (const harness::Failure& f) {
            std::cout << "not ok " << number << " - " << name << "\n  # failed: " << f.what << "\n";
        } catch (const std::exception& e) {
            std::cout << "not ok " << number << " - " << name << "\n  # threw: " << e.what() << "\n";
        }
    }
    const int total = static_cast<int>(harness::tests().size());
    std::cout << "1.." << total << "\n# tests " << total << "\n# pass " << passed << "\n# fail " << total - passed << "\n";
    return passed == total ? 0 : 1;
}
