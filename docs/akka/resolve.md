---
title: Resolve actors
description: Create top-level and child actors whose constructor dependencies come from dependency injection.
---

# Resolve actors

`ResolveExtensions` (package `Terp.Akka`) creates actors through Akka's `DependencyResolver`. The actor constructor gets its services from DI, and you can pass extra arguments that DI does not know.

```csharp
using Akka.Actor;
using Terp.Akka;
```

The actor type must derive from `ActorBase`, for example `ReceiveActor`.

## Top-level actors

`ResolveActor<TActor>` is available on `ActorSystem` and on `IActorContext`. On a context it creates the actor at the top level of the system, not as a child.

```csharp
IActorRef orders = system.ResolveActor<OrderManager>("orders");
IActorRef unnamed = system.ResolveActor<OrderManager>();
```

## Child actors

`ResolveChildActor<TActor>` is available on `IActorContext` and creates a child of the current actor.

```csharp
internal sealed class OrderManager : ReceiveActor
{
    public OrderManager()
    {
        var worker = Context.ResolveChildActor<OrderWorker>("worker");
        Receive<PlaceOrder>(m => worker.Forward(m));
    }
}
```

## Name, arguments and props

All overloads follow the same pattern. Each has a named and an unnamed form, and both have a variant that customises the `Props` before the actor is created.

| Shape | Example |
|-------|---------|
| unnamed | `Context.ResolveChildActor<T>()` |
| named | `Context.ResolveChildActor<T>("worker")` |
| unnamed, with props | `Context.ResolveChildActor<T>(p => p.WithDispatcher("my-dispatcher"))` |
| named, with props | `Context.ResolveChildActor<T>("worker", p => p.WithRouter(new RoundRobinPool(5)))` |

The `configure` function is `Func<Props, Props>`. Arguments that follow are `params object[] args`: constructor arguments that are not resolved from DI. They are passed to `DependencyResolver.Props<TActor>(args)`.

```csharp
var pool = Context.ResolveChildActor<TvdbWorker>(
    "tvdb-pool",
    props => props.WithRouter(new RoundRobinPool(4)),
    apiKey);                       // constructor argument outside DI
```

::: info A string first argument is the name
`ResolveActor<T>("x")` reads `"x"` as the actor name. To pass a string constructor argument without a name, write `ResolveActor<T>((string?)null, "x")`.
:::

A first argument of type `string` is always read as the actor name. A `null` name gives the actor a generated name. Child names must be unique among the siblings, as in plain Akka.

## Where the dependencies come from

The resolver is `DependencyResolver.For(system)`, which Akka.Hosting sets up when you register the system with [`ActorSystemSetup`](/akka/actor-system) or `AddAkka`. Services are resolved from the application's service provider. Akka calls the actor constructor outside of any request scope, so register actor dependencies as singletons or transients; a scoped service cannot be resolved from the root provider.

::: info Lifetimes
The constructor is called by Akka, outside of any request scope. Register what actors need as singletons or transients. A scoped service cannot be resolved from the root provider.
:::

## Overload list

| Extension on | Methods |
|--------------|---------|
| `ActorSystem` | `ResolveActor<T>(args)`, `ResolveActor<T>(name, args)`, `ResolveActor<T>(configure, args)`, `ResolveActor<T>(name, configure, args)` |
| `IActorContext` | `ResolveActor<T>(args)`, `ResolveActor<T>(name, args)` |
| `IActorContext` | `ResolveChildActor<T>(args)`, `ResolveChildActor<T>(name, args)`, `ResolveChildActor<T>(configure, args)`, `ResolveChildActor<T>(name, configure, args)` |
