using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Supervision;
using StreamDecider = Akka.Streams.Supervision.Decider;
using StreamDirective = Akka.Streams.Supervision.Directive;

namespace Terp.Akka;

/// <summary>
/// Extensions for applying a resuming supervision strategy to <see cref="Source{TOut, TMat}"/> and <see cref="Flow{TIn, TOut, TMat}"/> stages.
/// </summary>
public static class StreamSupervisionExtensions
{
    /// <summary>
    /// Resumes the stream after a stage throws: the failing element is dropped and the stage state is preserved.
    /// </summary>
    /// <typeparam name="TOut">The element type of the source.</typeparam>
    /// <typeparam name="TMat">The materialized value type of the source.</typeparam>
    /// <param name="source">The source to supervise.</param>
    /// <returns>The source with a resuming supervision strategy.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Source<TOut, TMat> ResumeOnError<TOut, TMat>(this Source<TOut, TMat> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider));
    }

    /// <summary>
    /// Resumes the stream after a stage throws and logs the failure: transient exceptions as warnings, all others as errors.
    /// The failing element is dropped and the stage state is preserved.
    /// </summary>
    /// <typeparam name="TOut">The element type of the source.</typeparam>
    /// <typeparam name="TMat">The materialized value type of the source.</typeparam>
    /// <param name="source">The source to supervise.</param>
    /// <param name="log">The logger that receives the failures.</param>
    /// <returns>The source with a resuming, logging supervision strategy.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="log"/> is <see langword="null"/>.</exception>
    public static Source<TOut, TMat> ResumeOnError<TOut, TMat>(this Source<TOut, TMat> source, ILoggingAdapter log)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(log);

        return source.WithAttributes(ActorAttributes.CreateSupervisionStrategy(CreateLoggingDecider(log)));
    }

    /// <summary>
    /// Resumes the stream after a stage throws: the failing element is dropped and the stage state is preserved.
    /// </summary>
    /// <typeparam name="TIn">The input element type of the flow.</typeparam>
    /// <typeparam name="TOut">The output element type of the flow.</typeparam>
    /// <typeparam name="TMat">The materialized value type of the flow.</typeparam>
    /// <param name="flow">The flow to supervise.</param>
    /// <returns>The flow with a resuming supervision strategy.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="flow"/> is <see langword="null"/>.</exception>
    public static Flow<TIn, TOut, TMat> ResumeOnError<TIn, TOut, TMat>(this Flow<TIn, TOut, TMat> flow)
    {
        ArgumentNullException.ThrowIfNull(flow);

        return flow.WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider));
    }

    /// <summary>
    /// Resumes the stream after a stage throws and logs the failure: transient exceptions as warnings, all others as errors.
    /// The failing element is dropped and the stage state is preserved.
    /// </summary>
    /// <typeparam name="TIn">The input element type of the flow.</typeparam>
    /// <typeparam name="TOut">The output element type of the flow.</typeparam>
    /// <typeparam name="TMat">The materialized value type of the flow.</typeparam>
    /// <param name="flow">The flow to supervise.</param>
    /// <param name="log">The logger that receives the failures.</param>
    /// <returns>The flow with a resuming, logging supervision strategy.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="flow"/> or <paramref name="log"/> is <see langword="null"/>.</exception>
    public static Flow<TIn, TOut, TMat> ResumeOnError<TIn, TOut, TMat>(this Flow<TIn, TOut, TMat> flow, ILoggingAdapter log)
    {
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(log);

        return flow.WithAttributes(ActorAttributes.CreateSupervisionStrategy(CreateLoggingDecider(log)));
    }

    internal static Exception Unwrap(Exception exception)
    {
        while (exception is AggregateException { InnerExceptions.Count: 1 } aggregate)
        {
            exception = aggregate.InnerExceptions[0];
        }

        return exception;
    }

    internal static bool IsTransient(Exception exception) =>
        exception is AskTimeoutException
            or OperationCanceledException
            or TimeoutException
            or HttpRequestException;

    private static StreamDecider CreateLoggingDecider(ILoggingAdapter log) => exception =>
    {
        var cause = Unwrap(exception);
        if (IsTransient(cause))
        {
            log.Warning(cause, "Stream supervision: Resume for transient {0}", cause.GetType().Name);
        }
        else
        {
            log.Error(cause, "Stream supervision: Resume for unexpected {0} — element dropped, state preserved", cause.GetType().Name);
        }

        return StreamDirective.Resume;
    };
}
