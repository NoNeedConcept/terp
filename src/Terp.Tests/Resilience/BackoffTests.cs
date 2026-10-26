using Terp.Resilience;

namespace Terp.Tests.Resilience;

public sealed class BackoffTests
{
    #region Create

    [Fact]
    public void Create_ValidInitialDelay_ReturnsPolicy()
    {
        var policy = Backoff.Create(TimeSpan.FromMilliseconds(500));

        Assert.NotNull(policy);
    }

    [Fact]
    public void Create_MultiplierBelowOne_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Backoff.Create(TimeSpan.FromMilliseconds(100), multiplier: 0.5));

        Assert.Equal("multiplier", ex.ParamName);
    }

    #endregion

    #region Delay

    [Fact]
    public void Delay_AttemptZero_ReturnsInitialDelay()
    {
        var policy = Backoff.Create(TimeSpan.FromMilliseconds(500));

        Assert.Equal(TimeSpan.FromMilliseconds(500), policy.Delay(0));
    }

    [Theory]
    [InlineData(0, 500)]
    [InlineData(1, 1000)]
    [InlineData(2, 2000)]
    [InlineData(3, 4000)]
    [InlineData(4, 8000)]
    public void Delay_VariousAttempts_FollowsExponentialProgression(int attempt, double expectedMs)
    {
        var policy = Backoff.Create(TimeSpan.FromMilliseconds(500));

        Assert.Equal(expectedMs, policy.Delay(attempt).TotalMilliseconds);
    }

    [Fact]
    public void Delay_CustomMultiplier_AppliesMultiplier()
    {
        var policy = Backoff.Create(TimeSpan.FromMilliseconds(100), multiplier: 3.0);

        Assert.Equal(TimeSpan.FromMilliseconds(900), policy.Delay(2));
    }

    [Fact]
    public void Delay_ExceedsMaxDelay_ReturnsMaxDelay()
    {
        var maxDelay = TimeSpan.FromSeconds(5);
        var policy = Backoff.Create(TimeSpan.FromSeconds(1), maxDelay: maxDelay);

        Assert.Equal(maxDelay, policy.Delay(10));
    }

    [Fact]
    public void Delay_VeryHighAttempt_ReturnsMaxDelayWithoutOverflow()
    {
        var maxDelay = TimeSpan.FromMinutes(1);
        var policy = Backoff.Create(TimeSpan.FromSeconds(1), maxDelay: maxDelay);

        Assert.Equal(maxDelay, policy.Delay(100));
    }

    [Fact]
    public void Delay_NegativeAttempt_ThrowsArgumentOutOfRangeException()
    {
        var policy = Backoff.Create(TimeSpan.FromMilliseconds(100));

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => policy.Delay(-1));

        Assert.Equal("attempt", ex.ParamName);
    }

    #endregion

    #region DelayWithJitter

    [Fact]
    public void DelayWithJitter_DefaultRandom_StaysWithinExpectedRange()
    {
        var policy = Backoff.Create(TimeSpan.FromMilliseconds(1000));

        for (var i = 0; i < 100; i++)
        {
            var delay = policy.DelayWithJitter(0);
            Assert.InRange(delay.TotalMilliseconds, 750, 1250);
        }
    }

    [Fact]
    public void DelayWithJitter_ExceedsMaxDelay_IsCappedAtMaxDelay()
    {
        var maxDelay = TimeSpan.FromSeconds(5);
        var policy = Backoff.Create(TimeSpan.FromSeconds(1), maxDelay: maxDelay);

        for (var i = 0; i < 100; i++)
        {
            Assert.True(policy.DelayWithJitter(10) <= maxDelay);
        }
    }

    [Fact]
    public void DelayWithJitter_RepeatedCalls_ProducesVariation()
    {
        var policy = Backoff.Create(TimeSpan.FromMilliseconds(1000));
        var delays = new HashSet<double>();

        for (var i = 0; i < 20; i++)
        {
            delays.Add(Math.Round(policy.DelayWithJitter(0).TotalMilliseconds));
        }

        Assert.True(delays.Count > 1, "Jitter should produce varying delays");
    }

    #endregion

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble()
        {
            return value;
        }
    }

    [Theory]
    [InlineData(0.0, 750)]
    [InlineData(0.5, 1000)]
    [InlineData(0.9999, 1249.95)]
    public void DelayWithJitter_InjectedRandom_UsesRandomValue(double randomValue, double expectedMs)
    {
        var policy = Backoff.Create(TimeSpan.FromMilliseconds(1000), random: new FixedRandom(randomValue));

        Assert.Equal(expectedMs, policy.DelayWithJitter(0).TotalMilliseconds, 1);
    }

    [Fact]
    public void DelayWithJitter_NoMaxDelayAndHugeAttempt_ReturnsMaxValueWithoutOverflow()
    {
        var policy = Backoff.Create(TimeSpan.FromSeconds(1), random: new FixedRandom(0.9999));

        Assert.Equal(TimeSpan.MaxValue, policy.DelayWithJitter(2000));
    }
}
