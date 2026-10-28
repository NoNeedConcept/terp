using System.Diagnostics.Metrics;
using Terp.Diagnostics;

namespace Terp.Tests.Diagnostics;

public sealed class CounterExtensionsTests
{
    private static KeyValuePair<string, object?> Tag(string key, object? value)
    {
        return new KeyValuePair<string, object?>(key, value);
    }

    public sealed class CounterUpTests
    {
        [Fact]
        public void Up_NoTags_RecordsOne()
        {
            using var meter = new Meter("test.counter.up.notags");
            var counter = meter.CreateCounter<long>("requests");
            using var collector = new MetricCollector(meter);

            counter.Up();

            var m = Assert.Single(collector.For("requests"));
            Assert.Equal(1, m.Value);
        }

        [Fact]
        public void Up_OneTag_RecordsOneWithTag()
        {
            using var meter = new Meter("test.counter.up.1tag");
            var counter = meter.CreateCounter<long>("requests");
            using var collector = new MetricCollector(meter);

            counter.Up(Tag("api", "tmdb"));

            var m = Assert.Single(collector.For("requests"));
            Assert.Multiple(
                () => Assert.Equal(1, m.Value),
                () => Assert.Equal("tmdb", m.Tags["api"]));
        }

        [Fact]
        public void Up_TwoTags_RecordsOneWithBothTags()
        {
            using var meter = new Meter("test.counter.up.2tags");
            var counter = meter.CreateCounter<long>("requests");
            using var collector = new MetricCollector(meter);

            counter.Up(Tag("api", "tmdb"), Tag("status", "hit"));

            var m = Assert.Single(collector.For("requests"));
            Assert.Multiple(
                () => Assert.Equal(1, m.Value),
                () => Assert.Equal("tmdb", m.Tags["api"]),
                () => Assert.Equal("hit", m.Tags["status"]));
        }

        [Fact]
        public void Up_SpanTags_RecordsOneWithAllTags()
        {
            using var meter = new Meter("test.counter.up.span");
            var counter = meter.CreateCounter<long>("requests");
            using var collector = new MetricCollector(meter);

            ReadOnlySpan<KeyValuePair<string, object?>> tags = [Tag("a", "1"), Tag("b", "2"), Tag("c", "3")];
            counter.Up(tags);

            var m = Assert.Single(collector.For("requests"));
            Assert.Multiple(
                () => Assert.Equal(1, m.Value),
                () => Assert.Equal("1", m.Tags["a"]),
                () => Assert.Equal("2", m.Tags["b"]),
                () => Assert.Equal("3", m.Tags["c"]));
        }
    }

    public sealed class UpDownCounterUpTests
    {
        [Fact]
        public void Up_NoTags_RecordsOne()
        {
            using var meter = new Meter("test.updown.up.notags");
            var counter = meter.CreateUpDownCounter<int>("connections");
            using var collector = new MetricCollector(meter);

            counter.Up();

            var m = Assert.Single(collector.For("connections"));
            Assert.Equal(1, m.Value);
        }

        [Fact]
        public void Up_OneTag_RecordsOneWithTag()
        {
            using var meter = new Meter("test.updown.up.1tag");
            var counter = meter.CreateUpDownCounter<int>("connections");
            using var collector = new MetricCollector(meter);

            counter.Up(Tag("pool", "main"));

            var m = Assert.Single(collector.For("connections"));
            Assert.Multiple(
                () => Assert.Equal(1, m.Value),
                () => Assert.Equal("main", m.Tags["pool"]));
        }

        [Fact]
        public void Up_TwoTags_RecordsOneWithBothTags()
        {
            using var meter = new Meter("test.updown.up.2tags");
            var counter = meter.CreateUpDownCounter<int>("connections");
            using var collector = new MetricCollector(meter);

            counter.Up(Tag("pool", "main"), Tag("state", "active"));

            var m = Assert.Single(collector.For("connections"));
            Assert.Multiple(
                () => Assert.Equal(1, m.Value),
                () => Assert.Equal("main", m.Tags["pool"]),
                () => Assert.Equal("active", m.Tags["state"]));
        }

