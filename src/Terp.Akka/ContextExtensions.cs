using Akka.Actor;
using Akka.Util;

namespace Terp.Akka;

/// <summary>
/// Extensions for looking up and messaging the children of an <see cref="IActorContext"/>.
/// </summary>
public static class ContextExtensions
{
    /// <summary>Gets the child actor with the given name.</summary>
    /// <param name="context">The context of the parent actor.</param>
    /// <param name="name">The child's name.</param>
    /// <returns>The child, or <see cref="Option{T}.None"/> if no such child exists.</returns>
    public static Option<IActorRef> GetChild(this IActorContext context, string name)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(name);

        var actorRef = context.Child(name);
        return actorRef.IsNobody() ? Option<IActorRef>.None : Option<IActorRef>.Create(actorRef);
    }

    /// <summary>Sends <paramref name="message"/> to the named child with the parent (<see cref="IActorContext.Self"/>) as sender.</summary>
    /// <param name="context">The context of the parent actor.</param>
    /// <param name="name">The child's name.</param>
    /// <param name="message">The message to send.</param>
    /// <returns><see langword="true"/> if the child exists and the message was sent; otherwise <see langword="false"/>.</returns>
    public static bool ChildTell(this IActorContext context, string name, object message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(message);

        var child = context.Child(name);
        if (child.IsNobody())
        {
            return false;
        }

        child.Tell(message, context.Self);
        return true;
    }

    /// <summary>Forwards <paramref name="message"/> to the named child, preserving the original sender of the message being processed.</summary>
    /// <param name="context">The context of the parent actor.</param>
    /// <param name="name">The child's name.</param>
    /// <param name="message">The message to forward.</param>
    /// <returns><see langword="true"/> if the child exists and the message was forwarded; otherwise <see langword="false"/>.</returns>
    public static bool ChildForward(this IActorContext context, string name, object message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(message);

        var child = context.Child(name);
        if (child.IsNobody())
        {
            return false;
        }

        child.Tell(message, context.Sender);
        return true;
    }
}
