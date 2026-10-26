using Akka.Util;

namespace Terp.Akka.Tests;

public sealed class AkkaOptionsExtensionsTests
{
    [Fact]
    public void Match_OptionWithValue_InvokesOnlySomeBranch()
    {
        var option = Option<string>.Create("test");
        var invoked = "";

        option.Match(value => invoked = value, () => Assert.Fail("none branch must not run"));

        Assert.Equal("test", invoked);
        Assert.True(option.Match(_ => true, () => false));
    }

    [Fact]
    public void Match_OptionWithoutValue_InvokesOnlyNoneBranch()
    {
        var option = Option<string>.None;
        var noneInvoked = false;

        option.Match(_ => Assert.Fail("some branch must not run"), () => noneInvoked = true);

        Assert.True(noneInvoked);
        Assert.False(option.Match(_ => true, () => false));
    }

    [Fact]
    public void Match_NullBranches_ThrowsArgumentNullException()
    {
        var option = Option<string>.Create("test");

        AssertParamName("some", () => option.Match((Func<string, int>)null!, () => 0));
        AssertParamName("none", () => option.Match(_ => 0, null!));
        AssertParamName("some", () => option.Match((Action<string>)null!, () => { }));
        AssertParamName("none", () => option.Match(_ => { }, null!));
    }

    private static void AssertParamName(string expected, Action act)
    {
        var ex = Assert.Throws<ArgumentNullException>(act);

        Assert.Equal(expected, ex.ParamName);
    }
}
