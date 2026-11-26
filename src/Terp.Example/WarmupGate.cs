namespace Terp.Example;

internal sealed class WarmupGate(int requiredAttempts)
{
    public int Attempts { get; private set; }

    public Task<bool> CheckAsync(CancellationToken cancellationToken)
    {
        Attempts++;
        return Task.FromResult(Attempts >= requiredAttempts);
    }
}
