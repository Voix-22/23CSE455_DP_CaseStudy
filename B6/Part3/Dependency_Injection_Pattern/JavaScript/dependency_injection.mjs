// Dependency Injection: "Service Registration and Constructor Injection"
// (port of the registrations in FlemanApi/Program.cs and the constructor-
// injected services). A minimal Microsoft.Extensions.DependencyInjection:
// singleton / scoped / transient lifetimes, open-generic registration
// (IGenericRepository<,> -> GenericRepository<,>), last registration wins,
// and scope validation as in the Development environment.
//
// JavaScript has no constructor parameter types to reflect on, so each
// registration names its dependencies explicitly.

export const Lifetime = Object.freeze({ SINGLETON: 'singleton', SCOPED: 'scoped', TRANSIENT: 'transient' });

const GENERIC_TOKEN = /^(\w+)<(\w+)>$/;

export class ServiceCollection {
  #registrations = new Map();
  #openGenerics = new Map();

  addSingleton(token, implementation, deps = []) { return this.#add(token, Lifetime.SINGLETON, implementation, deps); }
  addScoped(token, implementation, deps = []) { return this.#add(token, Lifetime.SCOPED, implementation, deps); }
  addTransient(token, implementation, deps = []) { return this.#add(token, Lifetime.TRANSIENT, implementation, deps); }

  // Registers e.g. 'IGenericRepository' so that 'IGenericRepository<Car>'
  // resolves to factory('Car', provider).
  addOpenGeneric(token, lifetime, factory) {
    this.#openGenerics.set(token, { lifetime, factory });
    return this;
  }

  // An instance (not a class/function) registered as a singleton is used as-is,
  // like AddSingleton(TimeProvider.System).
  #add(token, lifetime, implementation, deps) {
    const create = typeof implementation === 'function'
      ? (sp) => new implementation(...deps.map((d) => sp.getRequiredService(d)))
      : () => implementation;
    this.#registrations.set(token, { lifetime, create }); // last registration wins
    return this;
  }

  build() {
    return new ServiceProvider(this.#registrations, this.#openGenerics, null);
  }
}

export class ServiceProvider {
  #registrations;
  #openGenerics;
  #root;
  #instances = new Map(); // singletons on the root, scoped instances on a scope

  constructor(registrations, openGenerics, root) {
    this.#registrations = registrations;
    this.#openGenerics = openGenerics;
    this.#root = root;
  }

  createScope() {
    return new ServiceProvider(this.#registrations, this.#openGenerics, this.#root ?? this);
  }

  getRequiredService(token) {
    const registration = this.#lookup(token);
    if (!registration) throw new Error(`No service for type '${token}' has been registered.`);

    if (registration.lifetime === Lifetime.TRANSIENT) return registration.create(this);

    if (registration.lifetime === Lifetime.SCOPED && this.#root === null) {
      throw new Error(`Cannot resolve scoped service '${token}' from root provider.`);
    }

    const owner = registration.lifetime === Lifetime.SINGLETON ? (this.#root ?? this) : this;
    return owner.#getOrCreate(token, registration);
  }

  #getOrCreate(token, registration) {
    if (!this.#instances.has(token)) this.#instances.set(token, registration.create(this));
    return this.#instances.get(token);
  }

  #lookup(token) {
    if (this.#registrations.has(token)) return this.#registrations.get(token);
    const match = GENERIC_TOKEN.exec(token);
    const open = match && this.#openGenerics.get(match[1]);
    return open ? { lifetime: open.lifetime, create: (sp) => open.factory(match[2], sp) } : null;
  }
}
