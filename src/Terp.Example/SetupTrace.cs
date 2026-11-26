namespace Terp.Example;

internal static class SetupTrace
{
    private static readonly List<string> _entries = [];

    public static IReadOnlyList<string> Entries => _entries;

    public static void Add(string target)
    {
        _entries.Add(target);
    }
}
