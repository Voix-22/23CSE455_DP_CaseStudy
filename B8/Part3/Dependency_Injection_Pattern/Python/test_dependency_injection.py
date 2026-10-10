"""Same ten cases (D01-D09, D04b) as Part3/javascript/test/dependency_injection.test.mjs."""
import unittest

from dependency_injection import Lifetime, ServiceCollection


# ---- Minimal stand-ins for the Program.cs shape --------------------------------

class FlemanDbContext:
    """One unit of work: added rows become visible only after save_changes()."""

    def __init__(self):
        self.pending, self.saved = [], []

    def save_changes(self):
        self.saved.extend(self.pending)
        self.pending.clear()


class GenericRepository:
    def __init__(self, context, entity):
        self.context, self.entity = context, entity

    def add(self, row):
        self.context.pending.append((self.entity, row))

    def save_changes(self):
        self.context.save_changes()

    def count(self):
        return sum(1 for entity, _ in self.context.saved if entity == self.entity)


class MaintenanceService:
    def __init__(self, schedules, cars, time_provider):
        self.schedules, self.cars, self.time_provider = schedules, cars, time_provider


class SystemClock:
    def now(self):
        return "2026-10-08T09:30:00"


SYSTEM_CLOCK = SystemClock()


def build_services():
    return (ServiceCollection()
            .add_scoped("FlemanDbContext", FlemanDbContext)
            .add_open_generic("IGenericRepository", Lifetime.SCOPED,
                              lambda entity, sp: GenericRepository(sp.get_required_service("FlemanDbContext"), entity))
            .add_singleton_instance("TimeProvider", SYSTEM_CLOCK)
            .add_scoped("IMaintenanceService", MaintenanceService,
                        "IGenericRepository<MaintenanceSchedule>", "IGenericRepository<Car>", "TimeProvider"))


# ---- Tests ---------------------------------------------------------------------

class DependencyInjectionTest(unittest.TestCase):
    """Dependency Injection: Service Registration and Constructor Injection"""

    def test_d01_constructor_injection_resolves_the_whole_graph(self):
        service = build_services().build().create_scope().get_required_service("IMaintenanceService")

        self.assertIsInstance(service, MaintenanceService)
        self.assertIsInstance(service.schedules, GenericRepository)
        self.assertEqual(service.schedules.entity, "MaintenanceSchedule")
        self.assertEqual(service.cars.entity, "Car")
        self.assertIs(service.time_provider, SYSTEM_CLOCK)

    def test_d02_scoped_one_instance_per_scope(self):
        root = build_services().build()
        scope_a, scope_b = root.create_scope(), root.create_scope()

        self.assertIs(scope_a.get_required_service("FlemanDbContext"), scope_a.get_required_service("FlemanDbContext"))
        self.assertIsNot(scope_a.get_required_service("FlemanDbContext"), scope_b.get_required_service("FlemanDbContext"))

    def test_d03_services_in_one_scope_share_the_db_context(self):
        service = build_services().build().create_scope().get_required_service("IMaintenanceService")
        service.cars.add("MH01AA1111")
        service.schedules.save_changes()  # saved through the *other* repository

        self.assertEqual(service.cars.count(), 1)

    def test_d04_singleton_instance_shared_everywhere(self):
        root = build_services().build()

        self.assertIs(root.get_required_service("TimeProvider"), SYSTEM_CLOCK)
        self.assertIs(root.create_scope().get_required_service("TimeProvider"),
                      root.create_scope().get_required_service("TimeProvider"))

    def test_d04b_class_registered_singleton_built_once(self):
        class EmailBackgroundQueue:
            pass

        root = ServiceCollection().add_singleton("IEmailBackgroundQueue", EmailBackgroundQueue).build()
        from_scope_a = root.create_scope().get_required_service("IEmailBackgroundQueue")

        self.assertIsInstance(from_scope_a, EmailBackgroundQueue)
        self.assertIs(from_scope_a, root.create_scope().get_required_service("IEmailBackgroundQueue"))
        self.assertIs(from_scope_a, root.get_required_service("IEmailBackgroundQueue"))

    def test_d05_transient_new_instance_every_resolve(self):
        class Stopwatch:
            pass

        scope = ServiceCollection().add_transient("Stopwatch", Stopwatch).build().create_scope()

        self.assertIsNot(scope.get_required_service("Stopwatch"), scope.get_required_service("Stopwatch"))

    def test_d06_open_generic_closes_per_type_argument(self):
        scope = build_services().build().create_scope()
        cars = scope.get_required_service("IGenericRepository<Car>")

        self.assertIs(cars, scope.get_required_service("IGenericRepository<Car>"))
        self.assertIsNot(cars, scope.get_required_service("IGenericRepository<Hub>"))

    def test_d07_unregistered_service_fails_with_dotnet_message(self):
        scope = build_services().build().create_scope()

        with self.assertRaises(LookupError) as ctx:
            scope.get_required_service("IBookingService")
        self.assertEqual(str(ctx.exception), "No service for type 'IBookingService' has been registered.")

    def test_d08_scoped_service_not_resolvable_from_root(self):
        with self.assertRaises(LookupError) as ctx:
            build_services().build().get_required_service("IMaintenanceService")
        self.assertEqual(str(ctx.exception), "Cannot resolve scoped service 'IMaintenanceService' from root provider.")

    def test_d09_last_registration_wins(self):
        class InvoicePdfService:
            pass

        class JavaInvoicePdfService:
            pass

        scope = (ServiceCollection()
                 .add_scoped("IInvoicePdfService", InvoicePdfService)
                 .add_scoped("IInvoicePdfService", JavaInvoicePdfService)
                 .build().create_scope())

        self.assertIsInstance(scope.get_required_service("IInvoicePdfService"), JavaInvoicePdfService)


if __name__ == "__main__":
    unittest.main(verbosity=2)
