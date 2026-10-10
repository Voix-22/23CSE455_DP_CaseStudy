package di;

import java.util.HashMap;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

// The root provider owns singletons; each scope owns its scoped instances.
public final class ServiceProvider {
    private static final Pattern GENERIC_TOKEN = Pattern.compile("^(\\w+)<(\\w+)>$");

    private final Map<String, ServiceCollection.Registration> registrations;
    private final Map<String, ServiceCollection.OpenGeneric> openGenerics;
    private final ServiceProvider root; // null on the root itself
    private final Map<String, Object> instances = new HashMap<>();

    ServiceProvider(Map<String, ServiceCollection.Registration> registrations,
                    Map<String, ServiceCollection.OpenGeneric> openGenerics,
                    ServiceProvider root) {
        this.registrations = registrations;
        this.openGenerics = openGenerics;
        this.root = root;
    }

    public ServiceProvider createScope() {
        return new ServiceProvider(registrations, openGenerics, root != null ? root : this);
    }

    public Object getRequiredService(String token) {
        ServiceCollection.Registration registration = lookup(token);
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

    private Object getOrCreate(String token, ServiceCollection.Registration registration) {
        Object existing = instances.get(token);
        if (existing != null) return existing;
        Object created = registration.create().apply(this);
        instances.put(token, created);
        return created;
    }

    private ServiceCollection.Registration lookup(String token) {
        ServiceCollection.Registration exact = registrations.get(token);
        if (exact != null) return exact;
        Matcher m = GENERIC_TOKEN.matcher(token);
        if (!m.matches()) return null;
        ServiceCollection.OpenGeneric open = openGenerics.get(m.group(1));
        if (open == null) return null;
        String typeArg = m.group(2);
        return new ServiceCollection.Registration(open.lifetime(), sp -> open.factory().apply(typeArg, sp));
    }
}
