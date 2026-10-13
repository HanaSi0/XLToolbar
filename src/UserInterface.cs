using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Events.EventDataTypes;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Interface.Utility;
using Lumina.Excel.Sheets;

namespace XLToolbar;

internal sealed partial class UserInterface(Plugin plugin)
{
    internal bool SettingsOpen;
    private Configuration C => plugin.Config;
    private bool commandsOpen, teleportOpen, gameWindowsOpen;
    private string commandSearch = "", commandArguments = "", teleportSearch = "", gameWindowSearch = "", notice = "";
    private List<TeleportDestination> teleportRows = [];
    private uint gil;
    private byte weather;
    private long serverTime;
    private DateTime lastTelemetry;
    private bool telemetryAvailable;
    private float minimumDurability, maximumSpiritbond;
    private ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse |
        (C.LockWindows ? ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize : ImGuiWindowFlags.None);

    internal void Invalidate() { cachedTopItems = null; cachedTopLayout = null; }
    internal void Reset()
    {
        Invalidate(); lastTelemetry = default; telemetryAvailable = false;
        effectsOpen = tribesOpen = onlineOpen = jobsOpen = equipmentOpen = currencyOpen = volumeOpen = false;
        commandsOpen = teleportOpen = gameWindowsOpen = false;
        gearsets.Clear(); currencies.Clear(); teleportRows.Clear(); currencyRead = default;
        notice = ""; topIconCache.Clear();
    }

    internal void Draw()
    {
        if (C.Enabled && Plugin.Client.IsLoggedIn && !Plugin.GameGui.GameUiHidden)
        {
            DrawTop();
            DrawUtilityPanels();
            if (commandsOpen) DrawCommands();
            if (teleportOpen) DrawTeleports();
            if (gameWindowsOpen) DrawGameWindows();
        }
        if (SettingsOpen) DrawSettings();
    }

    private void OpenGame(uint command) => Native(() =>
    {
        if (!GameBridge.MainCommandEnabled(command)) { notice = "当前游戏状态下该功能不可用。"; return; }
        GameBridge.Open(command);
    });

    private void Native(System.Action action)
    {
        if (!C.NativeInteractions) { SettingsOpen = true; return; }
        if (Plugin.Objects.LocalPlayer is null) return;
        try { action(); }
        catch (Exception ex)
        {
            C.NativeInteractions = false; plugin.Save(); SettingsOpen = true;
            notice = "游戏接口调用失败，已关闭游戏交互。请查看 /xllog。";
            Plugin.Log.Error(ex, "XLToolbar 游戏接口调用失败");
        }
    }

    private static void InvokeDtr(IReadOnlyDtrBarEntry entry, MouseClickType click)
    {
        try
        {
            var io = ImGui.GetIO();
            entry.OnClick?.Invoke(new DtrInteractionEvent
            {
                ClickType = click, Position = ImGui.GetMousePos(),
                ModifierKeys = (io.KeyCtrl ? ClickModifierKeys.Ctrl : 0) |
                    (io.KeyAlt ? ClickModifierKeys.Alt : 0) | (io.KeyShift ? ClickModifierKeys.Shift : 0),
            });
        }
        catch (Exception ex) { Plugin.Log.Error(ex, "DTR 插件入口调用失败：{Title}", entry.Title); }
    }

    private static void DrawOutlinedText(ImDrawListPtr draw, Vector2 pos, uint color, string text)
    {
        draw.AddText(pos + new Vector2(1, 1), 0xdf000000, text);
        draw.AddText(pos, color, text);
    }

    private void DrawSettings()
    {
        ImGui.SetNextWindowSize(new Vector2(560, 650) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);
        var visible = ImGui.Begin("XLToolbar 设置", ref SettingsOpen, ImGuiWindowFlags.NoCollapse);
        try
        {
            if (!visible) return;
            var changed = false;
            var enabled = C.Enabled;
            if (ImGui.Checkbox("显示顶部工具条", ref enabled)) { C.Enabled = enabled; changed = true; }
            var interactions = C.NativeInteractions;
            if (ImGui.Checkbox("启用游戏交互", ref interactions)) { C.NativeInteractions = interactions; changed = true; }
            var icons = C.TopIcons;
            if (ImGui.Checkbox("显示图标", ref icons)) { C.TopIcons = icons; changed = true; }
            var locked = C.LockWindows;
            if (ImGui.Checkbox("锁定功能面板位置与大小", ref locked)) { C.LockWindows = locked; changed = true; }
            var size = C.TopIconSize; var gap = C.TopGap; var padding = C.TopRowPadding; var width = C.TopMaxLabelWidth;
            if (ImGui.SliderFloat("图标大小", ref size, 14, 32)) { C.TopIconSize = size; changed = true; }
            if (ImGui.SliderFloat("间距", ref gap, 0, 24)) { C.TopGap = gap; changed = true; }
            if (ImGui.SliderFloat("上下留白", ref padding, 2, 14)) { C.TopRowPadding = padding; changed = true; }
            if (ImGui.SliderFloat("文字最大宽度", ref width, 60, 300)) { C.TopMaxLabelWidth = width; changed = true; }
            if (notice.Length > 0) ImGui.TextWrapped(notice);
            ImGui.TextWrapped("左侧快捷入口均匀排列；固定项靠右。宽度不足时自动换行。使用 /xltoolbar 可随时打开设置。");
            ImGui.Separator();
            if (ImGui.BeginTable("sections", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                try
                {
                    ImGui.TableSetupColumn("功能"); ImGui.TableSetupColumn("显示"); ImGui.TableSetupColumn("固定右侧");
                    ImGui.TableHeadersRow();
                    foreach (var (key, label) in Configuration.TopLabels)
                    {
                        ImGui.PushID(key);
                        try
                        {
                            ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted(label);
                            ImGui.TableSetColumnIndex(1); var show = C.TopSections.GetValueOrDefault(key, true);
                            if (ImGui.Checkbox("##show", ref show)) { C.TopSections[key] = show; changed = true; }
                            ImGui.TableSetColumnIndex(2); var pin = C.TopPinned.GetValueOrDefault(key);
                            if (ImGui.Checkbox("##pin", ref pin)) { C.TopPinned[key] = pin; changed = true; }
                        }
                        finally { ImGui.PopID(); }
                    }
                }
                finally { ImGui.EndTable(); }
            }
            if (changed) plugin.Save();
        }
        finally { ImGui.End(); }
    }

    private void DrawGameWindows()
    {
        var visible = BeginPanel("游戏功能", ref gameWindowsOpen, new Vector2(520, 620));
        try
        {
            if (!visible) return;
            ImGui.InputTextWithHint("##game-search", "搜索游戏功能", ref gameWindowSearch, 100);
            foreach (var entry in Plugin.Data.GetExcelSheet<MainCommand>().OrderBy(x => x.RowId))
            {
                var label = entry.Name.ToString();
                if (string.IsNullOrWhiteSpace(label) || !label.Contains(gameWindowSearch, StringComparison.OrdinalIgnoreCase)) continue;
                var enabled = GameBridge.MainCommandEnabled(entry.RowId);
                ImGui.BeginDisabled(!enabled);
                if (ImGui.Selectable(label + "###game" + entry.RowId)) OpenGame(entry.RowId);
                ImGui.EndDisabled();
            }
        }
        finally { EndPanel(); }
    }
}
