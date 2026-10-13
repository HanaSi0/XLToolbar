namespace XLToolbar;

internal sealed record GameMenuEntry(uint Id, string Label, uint Icon);

internal static class GameMenuPolicy
{
    internal static GameMenuEntry[] Filter(IEnumerable<GameMenuEntry> entries, string search) =>
        entries.Where(e => !string.IsNullOrWhiteSpace(e.Label) &&
            e.Label.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();

    internal static GameMenuEntry[] Favorites(IEnumerable<GameMenuEntry> entries, IReadOnlySet<uint> favorites) =>
        entries.Where(e => favorites.Contains(e.Id)).ToArray();
}
