using System.Text.RegularExpressions;

namespace XLToolbar;

internal static partial class ToolbarEntryPolicy
{
    // DTR remains available for other plugins; only their duplicate FPS indicators are omitted.
    internal static bool IsFps(string title, string label) =>
        FpsMarker().IsMatch(title) || FpsMarker().IsMatch(label);

    [GeneratedRegex(@"\bFPS\b|\bframe\s*rate\b|帧率|幀率|[帧幀]\s*/\s*秒", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FpsMarker();
}
