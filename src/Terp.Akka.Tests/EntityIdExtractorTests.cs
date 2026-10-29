using Terp.Akka.Entities;

namespace Terp.Akka.Tests;

public sealed class EntityIdExtractorTests
{
    private sealed record TestCommand(string EntityId) : IWithEntityId;
    private sealed record PlainMessage(string Text);

    [Fact]
    public void EntityId_WithEntityIdMessage_ReturnsId()
    {
        var extractor = EntityIdExtractor.Instance;

        var result = extractor.EntityId(new TestCommand("order-42"));

        Assert.Equal("order-42", result);
    }

    [Fact]
    public void EntityId_NonEntityIdMessage_ReturnsNull()
    {
        var extractor = EntityIdExtractor.Instance;

        var result = extractor.EntityId(new PlainMessage("hello"));

        Assert.Null(result);
    }

    [Fact]
    public void EntityMessage_ReturnsMessageUnchanged()
    {
        var extractor = EntityIdExtractor.Instance;
        var msg = new TestCommand("abc");

        var result = extractor.EntityMessage(msg);

        Assert.Same(msg, result);
    }

    [Fact]
    public void EntityId_NullMessage_ThrowsArgumentNullException()
    {
        var extractor = EntityIdExtractor.Instance;

        var ex = Assert.Throws<ArgumentNullException>(() => extractor.EntityId(null!));
        Assert.Equal("message", ex.ParamName);
    }
}
