using System.Diagnostics.Metrics;
using Terp.Diagnostics;

namespace Terp.Tests.Diagnostics;

public sealed class TimerScopeTests
{
    private const int Iterations = 200_000;

    private static readonly KeyValuePair<string, object?> _tagA = new("a", "1");
    private static readonly KeyValuePair<string, object?> _tagB = new("b", "2");

    private static KeyValuePair<string, object?> Tag(string key, object? value)
    {
        return new KeyValuePair<string, object?>(key, value);
    }

    private static long MeasureAllocatedBytes(Action action)
    {
        for (var i = 0; i < 1000; i++)
        {
            action();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Iterations; i++)
        {
            action();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static MeterListener CreateNoOpListener(Meter meter)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (i, l) =>
        {
            if (ReferenceEquals(i.Meter, meter))
            {
                l.EnableMeasurementEvents(i);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, _, _, _) => { });
        listener.Start();
        return listener;
    }

    [Fact]
    public void StartTimer_DisposedWithoutFail_RecordsSuccessOutcome()
    {
        using var meter = new Meter("test.timer.success");
        var histogram = meter.CreateHistogram<double>("op.duration", "s");
        using var collector = new MetricCollector(meter);
        var time = new ManualTimeProvider();

        using (histogram.StartTimer(time))
        {
            time.Advance(TimeSpan.FromMilliseconds(1500));
        }

        var m = Assert.Single(collector.For("op.duration"));
        Assert.Multiple(
            () => Assert.Equal(1.5, m.Value, 6),
            () => Assert.Equal("success", m.Tags["outcome"]));
    }

    [Fact]
    public void Fail_BeforeDispose_RecordsFailureOutcome()
    {
        using var meter = new Meter("test.timer.fail");
        var histogram = meter.CreateHistogram<double>("op.duration", "s");
        using var collector = new MetricCollector(meter);
        var time = new ManualTimeProvider();

        using (var timer = histogram.StartTimer(time))
        {
            time.Advance(TimeSpan.FromSeconds(2));
            timer.Fail();
        }

        var m = Assert.Single(collector.For("op.duration"));
        Assert.Multiple(
            () => Assert.Equal(2, m.Value, 6),
            () => Assert.Equal("failure", m.Tags["outcome"]));
    }

    [Fact]
    public void Dispose_CalledRepeatedly_RecordsOnlyOnce()
    {
        using var meter = new Meter("test.timer.idempotent");
        var histogram = meter.CreateHistogram<double>("op.duration", "s");
        using var collector = new MetricCollector(meter);
        var time = new ManualTimeProvider();

        var timer = histogram.StartTimer(time);
        timer.Dispose();
        timer.Fail();
        timer.Dispose();

        var m = Assert.Single(collector.For("op.duration"));
        Assert.Equal("success", m.Tags["outcome"]);
    }

    [Fact]
    public void Dispose_DefaultInstance_RecordsNothing()
    {
        var ex = Record.Exception(() =>
        {
            TimerScope scope = default;
            scope.Fail();
            scope.Dispose();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void StartTimer_WithOneTwoAndSpanTags_ForwardsTagsAndAddsOutcome()
    {
        using var meter = new Meter("test.timer.tags");
        var histogram = meter.CreateHistogram<double>("op.duration", "s");
        using var collector = new MetricCollector(meter);
        var time = new ManualTimeProvider();

        histogram.StartTimer(Tag("a", "1"), time).Dispose();
        histogram.StartTimer(Tag("a", "1"), Tag("b", "2"), time).Dispose();
        histogram.StartTimer([Tag("a", "1"), Tag("b", "2"), Tag("c", "3")], time).Dispose();

        var all = collector.For("op.duration");
        Assert.Equal(3, all.Count);
        Assert.Multiple(
            () => Assert.Equal(["a", "outcome"], all[0].Tags.Keys.Order()),
            () => Assert.Equal(["a", "b", "outcome"], all[1].Tags.Keys.Order()),
            () => Assert.Equal(["a", "b", "c", "outcome"], all[2].Tags.Keys.Order()),
            () => Assert.Equal("3", all[2].Tags["c"]));
    }

    [Fact]
    public void StartTimer_WithoutListener_DoesNotThrow()
    {
        using var meter = new Meter("test.timer.nolistener");
        var histogram = meter.CreateHistogram<double>("op.duration", "s");

        var ex = Record.Exception(() =>
        {
            using var timer = histogram.StartTimer();
            timer.Fail();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void StartTimer_NullHistogram_ThrowsArgumentNullException()
    {
        Histogram<double> histogram = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => histogram.StartTimer());

        Assert.Equal("histogram", ex.ParamName);
    }

    [Fact]
    public void StartTimer_WithZeroOneTwoOrSpanTags_AllocatesNothing()
    {
        using var meter = new Meter("test.alloc.timer");
        var histogram = meter.CreateHistogram<double>("op.duration", "s");
        using var listener = CreateNoOpListener(meter);

        var zero = MeasureAllocatedBytes(() => histogram.StartTimer().Dispose());
        var one = MeasureAllocatedBytes(() => histogram.StartTimer(_tagA).Dispose());
        var two = MeasureAllocatedBytes(() => histogram.StartTimer(_tagA, _tagB).Dispose());
        var span = MeasureAllocatedBytes(() => histogram.StartTimer([_tagA, _tagB]).Dispose());

        Console.WriteLine($"ALLOC timer bytes/{Iterations}: 0 tags={zero}, 1 tag={one}, 2 tags={two}, span(2)={span}");
        Assert.Multiple(
            () => Assert.Equal(0, zero),
            () => Assert.Equal(0, one),
            () => Assert.Equal(0, two),
            () => Assert.Equal(0, span));
    }
}
