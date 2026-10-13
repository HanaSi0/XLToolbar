using System.Numerics;
using Dalamud.Configuration;

namespace XLToolbar;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public bool NativeInteractions { get; set; } = true;
    public bool LockWindows { get; set; }
    public bool TopIcons { get; set; } = true;
    public float TopIconSize { get; set; } = 20;
    public float TopGap { get; set; } = 8;
    public float TopRowPadding { get; set; } = 5;
    public float TopMaxLabelWidth { get; set; } = 180;
    public bool Rounded { get; set; } = true;
    public Vector3 Tint { get; set; } = new(.39f, .77f, .73f);
    public Vector3 ForegroundColor { get; set; } = new(.95f, .97f, 1);
    public Dictionary<string, bool> TopPinned { get; set; } = new()
    { ["currency"] = true, ["equipment"] = true, ["fps"] = true, ["sound"] = true, ["clock"] = true };
    public Dictionary<string, bool> TopSections { get; set; } = [];
    public static readonly Dictionary<string, string> TopLabels = new()
    {
        ["effects"] = "特效", ["tribes"] = "部族", ["online"] = "在线状态", ["equipment"] = "装备",
        ["jobs"] = "职业套装", ["timers"] = "任务情报", ["currency"] = "货币", ["teleport"] = "传送",
        ["map"] = "地图", ["plugins"] = "插件收纳", ["commands"] = "指令收纳", ["inventory"] = "背包",
        ["quests"] = "任务", ["zone"] = "区域/天气", ["fps"] = "FPS", ["dtr"] = "其他插件 DTR",
        ["sound"] = "音量", ["clock"] = "LT/ET", ["lock"] = "布局锁定", ["gamewindows"] = "游戏功能", ["settings"] = "设置",
    };

    public void Normalize()
    {
        TopPinned ??= [];
        TopSections ??= [];
        TopIconSize = Bound(TopIconSize, 14, 32, 20);
        TopGap = Bound(TopGap, 0, 24, 8);
        TopRowPadding = Bound(TopRowPadding, 2, 14, 5);
        TopMaxLabelWidth = Bound(TopMaxLabelWidth, 60, 300, 180);
        foreach (var key in TopLabels.Keys) TopSections.TryAdd(key, true);
    }

    private static float Bound(float value, float min, float max, float fallback) =>
        float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}
