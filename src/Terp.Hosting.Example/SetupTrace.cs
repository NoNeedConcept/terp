namespace Terp.Hosting.Example;

/// <summary>Records which target was set up when; setups run before any container exists, so the trace is static.</summary>
internal static class SetupTrace
{
    private static readonly List<string> _entries = [];

    public static IReadOnlyList<string> Entries => _entries;

    public static void Add(string target)
    {
        _entries.Add(target);
    }
}
