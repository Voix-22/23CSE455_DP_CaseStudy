package di;

import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.util.HashMap;
import java.util.Map;
import java.util.function.BiFunction;
import java.util.function.Function;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

// Dependency Injection: "Service Registration and Constructor Injection"
// (port of the registrations in FlemanApi/Program.cs). A minimal
// Microsoft.Extensions.DependencyInjection: singleton / scoped / transient
// lifetimes, open-generic registration (IGenericRepository<,> ->
// GenericRepository<,>), last registration wins, and scope validation as in
// the Development environment.
//
// One file, like dependency_injection.{mjs,py,hpp} in the other ports: Java
// allows one public top-level class per file, so the three participants are
// nested classes of DependencyInjection.
//
// Tokens are strings because Java erases generic type arguments at run time:
// IGenericRepository<Car> and IGenericRepository<Hub> would be the same Class.
public final class DependencyInjection {
    private DependencyInjection() { }

    // Mirrors Microsoft.Extensions.DependencyInjection.ServiceLifetime.
    public enum Lifetime { SINGLETON, SCOPED, TRANSIENT }

    private record Registration(Lifetime lifetime, Function<ServiceProvider, Object> create) { }

    private record OpenGeneric(Lifetime lifetime, BiFunction<String, ServiceProvider, Object> factory) { }

    // Registration API: the Program.cs role.
    public static final class ServiceCollection {
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

    // The root provider owns singletons; each scope owns its scoped instances.
    public static final class ServiceProvider {
        private static final Pattern GENERIC_TOKEN = Pattern.compile("^(\\w+)<(\\w+)>$");

        private final Map<String, Registration> registrations;
        private final Map<String, OpenGeneric> openGenerics;
        private final ServiceProvider root; // null on the root itself
        private final Map<String, Object> instances = new HashMap<>();

        private ServiceProvider(Map<String, Registration> registrations, Map<String, OpenGeneric> openGenerics,
                                ServiceProvider root) {
            this.registrations = registrations;
            this.openGenerics = openGenerics;
            this.root = root;
        }

        public ServiceProvider createScope() {
            return new ServiceProvider(registrations, openGenerics, root != null ? root : this);
        }

        public Object getRequiredService(String token) {
            Registration registration = lookup(token);
            if (registration == null)
                throw new IllegalStateException("No service for type '" + token + "' has been registered.");

            if (registration.lifetime() == Lifetime.TRANSIENT) return registration.create().apply(this);

            if (registration.lifetime() == Lifetime.SCOPED && root == null)
                throw new IllegalStateException("Cannot resolve scoped service '" + token + "' from root provider.");

            ServiceProvider owner = registration.lifetime() == Lifetime.SINGLETON && root != null ? root : this;
            return owner.getOrCreate(token, registration);
        }

        // Typed convenience: the cast that .NET's GetRequiredService<T>() does for you.
        public <T> T getRequiredService(String token, Class<T> type) {
            return type.cast(getRequiredService(token));
        }

        private Object getOrCreate(String token, Registration registration) {
            Object existing = instances.get(token);
            if (existing != null) return existing;
            Object created = registration.create().apply(this);
            instances.put(token, created);
            return created;
        }

        private Registration lookup(String token) {
            Registration exact = registrations.get(token);
            if (exact != null) return exact;
            Matcher m = GENERIC_TOKEN.matcher(token);
            if (!m.matches()) return null;
            OpenGeneric open = openGenerics.get(m.group(1));
            if (open == null) return null;
            String typeArg = m.group(2);
            return new Registration(open.lifetime(), sp -> open.factory().apply(typeArg, sp));
        }
    }
}
