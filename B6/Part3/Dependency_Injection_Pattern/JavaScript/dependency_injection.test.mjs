import assert from 'node:assert/strict';
import { describe, test } from 'node:test';
import { Lifetime, ServiceCollection } from '../src/dependency_injection.mjs';
import { GenericRepository, InMemoryDbContext } from '../src/repository.mjs';

// Same shape as Program.cs: DbContext scoped, IGenericRepository<,> open
// generic scoped, TimeProvider singleton, a service with constructor deps.
class MaintenanceService {
  constructor(schedules, cars, timeProvider) {
    Object.assign(this, { schedules, cars, timeProvider });
  }
}

const SYSTEM_CLOCK = Object.freeze({ now: () => '2026-10-08T09:30:00' });

function buildServices() {
  return new ServiceCollection()
    .addScoped('FlemanDbContext', InMemoryDbContext)
    .addOpenGeneric('IGenericRepository', Lifetime.SCOPED,
      (entity, sp) => new GenericRepository(sp.getRequiredService('FlemanDbContext'), entity, `${entity[0].toLowerCase()}${entity.slice(1)}Id`))
    .addSingleton('TimeProvider', SYSTEM_CLOCK)
    .addScoped('IMaintenanceService', MaintenanceService,
      ['IGenericRepository<MaintenanceSchedule>', 'IGenericRepository<Car>', 'TimeProvider']);
}

describe('Dependency Injection: Service Registration and Constructor Injection', () => {
  test('D01 constructor injection resolves the whole dependency graph', () => {
    const scope = buildServices().build().createScope();
    const service = scope.getRequiredService('IMaintenanceService');

    assert.ok(service instanceof MaintenanceService);
    assert.ok(service.schedules instanceof GenericRepository);
    assert.ok(service.cars instanceof GenericRepository);
    assert.equal(service.timeProvider, SYSTEM_CLOCK);
  });

  test('D02 scoped: one instance per scope, a new one in each new scope', () => {
    const root = buildServices().build();
    const scopeA = root.createScope();
    const scopeB = root.createScope();

    assert.equal(scopeA.getRequiredService('FlemanDbContext'), scopeA.getRequiredService('FlemanDbContext'));
    assert.notEqual(scopeA.getRequiredService('FlemanDbContext'), scopeB.getRequiredService('FlemanDbContext'));
  });

  test('D03 services in one scope share that scope\'s DbContext (one unit of work per request)', async () => {
    const scope = buildServices().build().createScope();
    const service = scope.getRequiredService('IMaintenanceService');
    await service.cars.add({ carId: 0, vehicleNumber: 'MH01AA1111' });
    await service.schedules.saveChanges(); // saved through the *other* repository

    assert.equal((await service.cars.getAll()).length, 1);
  });

  test('D04 singleton: the same instance in every scope and on the root', () => {
    const root = buildServices().build();

    assert.equal(root.getRequiredService('TimeProvider'), SYSTEM_CLOCK);
    assert.equal(root.createScope().getRequiredService('TimeProvider'), root.createScope().getRequiredService('TimeProvider'));
  });

  test('D04b a class-registered singleton is built once and shared by all scopes (AddSingleton<IEmailBackgroundQueue, ...>)', () => {
    class EmailBackgroundQueue {}
    const root = new ServiceCollection().addSingleton('IEmailBackgroundQueue', EmailBackgroundQueue).build();
    const fromScopeA = root.createScope().getRequiredService('IEmailBackgroundQueue');

    assert.ok(fromScopeA instanceof EmailBackgroundQueue);
    assert.equal(fromScopeA, root.createScope().getRequiredService('IEmailBackgroundQueue'));
    assert.equal(fromScopeA, root.getRequiredService('IEmailBackgroundQueue'));
  });

  test('D05 transient: a new instance on every resolve', () => {
    class Stopwatch {}
    const scope = new ServiceCollection().addTransient('Stopwatch', Stopwatch).build().createScope();

    assert.notEqual(scope.getRequiredService('Stopwatch'), scope.getRequiredService('Stopwatch'));
  });

  test('D06 open generic closes per type argument', () => {
    const scope = buildServices().build().createScope();
    const cars = scope.getRequiredService('IGenericRepository<Car>');

    assert.equal(cars, scope.getRequiredService('IGenericRepository<Car>'));
    assert.notEqual(cars, scope.getRequiredService('IGenericRepository<Hub>'));
  });

  test('D07 an unregistered service fails with the .NET message', () => {
    const scope = buildServices().build().createScope();

    assert.throws(() => scope.getRequiredService('IBookingService'),
      { message: "No service for type 'IBookingService' has been registered." });
  });

  test('D08 a scoped service cannot be resolved from the root provider', () => {
    assert.throws(() => buildServices().build().getRequiredService('IMaintenanceService'),
      { message: "Cannot resolve scoped service 'IMaintenanceService' from root provider." });
  });

  test('D09 the last registration wins (swap JavaInvoicePdfService for InvoicePdfService)', () => {
    class InvoicePdfService {}
    class JavaInvoicePdfService {}
    const scope = new ServiceCollection()
      .addScoped('IInvoicePdfService', InvoicePdfService)
      .addScoped('IInvoicePdfService', JavaInvoicePdfService)
      .build().createScope();

    assert.ok(scope.getRequiredService('IInvoicePdfService') instanceof JavaInvoicePdfService);
  });
});
