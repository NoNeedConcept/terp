using Akka.Actor;

namespace Terp.Akka.Tests;

public sealed class BackoffExtensionsTests
{
    // Index of minBackoff/maxBackoff in the Props.Arguments Akka.Pattern.Backoff.OnFailure/OnStop builds.
    private const int MinBackoffArgIndex = 2;
    private const int MaxBackoffArgIndex = 3;

    public sealed class TestActor : ReceiveActor
    {
    }

    [Fact]
    public void BackoffOnFailure_ValidProps_WrapsInRestartSupervisor()
    {
        var props = Props.Create<TestActor>()
            .BackoffOnFailure("test-actor", minBackoff: TimeSpan.FromSeconds(1), maxBackoff: TimeSpan.FromSeconds(10));

        Assert.Multiple(
            () => Assert.Equal("BackoffOnRestartSupervisor", props.Type.Name),
            () => Assert.Equal(TimeSpan.FromSeconds(1), props.Arguments[MinBackoffArgIndex]),
            () => Assert.Equal(TimeSpan.FromSeconds(10), props.Arguments[MaxBackoffArgIndex]));
    }

    [Fact]
    public void BackoffOnStop_ValidProps_WrapsInStopSupervisor()
    {
        var props = Props.Create<TestActor>()
            .BackoffOnStop("test-actor", minBackoff: TimeSpan.FromSeconds(2), maxBackoff: TimeSpan.FromSeconds(20));

        Assert.Multiple(
            () => Assert.Equal("BackoffSupervisor", props.Type.Name),
            () => Assert.Equal(TimeSpan.FromSeconds(2), props.Arguments[MinBackoffArgIndex]),
            () => Assert.Equal(TimeSpan.FromSeconds(20), props.Arguments[MaxBackoffArgIndex]));
    }

    [Fact]
    public void BackoffOnFailure_ConfigureCallback_WrapsInRestartSupervisor()
    {
        var props = Props.Create<TestActor>()
            .BackoffOnFailure("test-actor", configure: o => o.WithMaxNrOfRetries(5));

        Assert.Equal("BackoffOnRestartSupervisor", props.Type.Name);
    }

    [Fact]
    public void BackoffOnStop_ConfigureCallback_WrapsInStopSupervisor()
    {
        var props = Props.Create<TestActor>()
            .BackoffOnStop("test-actor", configure: o => o.WithMaxNrOfRetries(5));

        Assert.Equal("BackoffSupervisor", props.Type.Name);
    }

    [Fact]
    public void Backoff_NullPropsOrChildName_ThrowsArgumentNullException()
    {
        var props = Props.Create<TestActor>();

        AssertParamName("childProps", () => ((Props)null!).BackoffOnFailure("test-actor"));
        AssertParamName("childName", () => props.BackoffOnFailure(null!));
        AssertParamName("childProps", () => ((Props)null!).BackoffOnStop("test-actor"));
        AssertParamName("childName", () => props.BackoffOnStop(null!));
    }

    private static void AssertParamName(string expected, Action act)
    {
        var ex = Assert.Throws<ArgumentNullException>(act);

        Assert.Equal(expected, ex.ParamName);
    }
}
