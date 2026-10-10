"""Dependency Injection: "Service Registration and Constructor Injection".

Port of the registrations in FlemanApi/Program.cs and the constructor-injected
services. A minimal Microsoft.Extensions.DependencyInjection: singleton /
scoped / transient lifetimes, open-generic registration
(IGenericRepository<,> -> GenericRepository<,>), last registration wins, and
scope validation as in the Development environment.

Each registration names its dependency tokens explicitly, as in the other
three languages; Python could instead read them from constructor type hints
(inspect.signature), but that would make the four ports diverge.
"""
from __future__ import annotations

import re
from dataclasses import dataclass
from enum import Enum
from typing import Any, Callable


class Lifetime(Enum):
    SINGLETON = "singleton"
    SCOPED = "scoped"
    TRANSIENT = "transient"


@dataclass(frozen=True)
class _Registration:
    lifetime: Lifetime
    create: Callable[["ServiceProvider"], Any]


_GENERIC_TOKEN = re.compile(r"^(\w+)<(\w+)>$")


class ServiceCollection:
    def __init__(self) -> None:
        self._registrations: dict[str, _Registration] = {}
        self._open_generics: dict[str, tuple[Lifetime, Callable[[str, ServiceProvider], Any]]] = {}

    def add_singleton(self, token: str, implementation: type, *deps: str) -> ServiceCollection:
        return self._add(token, Lifetime.SINGLETON, implementation, deps)

    def add_singleton_instance(self, token: str, instance: Any) -> ServiceCollection:
        """Like AddSingleton(TimeProvider.System): the instance is used as-is."""
        self._registrations[token] = _Registration(Lifetime.SINGLETON, lambda sp: instance)
        return self

    def add_scoped(self, token: str, implementation: type, *deps: str) -> ServiceCollection:
        return self._add(token, Lifetime.SCOPED, implementation, deps)

    def add_transient(self, token: str, implementation: type, *deps: str) -> ServiceCollection:
        return self._add(token, Lifetime.TRANSIENT, implementation, deps)

    def add_open_generic(self, token: str, lifetime: Lifetime,
                         factory: Callable[[str, ServiceProvider], Any]) -> ServiceCollection:
        """Registers e.g. 'IGenericRepository' so that 'IGenericRepository<Car>'
        resolves to factory('Car', provider)."""
        self._open_generics[token] = (lifetime, factory)
        return self

    def build(self) -> ServiceProvider:
        return ServiceProvider(dict(self._registrations), dict(self._open_generics), root=None)

    def _add(self, token: str, lifetime: Lifetime, implementation: type, deps: tuple[str, ...]) -> ServiceCollection:
        # Constructor injection: each dependency is resolved by its token, in order.
        create = lambda sp: implementation(*(sp.get_required_service(d) for d in deps))  # noqa: E731
        self._registrations[token] = _Registration(lifetime, create)  # last registration wins
        return self


class ServiceProvider:
    """The root provider owns singletons; each scope owns its scoped instances."""

    def __init__(self, registrations, open_generics, root: ServiceProvider | None) -> None:
        self._registrations = registrations
        self._open_generics = open_generics
        self._root = root
        self._instances: dict[str, Any] = {}

    def create_scope(self) -> ServiceProvider:
        return ServiceProvider(self._registrations, self._open_generics, self._root or self)

    def get_required_service(self, token: str) -> Any:
        registration = self._lookup(token)
        if registration is None:
            raise LookupError(f"No service for type '{token}' has been registered.")

        if registration.lifetime is Lifetime.TRANSIENT:
            return registration.create(self)

        if registration.lifetime is Lifetime.SCOPED and self._root is None:
            raise LookupError(f"Cannot resolve scoped service '{token}' from root provider.")

        owner = (self._root or self) if registration.lifetime is Lifetime.SINGLETON else self
        if token not in owner._instances:
            owner._instances[token] = registration.create(owner)
        return owner._instances[token]

    def _lookup(self, token: str) -> _Registration | None:
        if token in self._registrations:
            return self._registrations[token]
        match = _GENERIC_TOKEN.match(token)
        open_generic = match and self._open_generics.get(match.group(1))
        if not open_generic:
            return None
        lifetime, factory = open_generic
        type_arg = match.group(2)
        return _Registration(lifetime, lambda sp: factory(type_arg, sp))
