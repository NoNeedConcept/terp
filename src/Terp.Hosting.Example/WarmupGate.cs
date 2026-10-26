namespace Terp.Hosting.Example;

/// <summary>Pretends a dependency needs a few attempts to become ready.</summary>
internal sealed class WarmupGate(int requiredAttempts)
{
    public int Attempts { get; private set; }

    public Task<bool> CheckAsync(CancellationToken cancellationToken)
    {
        Attempts++;
        return Task.FromResult(Attempts >= requiredAttempts);
    }
}
