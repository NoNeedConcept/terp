using System.Diagnostics.Metrics;
using Terp.Diagnostics;

namespace Terp.Tests.Diagnostics;

public sealed class JobMetricsTests
{
    private const int Iterations = 100_000;
    private const long MaxBytesPerRun = 256;

    private static readonly string[] _instrumentNames = ["job.runs", "job.duration", "job.last_success_timestamp"];
    private static readonly KeyValuePair<string, object?> _tagA = new("a", "1");
    private static readonly KeyValuePair<string, object?> _tagB = new("b", "2");

    [Fact]
    public void Create_ValidMeter_CreatesThreeInstrumentsWithUnits()
    {
        using var meter = new Meter("test.job.create");
        var seen = new Dictionary<string, (string? Unit, Type Type)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (i, _) =>
        {
            if (ReferenceEquals(i.Meter, meter))
            {
                seen[i.Name] = (i.Unit, i.GetType().GetGenericTypeDefinition());
            }
        };
        listener.Start();

        JobMetrics.Create(meter, "job.x");

        Assert.Multiple(
            () => Assert.Equal(("{run}", typeof(Counter<>)), seen["job.x.runs"]),
            () => Assert.Equal(("s", typeof(Histogram<>)), seen["job.x.duration"]),
            () => Assert.Equal(("s", typeof(Gauge<>)), seen["job.x.last_success_timestamp"]));
    }

