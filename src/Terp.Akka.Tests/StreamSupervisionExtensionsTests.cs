using Akka.Event;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaLogLevel = Akka.Event.LogLevel;

namespace Terp.Akka.Tests;

public sealed class StreamSupervisionExtensionsTests : TestKit
{
    public sealed record LogEntry(AkkaLogLevel Level, Exception? Cause, string Message);

    public sealed class RecordingLoggingAdapter : LoggingAdapterBase
    {
        private readonly List<LogEntry> _entries = [];

        public RecordingLoggingAdapter()
            : base(DefaultLogMessageFormatter.Instance)
        {
        }

        public override bool IsDebugEnabled => true;

        public override bool IsInfoEnabled => true;

        public override bool IsWarningEnabled => true;

        public override bool IsErrorEnabled => true;

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_entries)
                {
                    return [.. _entries];
                }
            }
        }

        protected override void NotifyLog(AkkaLogLevel logLevel, object message, Exception? cause = null)
        {
            lock (_entries)
            {
                _entries.Add(new LogEntry(logLevel, cause, message.ToString() ?? string.Empty));
            }
        }
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    private static int FailOn(int value, int failing, Exception exception) =>
        value == failing ? throw exception : value;

    private async Task<IReadOnlyList<int>> RunAsync<TMat>(Source<int, TMat> source) =>
        await source.RunWith(Sink.Seq<int>(), Sys.Materializer())
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken).ConfigureAwait(false);

    [Fact]
    public async Task ResumeOnError_Source_ContinuesAfterException()
    {
        var source = Source.From([1, 2, 3, 4])
            .Select(x => FailOn(x, 2, new InvalidOperationException("boom")))
            .ResumeOnError();

        var result = await RunAsync(source);

        Assert.Equal([1, 3, 4], result);
    }

    [Fact]
    public async Task ResumeOnError_Flow_ContinuesAfterException()
    {
        var flow = Flow.Create<int>()
            .Select(x => FailOn(x, 3, new InvalidOperationException("boom")))
            .ResumeOnError();

        var result = await RunAsync(Source.From([1, 2, 3, 4]).Via(flow));

        Assert.Equal([1, 2, 4], result);
    }

    [Fact]
    public async Task ResumeOnError_Source_WithLog_TransientException_LogsWarning()
    {
        var log = new RecordingLoggingAdapter();
        var failure = new TimeoutException("slow");
        var source = Source.From([1, 2, 3])
            .Select(x => FailOn(x, 2, failure))
            .ResumeOnError(log);

        var result = await RunAsync(source);

        var entry = Assert.Single(log.Entries);
        Assert.Multiple(
            () => Assert.Equal([1, 3], result),
            () => Assert.Equal(AkkaLogLevel.WarningLevel, entry.Level),
            () => Assert.Same(failure, entry.Cause),
            () => Assert.Equal("Stream supervision: Resume for transient TimeoutException", entry.Message));
    }

    [Fact]
    public async Task ResumeOnError_Source_WithLog_NonTransientException_LogsError()
    {
        var log = new RecordingLoggingAdapter();
        var failure = new InvalidOperationException("boom");
        var source = Source.From([1, 2, 3])
            .Select(x => FailOn(x, 2, failure))
            .ResumeOnError(log);

        var result = await RunAsync(source);

        var entry = Assert.Single(log.Entries);
        Assert.Multiple(
            () => Assert.Equal([1, 3], result),
            () => Assert.Equal(AkkaLogLevel.ErrorLevel, entry.Level),
            () => Assert.Same(failure, entry.Cause),
            () => Assert.Equal("Stream supervision: Resume for unexpected InvalidOperationException — element dropped, state preserved", entry.Message));
    }

    [Fact]
    public async Task ResumeOnError_Source_WithLog_UnwrapsAggregateException()
    {
        var log = new RecordingLoggingAdapter();
        var inner = new TimeoutException("slow");
        var source = Source.From([1, 2, 3])
            .Select(x => FailOn(x, 2, new AggregateException(inner)))
            .ResumeOnError(log);

        var result = await RunAsync(source);

        var entry = Assert.Single(log.Entries);
        Assert.Multiple(
            () => Assert.Equal([1, 3], result),
            () => Assert.Equal(AkkaLogLevel.WarningLevel, entry.Level),
            () => Assert.Same(inner, entry.Cause));
    }

    [Fact]
    public void ResumeOnError_Source_WithNullLog_ThrowsArgumentNullException()
    {
        var source = Source.Single(1);

        var ex = Assert.Throws<ArgumentNullException>(() => source.ResumeOnError(null!));

        Assert.Equal("log", ex.ParamName);
    }

    [Fact]
    public void ResumeOnError_Flow_WithNullLog_ThrowsArgumentNullException()
    {
        var flow = Flow.Create<int>();

        var ex = Assert.Throws<ArgumentNullException>(() => flow.ResumeOnError(null!));

        Assert.Equal("log", ex.ParamName);
    }

    [Fact]
    public async Task ResumeOnError_Source_PreservesMaterializedValue()
    {
        var source = Source.From([1, 2]).MapMaterializedValue(_ => "mat").ResumeOnError();

        var (mat, completion) = source.ToMaterialized(Sink.Seq<int>(), Keep.Both).Run(Sys.Materializer());
        var result = await completion.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal("mat", mat),
            () => Assert.Equal([1, 2], result));
    }

    [Fact]
    public async Task ResumeOnError_Flow_PreservesMaterializedValue()
    {
        var flow = Flow.Create<int>().MapMaterializedValue(_ => "mat").ResumeOnError();

        var (mat, completion) = Source.From([1, 2])
            .ViaMaterialized(flow, Keep.Right)
            .ToMaterialized(Sink.Seq<int>(), Keep.Both)
            .Run(Sys.Materializer());
        var result = await completion.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal("mat", mat),
            () => Assert.Equal([1, 2], result));
    }
}
