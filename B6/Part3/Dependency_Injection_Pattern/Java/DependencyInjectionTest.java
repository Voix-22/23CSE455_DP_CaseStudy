package di;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertInstanceOf;
import static org.junit.jupiter.api.Assertions.assertNotSame;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertThrows;

import di.DependencyInjection.Lifetime;
import di.DependencyInjection.ServiceCollection;
import di.DependencyInjection.ServiceProvider;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

// Same ten cases (D01-D09, D04b) as Part3/javascript/test/dependency_injection.test.mjs.
@DisplayName("Dependency Injection: Service Registration and Constructor Injection")
class DependencyInjectionTest {

    // ---- Minimal stand-ins for the Program.cs shape --------------------------

    // One unit of work: added rows become visible only after saveChanges().
    public static final class FlemanDbContext {
        final List<String[]> pending = new ArrayList<>();
        final List<String[]> saved = new ArrayList<>();

        void saveChanges() {
            saved.addAll(pending);
            pending.clear();
        }
    }

    public static final class GenericRepository {
        final FlemanDbContext context;
        final String entity;

        GenericRepository(FlemanDbContext context, String entity) {
            this.context = context;
            this.entity = entity;
        }

        void add(String row) { context.pending.add(new String[] { entity, row }); }
        void saveChanges() { context.saveChanges(); }
        long count() { return context.saved.stream().filter(r -> r[0].equals(entity)).count(); }
    }

    public interface TimeProvider { String now(); }

    public static final class MaintenanceService {
        final GenericRepository schedules;
        final GenericRepository cars;
        final TimeProvider timeProvider;

        public MaintenanceService(GenericRepository schedules, GenericRepository cars, TimeProvider timeProvider) {
            this.schedules = schedules;
            this.cars = cars;
            this.timeProvider = timeProvider;
        }
    }

    public static final class Stopwatch { public Stopwatch() { } }
    public static final class EmailBackgroundQueue { public EmailBackgroundQueue() { } }
    public static final class InvoicePdfService { public InvoicePdfService() { } }
    public static final class JavaInvoicePdfService { public JavaInvoicePdfService() { } }

    static final TimeProvider SYSTEM_CLOCK = () -> "2026-10-08T09:30:00";

    static ServiceCollection buildServices() {
        return new ServiceCollection()
                .addScoped("FlemanDbContext", FlemanDbContext.class)
                .addOpenGeneric("IGenericRepository", Lifetime.SCOPED,
                        (entity, sp) -> new GenericRepository(sp.getRequiredService("FlemanDbContext", FlemanDbContext.class), entity))
                .addSingletonInstance("TimeProvider", SYSTEM_CLOCK)
                .addScoped("IMaintenanceService", MaintenanceService.class,
                        "IGenericRepository<MaintenanceSchedule>", "IGenericRepository<Car>", "TimeProvider");
    }

    // ---- Tests ---------------------------------------------------------------

    @Test
    @DisplayName("D01 constructor injection resolves the whole dependency graph")
    void d01() {
        ServiceProvider scope = buildServices().build().createScope();
        MaintenanceService service = scope.getRequiredService("IMaintenanceService", MaintenanceService.class);

        assertInstanceOf(GenericRepository.class, service.schedules);
        assertInstanceOf(GenericRepository.class, service.cars);
        assertEquals("MaintenanceSchedule", service.schedules.entity);
        assertEquals("Car", service.cars.entity);
        assertSame(SYSTEM_CLOCK, service.timeProvider);
    }

    @Test
    @DisplayName("D02 scoped: one instance per scope, a new one in each new scope")
    void d02() {
        ServiceProvider root = buildServices().build();
        ServiceProvider scopeA = root.createScope();
        ServiceProvider scopeB = root.createScope();

        assertSame(scopeA.getRequiredService("FlemanDbContext"), scopeA.getRequiredService("FlemanDbContext"));
        assertNotSame(scopeA.getRequiredService("FlemanDbContext"), scopeB.getRequiredService("FlemanDbContext"));
    }

    @Test
    @DisplayName("D03 services in one scope share that scope's DbContext (one unit of work per request)")
    void d03() {
        ServiceProvider scope = buildServices().build().createScope();
        MaintenanceService service = scope.getRequiredService("IMaintenanceService", MaintenanceService.class);
        service.cars.add("MH01AA1111");
        service.schedules.saveChanges(); // saved through the *other* repository

        assertEquals(1, service.cars.count());
    }

    @Test
    @DisplayName("D04 singleton: the same instance in every scope and on the root")
    void d04() {
        ServiceProvider root = buildServices().build();

        assertSame(SYSTEM_CLOCK, root.getRequiredService("TimeProvider"));
        assertSame(root.createScope().getRequiredService("TimeProvider"), root.createScope().getRequiredService("TimeProvider"));
    }

    @Test
    @DisplayName("D04b a class-registered singleton is built once and shared by all scopes")
    void d04b() {
        ServiceProvider root = new ServiceCollection().addSingleton("IEmailBackgroundQueue", EmailBackgroundQueue.class).build();
        Object fromScopeA = root.createScope().getRequiredService("IEmailBackgroundQueue");

        assertInstanceOf(EmailBackgroundQueue.class, fromScopeA);
        assertSame(fromScopeA, root.createScope().getRequiredService("IEmailBackgroundQueue"));
        assertSame(fromScopeA, root.getRequiredService("IEmailBackgroundQueue"));
    }

    @Test
    @DisplayName("D05 transient: a new instance on every resolve")
    void d05() {
        ServiceProvider scope = new ServiceCollection().addTransient("Stopwatch", Stopwatch.class).build().createScope();

        assertNotSame(scope.getRequiredService("Stopwatch"), scope.getRequiredService("Stopwatch"));
    }

    @Test
    @DisplayName("D06 open generic closes per type argument")
    void d06() {
        ServiceProvider scope = buildServices().build().createScope();
        Object cars = scope.getRequiredService("IGenericRepository<Car>");

        assertSame(cars, scope.getRequiredService("IGenericRepository<Car>"));
        assertNotSame(cars, scope.getRequiredService("IGenericRepository<Hub>"));
    }

    @Test
    @DisplayName("D07 an unregistered service fails with the .NET message")
    void d07() {
        ServiceProvider scope = buildServices().build().createScope();

        IllegalStateException e = assertThrows(IllegalStateException.class, () -> scope.getRequiredService("IBookingService"));
        assertEquals("No service for type 'IBookingService' has been registered.", e.getMessage());
    }

    @Test
    @DisplayName("D08 a scoped service cannot be resolved from the root provider")
    void d08() {
        ServiceProvider root = buildServices().build();

        IllegalStateException e = assertThrows(IllegalStateException.class, () -> root.getRequiredService("IMaintenanceService"));
        assertEquals("Cannot resolve scoped service 'IMaintenanceService' from root provider.", e.getMessage());
    }

    @Test
    @DisplayName("D09 the last registration wins (swap JavaInvoicePdfService for InvoicePdfService)")
    void d09() {
        ServiceProvider scope = new ServiceCollection()
                .addScoped("IInvoicePdfService", InvoicePdfService.class)
                .addScoped("IInvoicePdfService", JavaInvoicePdfService.class)
                .build().createScope();

        assertInstanceOf(JavaInvoicePdfService.class, scope.getRequiredService("IInvoicePdfService"));
    }
}