    [Fact]
    public void Create_NullMeter_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => JobMetrics.Create(null!, "x"));

        Assert.Equal("meter", ex.ParamName);
    }

    [Fact]
    public void Create_BlankName_ThrowsArgumentException()
    {
        using var meter = new Meter("test.job.args");

        var ex = Assert.Throws<ArgumentException>(() => JobMetrics.Create(meter, " "));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Succeed_AfterStart_RecordsRunDurationAndLastSuccess()
    {
        using var meter = new Meter("test.job.success");
        var time = new ManualTimeProvider();
        var jobs = JobMetrics.Create(meter, "job", time);
        using var collector = new MetricCollector(meter);

        using (var run = jobs.Start())
        {
            time.Advance(TimeSpan.FromSeconds(3));
            run.Succeed();
        }

        var runs = Assert.Single(collector.For("job.runs"));
        var duration = Assert.Single(collector.For("job.duration"));
        var last = Assert.Single(collector.For("job.last_success_timestamp"));
        Assert.Multiple(
            () => Assert.Equal(1, runs.Value),
            () => Assert.Equal("success", runs.Tags["outcome"]),
            () => Assert.False(runs.Tags.ContainsKey("error.type")),
            () => Assert.Equal(3, duration.Value, 6),
            () => Assert.Equal("success", duration.Tags["outcome"]),
            () => Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 3, TimeSpan.Zero).ToUnixTimeSeconds(), last.Value, 3),
            () => Assert.Empty(last.Tags));
    }

    [Fact]
    public void Fail_WithException_RecordsFailureWithErrorTypeAndKeepsLastSuccess()
    {
        using var meter = new Meter("test.job.fail");
        var time = new ManualTimeProvider();
        var jobs = JobMetrics.Create(meter, "job", time);
        using var collector = new MetricCollector(meter);

        using (var ok = jobs.Start())
        {
            ok.Succeed();
        }

        time.Advance(TimeSpan.FromMinutes(5));
        using (var bad = jobs.Start())
        {
            bad.Fail(new InvalidOperationException("boom"));
        }

        var runs = collector.For("job.runs");
        Assert.Equal(2, runs.Count);
        Assert.Multiple(
            () => Assert.Single(collector.For("job.last_success_timestamp")),
            () => Assert.Equal("failure", runs[1].Tags["outcome"]),
            () => Assert.Equal("System.InvalidOperationException", runs[1].Tags["error.type"]));
    }

    [Fact]
    public void Fail_WithoutException_RecordsUnknownErrorType()
    {
        using var meter = new Meter("test.job.unknown");
        var jobs = JobMetrics.Create(meter, "job", new ManualTimeProvider());
        using var collector = new MetricCollector(meter);

        using (var run = jobs.Start())
        {
            run.Fail();
        }

        var runs = Assert.Single(collector.For("job.runs"));
        Assert.Multiple(
            () => Assert.Equal("failure", runs.Tags["outcome"]),
            () => Assert.Equal("unknown", runs.Tags["error.type"]));
    }

    [Fact]
    public void Dispose_WithoutResult_RecordsAbandonedFailure()
    {
        using var meter = new Meter("test.job.abandoned");
        var jobs = JobMetrics.Create(meter, "job", new ManualTimeProvider());
        using var collector = new MetricCollector(meter);

        using (jobs.Start())
        {
        }

        var runs = Assert.Single(collector.For("job.runs"));
        Assert.Multiple(
            () => Assert.Equal("failure", runs.Tags["outcome"]),
            () => Assert.Equal("abandoned", runs.Tags["error.type"]),
            () => Assert.Empty(collector.For("job.last_success_timestamp")));
    }

    [Fact]
    public void Complete_CalledRepeatedly_RecordsOnlyFirstResult()
    {
        using var meter = new Meter("test.job.once");
        var jobs = JobMetrics.Create(meter, "job", new ManualTimeProvider());
        using var collector = new MetricCollector(meter);

        var run = jobs.Start();
        run.Succeed();
        run.Fail();
        run.Succeed();
        run.Dispose();
        run.Dispose();

        var runs = Assert.Single(collector.For("job.runs"));
        Assert.Multiple(
            () => Assert.Equal("success", runs.Tags["outcome"]),
            () => Assert.Single(collector.For("job.duration")),
            () => Assert.Single(collector.For("job.last_success_timestamp")));
    }

    [Fact]
    public void Succeed_RunPassedToOtherMethod_RecordsOnce()
    {
        using var meter = new Meter("test.job.shared");
        var jobs = JobMetrics.Create(meter, "job", new ManualTimeProvider());
        using var collector = new MetricCollector(meter);

        using (var run = jobs.Start())
        {
            CompleteElsewhere(run);
        }

        var runs = Assert.Single(collector.For("job.runs"));
        Assert.Equal("success", runs.Tags["outcome"]);
    }

    [Fact]
    public void Start_WithTags_AppliesTagsToAllInstruments()
    {
        using var meter = new Meter("test.job.tags");
        var jobs = JobMetrics.Create(meter, "job", new ManualTimeProvider());
        using var collector = new MetricCollector(meter);

        using (var run = jobs.Start(new("queue", "movies"), new("tier", "gold")))
        {
            run.Succeed();
        }

        var all = _instrumentNames
            .Select(name => Assert.Single(collector.For(name)))
            .ToList();
        Assert.Multiple(
            () => Assert.All(all, m => Assert.Equal("movies", m.Tags["queue"])),
            () => Assert.All(all, m => Assert.Equal("gold", m.Tags["tier"])));
    }

    [Fact]
    public void Start_WithOneTagAndSpanTags_AppliesTags()
    {
        using var meter = new Meter("test.job.tagoverloads");
        var jobs = JobMetrics.Create(meter, "job", new ManualTimeProvider());
        using var collector = new MetricCollector(meter);

        using (var one = jobs.Start(_tagA))
        {
            one.Succeed();
        }

        using (var many = jobs.Start([_tagA, _tagB, new("c", "3")]))
        {
            many.Succeed();
        }

        var runs = collector.For("job.runs");
        Assert.Equal(2, runs.Count);
        Assert.Multiple(
            () => Assert.Equal(["a", "outcome"], runs[0].Tags.Keys.Order()),
            () => Assert.Equal(["a", "b", "c", "outcome"], runs[1].Tags.Keys.Order()));
    }

    [Fact]
    public async Task Start_ParallelRuns_CountsExactly()
    {
        using var meter = new Meter("test.job.parallel");
        var jobs = JobMetrics.Create(meter, "job");
        using var collector = new MetricCollector(meter);

        await Parallel.ForAsync(0, 8000, TestContext.Current.CancellationToken, (i, _) =>
        {
            using var run = jobs.Start();
            if (i % 2 == 0)
            {
                run.Succeed();
            }
            else
            {
                run.Fail();
            }

            return ValueTask.CompletedTask;
        });

        var runs = collector.For("job.runs");
        Assert.Multiple(
            () => Assert.Equal(8000, runs.Count),
            () => Assert.Equal(4000, runs.Count(r => (string?)r.Tags["outcome"] == "success")),
            () => Assert.Equal(4000, runs.Count(r => (string?)r.Tags["outcome"] == "failure")),
            () => Assert.Equal(4000, collector.For("job.last_success_timestamp").Count));
    }

    [Fact]
    public void Start_WithListenerAttached_AllocatesOnlyTheRunObject()
    {
        using var meter = new Meter("test.alloc.job");
        var jobs = JobMetrics.Create(meter, "job");
        using var listener = new MeterListener();
        listener.InstrumentPublished = (i, l) =>
        {
            if (ReferenceEquals(i.Meter, meter))
            {
                l.EnableMeasurementEvents(i);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, _, _, _) => { });
        listener.SetMeasurementEventCallback<long>((_, _, _, _) => { });
        listener.Start();

        var plain = MeasureAllocatedBytes(() =>
        {
            using var run = jobs.Start();
            run.Succeed();
        });
        var tagged = MeasureAllocatedBytes(() =>
        {
            using var run = jobs.Start(_tagA, _tagB);
            run.Fail();
        });

        Console.WriteLine($"ALLOC job bytes/run over {Iterations} runs: start+succeed={plain / Iterations}, 2 tags start+fail={tagged / Iterations}");
        Assert.Multiple(
            () => Assert.InRange(plain, 1, MaxBytesPerRun * Iterations),
            () => Assert.InRange(tagged, 1, MaxBytesPerRun * Iterations));
    }

    private static void CompleteElsewhere(JobRun run)
    {
        run.Succeed();
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
}
