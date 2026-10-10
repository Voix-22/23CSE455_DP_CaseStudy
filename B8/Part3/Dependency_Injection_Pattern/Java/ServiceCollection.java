package di;

import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.util.HashMap;
import java.util.Map;
import java.util.function.BiFunction;
import java.util.function.Function;

// Dependency Injection: "Service Registration and Constructor Injection"
// (port of the registrations in FlemanApi/Program.cs). A minimal
// Microsoft.Extensions.DependencyInjection: singleton / scoped / transient
// lifetimes, open-generic registration (IGenericRepository<,> ->
// GenericRepository<,>), last registration wins, and scope validation as in
// the Development environment.
//
// Tokens are strings because Java erases generic type arguments at run time:
// IGenericRepository<Car> and IGenericRepository<Hub> would be the same Class.
public final class ServiceCollection {
    record Registration(Lifetime lifetime, Function<ServiceProvider, Object> create) { }

    record OpenGeneric(Lifetime lifetime, BiFunction<String, ServiceProvider, Object> factory) { }

    private final Map<String, Registration> registrations = new HashMap<>();
    private final Map<String, OpenGeneric> openGenerics = new HashMap<>();

    public ServiceCollection addSingleton(String token, Class<?> implementation, String... deps) {
        return add(token, Lifetime.SINGLETON, implementation, deps);
    }

    // Like AddSingleton(TimeProvider.System): the instance is used as-is.
    // Deliberately not an addSingleton overload: Java resolves non-varargs
    // overloads first, so addSingleton("X", Foo.class) with no deps would
    // pick (String, Object) and register the Class object itself.
    public ServiceCollection addSingletonInstance(String token, Object instance) {
        registrations.put(token, new Registration(Lifetime.SINGLETON, sp -> instance));
        return this;
    }

    public ServiceCollection addScoped(String token, Class<?> implementation, String... deps) {
        return add(token, Lifetime.SCOPED, implementation, deps);
    }

    public ServiceCollection addTransient(String token, Class<?> implementation, String... deps) {
        return add(token, Lifetime.TRANSIENT, implementation, deps);
    }

    // Registers e.g. "IGenericRepository" so that "IGenericRepository<Car>"
    // resolves to factory.apply("Car", provider).
    public ServiceCollection addOpenGeneric(String token, Lifetime lifetime,
                                            BiFunction<String, ServiceProvider, Object> factory) {
        openGenerics.put(token, new OpenGeneric(lifetime, factory));
        return this;
    }

    public ServiceProvider build() {
        return new ServiceProvider(Map.copyOf(registrations), Map.copyOf(openGenerics), null);
    }

    // Constructor injection: the public constructor taking deps.length
    // arguments receives each dependency resolved by its token, in order.
    private ServiceCollection add(String token, Lifetime lifetime, Class<?> implementation, String[] deps) {
        Constructor<?> constructor = findConstructor(implementation, deps.length);
        registrations.put(token, new Registration(lifetime, sp -> {
            Object[] args = new Object[deps.length];
            for (int i = 0; i < deps.length; i++) args[i] = sp.getRequiredService(deps[i]);
            return newInstance(constructor, args);
        })); // last registration wins
        return this;
    }

    private static Constructor<?> findConstructor(Class<?> type, int arity) {
        for (Constructor<?> c : type.getConstructors()) {
            if (c.getParameterCount() == arity) return c;
        }
        throw new IllegalArgumentException(type.getName() + " has no public constructor with " + arity + " parameter(s)");
    }

    private static Object newInstance(Constructor<?> constructor, Object[] args) {
        try {
            return constructor.newInstance(args);
        } catch (InvocationTargetException e) {
            throw e.getCause() instanceof RuntimeException r ? r : new IllegalStateException(e.getCause());
        } catch (ReflectiveOperationException e) {
            throw new IllegalStateException(e);
        }
    }
}