        [Fact]
        public void Up_SpanTags_RecordsOneWithAllTags()
        {
            using var meter = new Meter("test.updown.up.span");
            var counter = meter.CreateUpDownCounter<int>("connections");
            using var collector = new MetricCollector(meter);

            ReadOnlySpan<KeyValuePair<string, object?>> tags = [Tag("a", "1"), Tag("b", "2")];
            counter.Up(tags);

            var m = Assert.Single(collector.For("connections"));
            Assert.Multiple(
                () => Assert.Equal(1, m.Value),
                () => Assert.Equal("1", m.Tags["a"]),
                () => Assert.Equal("2", m.Tags["b"]));
        }
    }

    public sealed class UpDownCounterDownTests
    {
        [Fact]
        public void Down_NoTags_RecordsMinusOne()
        {
            using var meter = new Meter("test.updown.down.notags");
            var counter = meter.CreateUpDownCounter<int>("connections");
            using var collector = new MetricCollector(meter);

            counter.Down();

            var m = Assert.Single(collector.For("connections"));
            Assert.Equal(-1, m.Value);
        }

        [Fact]
        public void Down_OneTag_RecordsMinusOneWithTag()
        {
            using var meter = new Meter("test.updown.down.1tag");
            var counter = meter.CreateUpDownCounter<int>("connections");
            using var collector = new MetricCollector(meter);

            counter.Down(Tag("pool", "connections"));

            var m = Assert.Single(collector.For("connections"));
            Assert.Multiple(
                () => Assert.Equal(-1, m.Value),
                () => Assert.Equal("connections", m.Tags["pool"]));
        }

        [Fact]
        public void Down_TwoTags_RecordsMinusOneWithBothTags()
        {
            using var meter = new Meter("test.updown.down.2tags");
            var counter = meter.CreateUpDownCounter<int>("connections");
            using var collector = new MetricCollector(meter);

            counter.Down(Tag("pool", "main"), Tag("reason", "timeout"));

            var m = Assert.Single(collector.For("connections"));
            Assert.Multiple(
                () => Assert.Equal(-1, m.Value),
                () => Assert.Equal("main", m.Tags["pool"]),
                () => Assert.Equal("timeout", m.Tags["reason"]));
        }

        [Fact]
        public void Down_SpanTags_RecordsMinusOneWithAllTags()
        {
            using var meter = new Meter("test.updown.down.span");
            var counter = meter.CreateUpDownCounter<int>("connections");
            using var collector = new MetricCollector(meter);

            ReadOnlySpan<KeyValuePair<string, object?>> tags = [Tag("a", "1"), Tag("b", "2"), Tag("c", "3")];
            counter.Down(tags);

            var m = Assert.Single(collector.For("connections"));
            Assert.Multiple(
                () => Assert.Equal(-1, m.Value),
                () => Assert.Equal("1", m.Tags["a"]),
                () => Assert.Equal("2", m.Tags["b"]),
                () => Assert.Equal("3", m.Tags["c"]));
        }
    }

    public sealed class NullGuardTests
    {
        [Fact]
        public void CounterUp_NullCounter_ThrowsArgumentNullException()
        {
            Counter<long> counter = null!;

            var ex = Assert.Throws<ArgumentNullException>(() => counter.Up());

            Assert.Equal("counter", ex.ParamName);
        }

        [Fact]
        public void UpDownCounterUp_NullCounter_ThrowsArgumentNullException()
        {
            UpDownCounter<int> counter = null!;

            var ex = Assert.Throws<ArgumentNullException>(() => counter.Up());

            Assert.Equal("counter", ex.ParamName);
        }

        [Fact]
        public void UpDownCounterDown_NullCounter_ThrowsArgumentNullException()
        {
            UpDownCounter<int> counter = null!;

            var ex = Assert.Throws<ArgumentNullException>(() => counter.Down());

            Assert.Equal("counter", ex.ParamName);
        }
    }
}
