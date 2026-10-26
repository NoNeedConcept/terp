namespace Terp.Hosting.Gates;

internal sealed class DelegateStartupGate(string name, Func<CancellationToken, Task<bool>> check) : IStartupGate
{
    public string Name { get; } = name;

    public Task<bool> CheckAsync(CancellationToken cancellationToken)
    {
        return check(cancellationToken);
    }
}
