using Terp.Options;

namespace Terp.Tests.Options;

public sealed class ValidationFailuresTests
{
    [Fact]
    public void Constructor_WithSection_IsEmptyAndKeepsSection()
    {
        var sut = new ValidationFailures("FunkArr:Feed");

        Assert.Multiple(
            () => Assert.Equal(0, sut.Count),
            () => Assert.Equal("FunkArr:Feed", sut.Section));
    }

    [Fact]
    public void Add_WithSection_PrefixesMessage()
    {
        var sut = new ValidationFailures("FunkArr:Feed");

        sut.Add("Interval must be positive");

        var result = sut.ToResult();
        var failure = Assert.Single(result.Failures!);
        Assert.Equal("FunkArr:Feed:Interval must be positive", failure);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Add_WithoutSection_KeepsMessage(string? section)
    {
        var sut = new ValidationFailures(section);

        sut.Add("Interval must be positive");

        var result = sut.ToResult();
        var failure = Assert.Single(result.Failures!);
        Assert.Equal("Interval must be positive", failure);
    }

    [Fact]
    public void Add_TwoMessages_CountsBoth()
    {
        var sut = new ValidationFailures();

        sut.Add("a");
        sut.Add("b");

        Assert.Equal(2, sut.Count);
    }

    [Fact]
    public void Add_NullMessage_Throws()
    {
        var sut = new ValidationFailures();

        var ex = Assert.Throws<ArgumentNullException>(() => sut.Add(null!));

        Assert.Equal("message", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Add_BlankMessage_Throws(string message)
    {
        var sut = new ValidationFailures();

        var ex = Assert.Throws<ArgumentException>(() => sut.Add(message));

        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public void Require_SatisfiedCondition_RecordsNothing()
    {
        var sut = new ValidationFailures();

        sut.Require(true, "x");

        Assert.Equal(0, sut.Count);
    }

    [Fact]
    public void Require_ViolatedCondition_RecordsMessage()
    {
        var sut = new ValidationFailures("S");

        sut.Require(false, "x");

        var result = sut.ToResult();
        var failure = Assert.Single(result.Failures!);
        Assert.Equal("S:x", failure);
    }

    [Fact]
    public void Require_SatisfiedConditionWithNullMessage_Throws()
    {
        var sut = new ValidationFailures();

        var ex = Assert.Throws<ArgumentNullException>(() => sut.Require(true, null!));

        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public void ToResult_NoFailures_ReturnsSuccess()
    {
        var sut = new ValidationFailures("S");

        var result = sut.ToResult();

        Assert.Multiple(
            () => Assert.True(result.Succeeded),
            () => Assert.Same(Microsoft.Extensions.Options.ValidateOptionsResult.Success, result));
    }

    [Fact]
    public void ToResult_Failures_ReturnsFailedResultInInsertionOrder()
    {
        var sut = new ValidationFailures("S");

        sut.Add("a");
        sut.Add("b");
        var result = sut.ToResult();

        Assert.Multiple(
            () => Assert.True(result.Failed),
            () => Assert.Equal(["S:a", "S:b"], result.Failures));
    }
}
