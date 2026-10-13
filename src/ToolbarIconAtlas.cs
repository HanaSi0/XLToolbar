using System.Numerics;

namespace XLToolbar;

internal static class ToolbarIconAtlas
{
    internal const string ResourceName = "XLToolbar.ToolbarIcons.png";
    private static readonly string[] Keys = ["effects", "equipment", "jobs", "map", "inventory", "quests", "currency"];

    internal static bool TryGetUv(string key, out Vector2 minimum, out Vector2 maximum)
    {
        var index = Array.IndexOf(Keys, key);
        if (index < 0) { minimum = default; maximum = default; return false; }
        minimum = new Vector2(index % 4 / 4f, index / 4 / 2f);
        maximum = minimum + new Vector2(.25f, .5f);
        return true;
    }
}
