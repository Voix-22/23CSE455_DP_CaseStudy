// Dependency Injection: "Service Registration and Constructor Injection"
// (port of the registrations in FlemanApi/Program.cs). A minimal
// Microsoft.Extensions.DependencyInjection: singleton / scoped / transient
// lifetimes, open-generic registration (IGenericRepository<,> ->
// GenericRepository<,>), last registration wins, and scope validation as in
// the Development environment. C++17, header-only.
//
// C++ has no run-time reflection, so constructor injection is spelled out at
// compile time: add<Service, Impl, Deps...>(token, {depTokens...}) builds
// make_shared<Impl>(get<Deps>(depToken)...). Instances are stored type-erased
// (shared_ptr<void>) together with the Service type they were registered as,
// and get<T>() checks that type before casting.
#pragma once

#include <functional>
#include <map>
#include <memory>
#include <regex>
#include <stdexcept>
#include <string>
#include <typeindex>
#include <utility>
#include <vector>

namespace di {

enum class Lifetime { Singleton, Scoped, Transient };

class ServiceProvider;

struct Registration {
    Lifetime lifetime;
    std::type_index type;  // the Service type the instance is exposed as
    std::function<std::shared_ptr<void>(ServiceProvider&)> create;
};

struct OpenGeneric {
    Lifetime lifetime;
    std::type_index type;
    std::function<std::shared_ptr<void>(const std::string& typeArg, ServiceProvider&)> factory;
};

struct Registry {
    std::map<std::string, Registration> registrations;
    std::map<std::string, OpenGeneric> openGenerics;
};

// The root provider owns singletons; each scope owns its scoped instances.
class ServiceProvider {
public:
    explicit ServiceProvider(std::shared_ptr<const Registry> registry, ServiceProvider* root = nullptr)
        : registry_(std::move(registry)), root_(root) {}

    // The root must outlive its scopes, as with .NET's IServiceScope.
    ServiceProvider createScope() { return ServiceProvider(registry_, root_ ? root_ : this); }

    template <class T>
    std::shared_ptr<T> getRequiredService(const std::string& token) {
        const Registration registration = lookup(token);
        if (registration.type != std::type_index(typeid(T)))
            throw std::logic_error("Service '" + token + "' is not registered as the requested type.");
        return std::static_pointer_cast<T>(resolve(token, registration));
    }

private:
    std::shared_ptr<void> resolve(const std::string& token, const Registration& registration) {
        if (registration.lifetime == Lifetime::Transient) return registration.create(*this);

        if (registration.lifetime == Lifetime::Scoped && root_ == nullptr)
            throw std::logic_error("Cannot resolve scoped service '" + token + "' from root provider.");

        ServiceProvider& owner = registration.lifetime == Lifetime::Singleton && root_ ? *root_ : *this;
        auto found = owner.instances_.find(token);
        if (found != owner.instances_.end()) return found->second;
        auto created = registration.create(owner);
        owner.instances_.emplace(token, created);
        return created;
    }

    Registration lookup(const std::string& token) const {
        auto exact = registry_->registrations.find(token);
        if (exact != registry_->registrations.end()) return exact->second;

        static const std::regex genericToken(R"(^(\w+)<(\w+)>$)");
        std::smatch m;
        if (std::regex_match(token, m, genericToken)) {
            auto open = registry_->openGenerics.find(m[1].str());
            if (open != registry_->openGenerics.end()) {
                std::string typeArg = m[2].str();
                auto factory = open->second.factory;
                return {open->second.lifetime, open->second.type,
                        [factory, typeArg](ServiceProvider& sp) { return factory(typeArg, sp); }};
            }
        }
        throw std::logic_error("No service for type '" + token + "' has been registered.");
    }

    std::shared_ptr<const Registry> registry_;
    ServiceProvider* root_;  // nullptr on the root itself
    std::map<std::string, std::shared_ptr<void>> instances_;
};

class ServiceCollection {
public:
    template <class Service, class Impl = Service, class... Deps>
    ServiceCollection& addSingleton(const std::string& token, std::vector<std::string> deps = {}) {
        return add<Service, Impl, Deps...>(token, Lifetime::Singleton, std::move(deps));
    }

    // Like AddSingleton(TimeProvider.System): the instance is used as-is.
    template <class Service>
    ServiceCollection& addSingletonInstance(const std::string& token, std::shared_ptr<Service> instance) {
        registry_->registrations.insert_or_assign(
            token, Registration{Lifetime::Singleton, typeid(Service), [instance](ServiceProvider&) { return instance; }});
        return *this;
    }

    template <class Service, class Impl = Service, class... Deps>
    ServiceCollection& addScoped(const std::string& token, std::vector<std::string> deps = {}) {
        return add<Service, Impl, Deps...>(token, Lifetime::Scoped, std::move(deps));
    }

    template <class Service, class Impl = Service, class... Deps>
    ServiceCollection& addTransient(const std::string& token, std::vector<std::string> deps = {}) {
        return add<Service, Impl, Deps...>(token, Lifetime::Transient, std::move(deps));
    }

    // Registers e.g. "IGenericRepository" so that "IGenericRepository<Car>"
    // resolves to factory("Car", provider).
    template <class Service>
    ServiceCollection& addOpenGeneric(
        const std::string& token, Lifetime lifetime,
        std::function<std::shared_ptr<Service>(const std::string&, ServiceProvider&)> factory) {
        registry_->openGenerics.insert_or_assign(
            token, OpenGeneric{lifetime, typeid(Service),
                               [factory](const std::string& arg, ServiceProvider& sp) -> std::shared_ptr<void> {
                                   return factory(arg, sp);
                               }});
        return *this;
    }

    ServiceProvider build() const { return ServiceProvider(std::make_shared<const Registry>(*registry_)); }

private:
    template <class Service, class Impl, class... Deps>
    ServiceCollection& add(const std::string& token, Lifetime lifetime, std::vector<std::string> deps) {
        if (deps.size() != sizeof...(Deps))
            throw std::invalid_argument("dependency token count does not match the constructor's dependency types");
        auto create = [deps](ServiceProvider& sp) -> std::shared_ptr<void> {
            return construct<Service, Impl, Deps...>(sp, deps, std::index_sequence_for<Deps...>{});
        };
        registry_->registrations.insert_or_assign(token, Registration{lifetime, typeid(Service), create});  // last wins
        return *this;
    }

    // Constructor injection: Impl(get<Deps[0]>(deps[0]), get<Deps[1]>(deps[1]), ...).
    template <class Service, class Impl, class... Deps, std::size_t... I>
    static std::shared_ptr<Service> construct(ServiceProvider& sp, const std::vector<std::string>& deps,
                                              std::index_sequence<I...>) {
        return std::make_shared<Impl>(sp.getRequiredService<Deps>(deps[I])...);
    }

    std::shared_ptr<Registry> registry_ = std::make_shared<Registry>();
};

}  // namespace di
