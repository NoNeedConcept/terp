using Akka.Actor;
using Akka.DependencyInjection;

namespace Terp.Akka;

/// <summary>
/// Extensions for creating actors whose dependencies are resolved through <see cref="DependencyResolver"/>.
/// </summary>
public static class ResolveExtensions
{
    /// <summary>Creates a named child actor of the current actor, resolving its dependencies from DI.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="context">The context of the parent actor.</param>
    /// <param name="name">The child's name, or <see langword="null"/> for a generated name.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new child actor.</returns>
    public static IActorRef ResolveChildActor<TActor>(this IActorContext context, string? name, params object[] args)
        where TActor : ActorBase
    {
        ArgumentNullException.ThrowIfNull(context);

        var resolver = DependencyResolver.For(context.System);
        return Resolve<TActor>(context, resolver, name, configure: null, args);
    }

    /// <summary>Creates an unnamed child actor of the current actor, resolving its dependencies from DI.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="context">The context of the parent actor.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new child actor.</returns>
    public static IActorRef ResolveChildActor<TActor>(this IActorContext context, params object[] args)
        where TActor : ActorBase
    {
        return context.ResolveChildActor<TActor>((string?)null, args);
    }

    /// <summary>Creates a child actor, resolving its dependencies from DI and customizing its <see cref="Props"/> via <paramref name="configure"/>.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="context">The context of the parent actor.</param>
    /// <param name="name">The child's name, or <see langword="null"/> for a generated name.</param>
    /// <param name="configure">Adjusts the props before the actor is created.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new child actor.</returns>
    public static IActorRef ResolveChildActor<TActor>(this IActorContext context, string? name,
        Func<Props, Props> configure, params object[] args)
        where TActor : ActorBase
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configure);

        var resolver = DependencyResolver.For(context.System);
        return Resolve<TActor>(context, resolver, name, configure, args);
    }

    /// <summary>Creates an unnamed child actor, resolving its dependencies from DI and customizing its <see cref="Props"/> via <paramref name="configure"/>.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="context">The context of the parent actor.</param>
    /// <param name="configure">Adjusts the props before the actor is created.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new child actor.</returns>
    public static IActorRef ResolveChildActor<TActor>(this IActorContext context,
        Func<Props, Props> configure, params object[] args)
        where TActor : ActorBase
    {
        return context.ResolveChildActor<TActor>(null, configure, args);
    }

    /// <summary>Creates a named top-level actor, resolving its dependencies from DI.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="context">The actor context whose system hosts the new actor.</param>
    /// <param name="name">The actor's name, or <see langword="null"/> for a generated name.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new actor.</returns>
    public static IActorRef ResolveActor<TActor>(this IActorContext context, string? name, params object[] args)
        where TActor : ActorBase
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.System.ResolveActor<TActor>(name, args);
    }

    /// <summary>Creates an unnamed top-level actor, resolving its dependencies from DI.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="context">The actor context whose system hosts the new actor.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new actor.</returns>
    public static IActorRef ResolveActor<TActor>(this IActorContext context, params object[] args)
        where TActor : ActorBase
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.System.ResolveActor<TActor>(args);
    }

    /// <summary>Creates an unnamed top-level actor, resolving its dependencies from DI.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="system">The actor system that hosts the new actor.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new actor.</returns>
    public static IActorRef ResolveActor<TActor>(this ActorSystem system, params object[] args)
        where TActor : ActorBase
    {
        return system.ResolveActor<TActor>((string?)null, args);
    }

    /// <summary>Creates a named top-level actor, resolving its dependencies from DI.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="system">The actor system that hosts the new actor.</param>
    /// <param name="name">The actor's name, or <see langword="null"/> for a generated name.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new actor.</returns>
    public static IActorRef ResolveActor<TActor>(this ActorSystem system, string? name, params object[] args)
        where TActor : ActorBase
    {
        ArgumentNullException.ThrowIfNull(system);

        var resolver = DependencyResolver.For(system);
        return Resolve<TActor>(system, resolver, name, configure: null, args);
    }

    /// <summary>Creates a top-level actor, resolving its dependencies from DI and customizing its <see cref="Props"/> via <paramref name="configure"/>.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="system">The actor system that hosts the new actor.</param>
    /// <param name="name">The actor's name, or <see langword="null"/> for a generated name.</param>
    /// <param name="configure">Adjusts the props before the actor is created.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new actor.</returns>
    public static IActorRef ResolveActor<TActor>(this ActorSystem system, string? name,
        Func<Props, Props> configure, params object[] args)
        where TActor : ActorBase
    {
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(configure);

        var resolver = DependencyResolver.For(system);
        return Resolve<TActor>(system, resolver, name, configure, args);
    }

    /// <summary>Creates an unnamed top-level actor, resolving its dependencies from DI and customizing its <see cref="Props"/> via <paramref name="configure"/>.</summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <param name="system">The actor system that hosts the new actor.</param>
    /// <param name="configure">Adjusts the props before the actor is created.</param>
    /// <param name="args">Additional constructor arguments that are not resolved from DI.</param>
    /// <returns>The new actor.</returns>
    public static IActorRef ResolveActor<TActor>(this ActorSystem system,
        Func<Props, Props> configure, params object[] args)
        where TActor : ActorBase
    {
        return system.ResolveActor<TActor>(null, configure, args);
    }

    private static IActorRef Resolve<TActor>(IActorRefFactory factory,
        DependencyResolver resolver, string? name, Func<Props, Props>? configure, params object[] args)
        where TActor : ActorBase
    {
        var props = resolver.Props<TActor>(args);
        if (configure is not null)
        {
            props = configure(props);
        }

        return factory.ActorOf(props, name);
    }
}
