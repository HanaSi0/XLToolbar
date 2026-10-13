using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Lumina.Excel.Sheets;

namespace XLToolbar;

internal sealed partial class UserInterface
{
    private GameMenuEntry[]? gameMenuEntries;

    private void DrawGameWindows()
    {
        var visible = BeginPanel("游戏菜单", ref gameWindowsOpen, new Vector2(900, 650));
        try
        {
            if (!visible) return;
            // Resolve the complete native main menu from this client's sheets, including
            // character menus. This preserves client-specific names, icons and command IDs.
            gameMenuEntries ??= Plugin.Data.GetExcelSheet<MainCommand>()
                .OrderBy(e => e.RowId)
                .Where(e => !string.IsNullOrWhiteSpace(e.Name.ToString()))
                .Select(e => new GameMenuEntry(e.RowId, e.Name.ToString(), (uint)Math.Max(0, e.Icon)))
                .ToArray();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##game-search", "搜索游戏菜单…", ref gameWindowSearch, 128);
            ImGui.TextDisabled("点击图标或名称打开；点击 ☆ 收藏，★ 取消。常用项会保存并显示在顶部。");
            var entries = GameMenuPolicy.Filter(gameMenuEntries, gameWindowSearch);
            var favorites = GameMenuPolicy.Favorites(entries, C.FavoriteGameCommands);
            var child = ImGui.BeginChild("game-menu-scroll", Vector2.Zero, false);
            try
            {
                if (!child) return;
                ImGui.TextUnformatted("常用项");
                if (favorites.Length == 0)
                    ImGui.TextDisabled(gameWindowSearch.Length == 0 ? "还没有常用项，点击下方选项旁的 ☆ 添加。" : "没有匹配的常用项。");
                else DrawGameMenuGrid("favorite", favorites);
                ImGui.Separator();
                ImGui.TextUnformatted("所有选项");
                if (entries.Length == 0) ImGui.TextDisabled("没有匹配的游戏菜单。");
                else DrawGameMenuGrid("all", entries);
            }
            finally { ImGui.EndChild(); }
        }
        finally { EndPanel(); }
    }

    private void DrawGameMenuGrid(string id, IReadOnlyList<GameMenuEntry> entries)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var columns = Math.Clamp((int)(ImGui.GetContentRegionAvail().X / (250 * scale)), 1, 3);
        if (!ImGui.BeginTable("menu-grid-" + id, columns, ImGuiTableFlags.SizingStretchSame)) return;
        try
        {
            foreach (var entry in entries)
            {
                ImGui.TableNextColumn();
                ImGui.PushID((int)entry.Id);
                try
                {
                    var favorite = C.FavoriteGameCommands.Contains(entry.Id);
                    var starWidth = Math.Max(ImGui.CalcTextSize("★").X + ImGui.GetStyle().FramePadding.X * 2, 26 * scale);
                    var height = Math.Max(40 * scale, ImGui.GetFrameHeight());
                    var width = Math.Max(1, ImGui.GetContentRegionAvail().X - starWidth - ImGui.GetStyle().ItemSpacing.X);
                    var pos = ImGui.GetCursorScreenPos();
                    var enabled = C.NativeInteractions && GameBridge.MainCommandEnabled(entry.Id);
                    ImGui.BeginDisabled(!enabled);
                    var clicked = ImGui.InvisibleButton("##open", new Vector2(width, height));
                    var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
                    var draw = ImGui.GetWindowDrawList();
                    if (hovered && enabled)
                        draw.AddRectFilled(pos, pos + new Vector2(width, height), ImGui.ColorConvertFloat4ToU32(new Vector4(C.Tint, .2f)), 4 * scale);
                    draw.PushClipRect(pos, pos + new Vector2(width, height), true);
                    var iconSize = 36 * scale;
                    var texture = entry.Icon == 0 ? null : Plugin.Textures.GetFromGameIcon(new GameIconLookup(entry.Icon)).GetWrapOrDefault();
                    if (texture is not null)
                        draw.AddImage(texture.Handle, pos + new Vector2(0, (height - iconSize) / 2), pos + new Vector2(iconSize, (height + iconSize) / 2));
                    var text = FitTopLabel(entry.Label, Math.Max(0, width - iconSize - 8 * scale));
                    DrawOutlinedText(draw, pos + new Vector2(iconSize + 8 * scale, (height - ImGui.GetFontSize()) / 2),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(C.ForegroundColor, enabled ? 1 : .45f)), text);
                    draw.PopClipRect();
                    ImGui.EndDisabled();
                    if (hovered) ImGui.SetTooltip(entry.Label + (enabled ? "" : C.NativeInteractions ? "\n当前游戏状态下不可用" : "\n游戏交互已关闭，可在设置中启用"));
                    if (clicked) OpenGame(entry.Id);
                    ImGui.SameLine();
                    if (ImGui.Button(favorite ? "★##favorite" : "☆##favorite", new Vector2(starWidth, height)))
                    {
                        if (favorite) C.FavoriteGameCommands.Remove(entry.Id);
                        else C.FavoriteGameCommands.Add(entry.Id);
                        plugin.Save();
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(favorite ? "取消常用项" : "添加到常用项");
                }
                finally { ImGui.PopID(); }
            }
        }
        finally { ImGui.EndTable(); }
    }
}
