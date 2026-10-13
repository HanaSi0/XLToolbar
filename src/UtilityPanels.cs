using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Config;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace XLToolbar;

internal sealed partial class UserInterface
{
    private bool effectsOpen, tribesOpen, onlineOpen, jobsOpen, equipmentOpen, currencyOpen, volumeOpen;
    private List<GearsetSummary> gearsets = [];
    private uint? repairMaterialId;
    private string selectedCommand = "";
    private DateTime currencyRead;
    private List<CurrencySummary> currencies = [];
    private (int Acquired, int Limit) weeklyTomes;

    private void OpenAction(uint id)
    {
        Native(() => GameBridge.UseGeneralAction(id));
    }

    private void OpenPersonalInfo()
    {
        Native(() =>
        {
            if (!GameBridge.OpenSearchInfo()) notice = "个人信息尚未就绪：请先打开原版社交窗口，再重试。";
        });
    }

    private void PushPanelStyle()
    {
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 14) * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(10, 6) * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 9) * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6 * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4 * scale);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(.065f, .08f, .10f, .98f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(.94f, .96f, .98f, 1));
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(.63f, .70f, .77f, 1));
        ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(.10f, .13f, .16f, 1));
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(.13f, .18f, .22f, 1));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(.14f, .18f, .22f, 1));
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(.16f, .23f, .28f, 1));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(.23f, .36f, .42f, 1));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(.28f, .45f, .50f, 1));
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(.16f, .29f, .34f, 1));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(.22f, .36f, .42f, 1));
    }

    private void EndPanel()
    {
        ImGui.End();
        ImGui.PopStyleColor(11);
        ImGui.PopStyleVar(5);
    }

    private static void PanelSection(string label)
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(.67f, .85f, .88f, 1), label);
        ImGui.Separator();
    }

    private void DrawVolume()
    {
        var visible = BeginPanel("音量", ref volumeOpen, new Vector2(330, 160));
        try
        {
            if (!visible) return;
            if (!Plugin.GameConfig.System.TryGet("SoundMaster", out uint current)) return;
            var volume = (int)Math.Clamp(current, 0, 100);
            ImGui.BeginDisabled(!C.NativeInteractions);
            if (ImGui.SliderInt("主音量", ref volume, 0, 100, "%d%%"))
                Native(() => Plugin.GameConfig.System.Set("SoundMaster", (uint)volume));
            ImGui.EndDisabled();
            if (ImGui.Button("完整声音设置")) OpenGame(19);
        }
        finally { EndPanel(); }
    }

    private bool BeginPanel(string title, ref bool open, Vector2 size)
    {
        PushPanelStyle();
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.SetNextWindowSize(size * scale, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(300, 150) * scale, new Vector2(float.MaxValue));
        return ImGui.Begin(title + "###XLToolbarPanelV2" + title, ref open, WindowFlags) && open;
    }

    private void DrawUtilityPanels()
    {
        if (volumeOpen) DrawVolume();
        if (effectsOpen) DrawEffects();
        if (tribesOpen) DrawTribes();
        if (onlineOpen) DrawOnline();
        if (jobsOpen) DrawJobs();
        if (equipmentOpen) DrawEquipment();
        if (currencyOpen) DrawCurrency();
    }

    private void DrawEffects()
    {
        var visible = BeginPanel("特效与召唤兽", ref effectsOpen, new Vector2(700, 620));
        try
        {
            if (!visible) return;
            PanelSection("战斗特效");
            Effect("自身特效", UiConfigOption.BattleEffectSelf);
            Effect("队友特效", UiConfigOption.BattleEffectParty);
            Effect("其他玩家特效", UiConfigOption.BattleEffectOther);
            Effect("PvP 敌方特效", UiConfigOption.BattleEffectPvPEnemyPc);
            PanelSection("召唤兽大小");
            Pet("巴哈姆特", UiConfigOption.BahamutSize);
            Pet("凤凰", UiConfigOption.PhoenixSize);
            Pet("迦楼罗", UiConfigOption.GarudaSize);
            Pet("泰坦", UiConfigOption.TitanSize);
            Pet("伊弗利特", UiConfigOption.IfritSize);
            Pet("魔兽", UiConfigOption.SolBahamutSize);
        }
        finally { EndPanel(); }
    }

    private void Effect(string name, UiConfigOption option)
    {
        if (!Plugin.GameConfig.UiConfig.TryGet(option.ToString(), out uint value)) { ImGui.TextDisabled(name + "：当前客户端不支持"); return; }
        var index = 2 - (int)Math.Clamp(value, 0, 2);
        ChoiceRow(name, option, index, ["关闭", "简单", "完全"], selected =>
            Native(() => Plugin.GameConfig.UiConfig.Set(option.ToString(), (uint)(2 - selected))));
    }

    private void Pet(string name, UiConfigOption option)
    {
        if (!Plugin.GameConfig.UiConfig.TryGet(option.ToString(), out uint value)) { ImGui.TextDisabled(name + "：当前客户端不支持"); return; }
        ChoiceRow(name, option, (int)Math.Clamp(value, 0, 2), ["小", "中", "大"], selected =>
            Native(() => Plugin.GameConfig.UiConfig.Set(option.ToString(), (uint)selected)));
    }

    private void ChoiceRow(string name, UiConfigOption option, int selected, string[] choices, System.Action<int> action)
    {
        ImGui.PushID(option.ToString());
        try
        {
            ImGui.TextUnformatted(name);
            ImGui.BeginDisabled(!C.NativeInteractions);
            try
            {
                var width = Math.Max(1, (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2) / 3);
                for (var i = 0; i < choices.Length; i++)
                {
                    if (i > 0) ImGui.SameLine();
                    if (i == selected) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(.22f, .43f, .47f, 1));
                    var clicked = ImGui.Button(choices[i], new Vector2(width, 0));
                    if (i == selected) ImGui.PopStyleColor();
                    if (clicked) action(i);
                }
            }
            finally { ImGui.EndDisabled(); }
        }
        finally { ImGui.PopID(); }
    }

    private void DrawTribes()
    {
        var visible = BeginPanel("部族友好度", ref tribesOpen, new Vector2(380, 650));
        try
        {
            if (!visible) return;
            ImGui.TextUnformatted($"剩余部族任务次数：{GameBridge.TribeAllowance()}");
            uint? lastExpansion = null;
            foreach (var tribe in Plugin.Data.GetExcelSheet<BeastTribe>())
            {
                if (tribe.RowId == 0 || tribe.RowId > byte.MaxValue) continue;
                var rep = GameBridge.ReadReputation((byte)tribe.RowId);
                var rank = (byte)(rep.Rank & 0x7F);
                if (rank == 0) continue;
                if (lastExpansion != tribe.Expansion.RowId)
                {
                    lastExpansion = tribe.Expansion.RowId;
                    var expansion = Plugin.Data.GetExcelSheet<ExVersion>().GetRowOrDefault(tribe.Expansion.RowId);
                    ImGui.TextDisabled(expansion?.Name.ToString() ?? $"资料片 {tribe.Expansion.RowId}");
                }
                ImGui.PushID((int)tribe.RowId);
                GameIcon(tribe.IconReputation, 32); ImGui.SameLine();
                ImGui.BeginGroup();
                ImGui.TextUnformatted(tribe.Name.ToString());
                var rankRow = Plugin.Data.GetExcelSheet<BeastReputationRank>().GetRowOrDefault(rank);
                var rankName = rankRow?.Name.ToString() ?? rank.ToString();
                ImGui.TextDisabled($"{rankName} ({rank}/{tribe.MaxRank})");
                ImGui.EndGroup();
                ImGui.ProgressBar(rep.Required == 0 ? 1 : Math.Clamp((float)rep.Current / rep.Required, 0, 1), new Vector2(-1, 22 * ImGuiHelpers.GlobalScale), rep.Required == 0 ? "最高等级" : $"{rep.Current} / {rep.Required}");
                var currency = tribe.CurrencyItem.Value;
                if (currency.RowId != 0) ImGui.TextDisabled($"{currency.Name}：{GameBridge.ItemCount(currency.RowId)}");
                ImGui.Separator(); ImGui.PopID();
            }
        }
        finally { EndPanel(); }
    }

    private void DrawOnline()
    {
        var visible = BeginPanel("在线状态", ref onlineOpen, new Vector2(330, 490));
        try
        {
            if (!visible) return;
            // Only user-selectable states from the supplied reference; never system or GM states.
            foreach (var id in new uint[] { 47, 17, 12, 22, 21, 27, 28, 30, 29 })
            {
                var row = Plugin.Data.GetExcelSheet<OnlineStatus>().GetRowOrDefault(id);
                if (row is null) continue;
                GameIcon(row.Value.Icon, 28); ImGui.SameLine();
                if (ImGui.Selectable(row.Value.Name.ToString() + "###online" + id, Plugin.Objects.LocalPlayer?.OnlineStatus.RowId == id, ImGuiSelectableFlags.None, new Vector2(0, 32 * ImGuiHelpers.GlobalScale))) Native(() => GameBridge.SetOnlineStatus(id));
            }

            if (ImGui.Button("打开个人信息")) OpenPersonalInfo();
        }
        finally { EndPanel(); }
    }

    private void DrawJobs()
    {
        var visible = BeginPanel("职业与装备套装", ref jobsOpen, new Vector2(820, 670));
        try
        {
            if (!visible) return;
            if (ImGui.Button("刷新套装")) gearsets = GameBridge.ReadGearsets();

            var columnCount = Math.Clamp((int)(ImGui.GetContentRegionAvail().X / (260 * ImGuiHelpers.GlobalScale)), 1, 3);
            if (ImGui.BeginTable("job-columns", columnCount, ImGuiTableFlags.SizingStretchSame))
            {
                try
                {
                    foreach (var group in new[] { "防护职业", "治疗职业", "近战职业", "远程物理职业", "远程魔法职业", "大地使者", "能工巧匠", "其他职业" })
                    {
                        var matching = gearsets.Where(g => JobGroup(g.Job) == group).ToArray();
                        if (matching.Length == 0) continue;
                        ImGui.TableNextColumn();
                        PanelSection(group);
                            foreach (var gear in matching)
                            {
                                var job = Plugin.Data.GetExcelSheet<ClassJob>().GetRowOrDefault(gear.Job);
                                if (job is null) continue;
                                var level = Plugin.Player.GetClassJobLevel(job.Value);
                                ImGui.PushID(gear.Index);
                                GameIcon((uint)(62100 + gear.Job), 30); ImGui.SameLine();
                                var pos = ImGui.GetCursorScreenPos();
                                var width = ImGui.GetContentRegionAvail().X;
                                if (ImGui.Selectable("##gearset", false, ImGuiSelectableFlags.None, new Vector2(width, 62 * ImGuiHelpers.GlobalScale)))
                                    Native(() => GameBridge.EquipGearset(gear.Index));
                                var draw = ImGui.GetWindowDrawList();
                                draw.PushClipRect(pos, pos + new Vector2(width, 62 * ImGuiHelpers.GlobalScale), true);
                                draw.AddText(pos, 0xFFF4EBE5, FitTopLabel(gear.Name, width));
                                draw.AddText(pos + new Vector2(0, 20 * ImGuiHelpers.GlobalScale), 0xFFCEDFD9, FitTopLabel(job.Value.Name.ToString(), width));
                                draw.AddText(pos + new Vector2(0, 40 * ImGuiHelpers.GlobalScale), 0xFFB9C9D4, $"Lv.{level} · 装等 {gear.ItemLevel}");
                                draw.PopClipRect();
                                if (ImGui.IsItemHovered()) ImGui.SetTooltip(gear.Name);
                                ImGui.PopID();
                            }
                    }
                }
                finally { ImGui.EndTable(); }
            }
            if (gearsets.Count == 0) ImGui.TextDisabled("未读取到保存的装备套装；可在原版角色界面保存。");
            if (ImGui.Button("原版角色／套装界面")) OpenGame(2);
        }
        finally { EndPanel(); }
    }

    private string JobGroup(byte id)
    {
        var row = Plugin.Data.GetExcelSheet<ClassJob>().GetRowOrDefault(id);
        if (id is >= 8 and <= 15) return "能工巧匠";
        if (id is >= 16 and <= 18) return "大地使者";
        return row?.Role switch { 1 => "防护职业", 2 => "近战职业", 3 => row?.PrimaryStat == 4 ? "远程魔法职业" : "远程物理职业", 4 => "治疗职业", _ => "其他职业" };
    }

    private void DrawEquipment()
    {
        var visible = BeginPanel("装备耐久与精炼度", ref equipmentOpen, new Vector2(700, 620));
        try
        {
            if (!visible) return;
            if (ImGui.BeginTable("equipment", 3, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            {
                try
                {
                    ImGui.TableSetupColumn("装备", ImGuiTableColumnFlags.WidthStretch, 2);
                    ImGui.TableSetupColumn("耐久度", ImGuiTableColumnFlags.WidthStretch, 1);
                    ImGui.TableSetupColumn("精炼度", ImGuiTableColumnFlags.WidthStretch, 1);
                    ImGui.TableHeadersRow();
                    var index = 0;
                    foreach (var item in GameBridge.ReadInventory(InventoryType.EquippedItems))
                    {
                        var row = Plugin.Data.GetExcelSheet<Item>().GetRowOrDefault(item.Id);
                        if (row is null) continue;
                        ImGui.PushID(index++);
                        ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0);
                        GameIcon(row.Value.Icon, 28); ImGui.SameLine();
                        ImGui.TextWrapped(row.Value.Name.ToString());
                        ImGui.TableSetColumnIndex(1); PercentageBar(item.Condition, true);
                        ImGui.TableSetColumnIndex(2); PercentageBar(item.Spiritbond, false);
                        ImGui.PopID();
                    }
                }
                finally { ImGui.EndTable(); }
            }
            PanelSection("装备维护");
            if (repairMaterialId is null)
                repairMaterialId = Plugin.Data.GetExcelSheet<Item>().FirstOrDefault(i => i.Name.ToString() is "8级暗物质" or "Grade 8 Dark Matter").RowId;
            if (repairMaterialId.Value != 0) ImGui.TextDisabled($"8级暗物质 × {GameBridge.ItemCount(repairMaterialId.Value)}");
            if (ImGui.Button("修理")) OpenAction(6);
            ImGui.SameLine(); if (ImGui.Button("精制魔晶石")) OpenAction(14);
            ImGui.SameLine(); if (ImGui.Button("镶嵌魔晶石")) OpenAction(12);
            if (ImGui.Button("镶嵌魔晶石 II")) OpenAction(13);
        }
        finally { EndPanel(); }
    }

    private void DrawCurrency()
    {
        var visible = BeginPanel("货币", ref currencyOpen, new Vector2(680, 620));
        try
        {
            if (!visible) return;
            ImGui.TextUnformatted($"金币：{gil:N0}");
            if ((DateTime.UtcNow - currencyRead).TotalSeconds >= 1)
            {
                var nativeIds = GameBridge.ReadInventory(InventoryType.Currency).Select(i => i.Id).ToHashSet();
                var tomes = Plugin.Data.GetExcelSheet<TomestonesItem>().Where(t => t.Tomestones.RowId != 0).ToDictionary(t => t.Item.RowId, t => t.Tomestones.RowId);
                currencies.Clear();
                foreach (var item in Plugin.Data.GetExcelSheet<Item>().Where(i => i.ItemUICategory.RowId == 63 && i.RowId != 1))
                {
                    var count = GameBridge.ItemCount(item.RowId);
                    if (count == 0 && !nativeIds.Contains(item.RowId) && !tomes.ContainsKey(item.RowId)) continue;
                    var cap = item.RowId is >= 20 and <= 22 ? GameBridge.CompanySealCap((byte)(item.RowId - 19)) : item.StackSize;
                    currencies.Add(new CurrencySummary(item.RowId, count, cap, tomes.GetValueOrDefault(item.RowId) == 3));
                }
                weeklyTomes = GameBridge.WeeklyTomestones();
                currencyRead = DateTime.UtcNow;
            }
            if (ImGui.BeginTable("wallet", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                try
                {
                    ImGui.TableSetupColumn("货币", ImGuiTableColumnFlags.WidthStretch, 2);
                    ImGui.TableSetupColumn("持有 / 上限", ImGuiTableColumnFlags.WidthStretch, 1.5f);
                    ImGui.TableSetupColumn("本周", ImGuiTableColumnFlags.WidthStretch, 1);
                    ImGui.TableHeadersRow();
                    foreach (var currency in currencies)
                    {
                        var row = Plugin.Data.GetExcelSheet<Item>().GetRow(currency.Id);
                        ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0);
                        GameIcon(row.Icon, 26); ImGui.SameLine(); ImGui.TextWrapped(row.Name.ToString());
                        ImGui.TableSetColumnIndex(1);
                        var cap = currency.Cap == 0 ? "—" : currency.Cap.ToString("N0");
                        ImGui.TextUnformatted($"{currency.Quantity:N0} / {cap}");
                        ImGui.TableSetColumnIndex(2);
                        if (currency.Limited) ImGui.TextUnformatted($"{weeklyTomes.Acquired:N0} / {weeklyTomes.Limit:N0}");
                        else ImGui.TextDisabled("—");
                    }
                }
                finally { ImGui.EndTable(); }
            }
            ImGui.Spacing();
            if (ImGui.Button("打开完整货币窗口")) OpenGame(66);
        }
        finally { EndPanel(); }
    }

    private sealed record CurrencySummary(uint Id, int Quantity, uint Cap, bool Limited);

    private void DrawCommands()
    {
        var visible = BeginPanel("指令收纳", ref commandsOpen, new Vector2(640, 540));
        try
        {
            if (!visible) return;
            ImGui.InputTextWithHint("##command-filter", "搜索指令／帮助", ref commandSearch, 128);
            var groups = Plugin.Commands.Commands.ToArray().Where(p => p.Key.Contains(commandSearch, StringComparison.OrdinalIgnoreCase) || p.Value.HelpMessage.Contains(commandSearch, StringComparison.OrdinalIgnoreCase))
                .GroupBy(p => p.Value.Handler.Method.DeclaringType?.Assembly.GetName().Name ?? "未知来源").OrderBy(g => g.Key);
            var child = ImGui.BeginChild("command-list", new Vector2(0, -90 * ImGuiHelpers.GlobalScale), false);
            try
            {
                if (child)
                    foreach (var group in groups)
                        if (ImGui.TreeNode(group.Key))
                        {
                            foreach (var entry in group)
                            {
                                if (ImGui.Selectable(entry.Key, selectedCommand == entry.Key)) selectedCommand = entry.Key;
                                if (ImGui.IsItemHovered()) ImGui.SetTooltip(entry.Value.HelpMessage);
                            }
                            ImGui.TreePop();
                        }
            }
            finally { ImGui.EndChild(); }
            ImGui.TextUnformatted("已选：" + selectedCommand);
            ImGui.InputText("参数", ref commandArguments, 256);
            if (ImGui.Button("执行所选指令") && Plugin.Commands.Commands.ContainsKey(selectedCommand))
            {
                try { Plugin.Commands.ProcessCommand(selectedCommand + (commandArguments.Length == 0 ? "" : " " + commandArguments)); }
                catch (Exception ex) { Plugin.Log.Error(ex, "插件指令执行失败"); }
            }
        }
        finally { EndPanel(); }
    }

    private void RefreshTeleports()
    {
        try { teleportRows = GameBridge.ReadTeleports(); }
        catch (Exception ex) { Plugin.Log.Error(ex, "传送列表读取失败"); teleportRows = []; }
    }

    private void DrawTeleports()
    {
        var visible = BeginPanel("传送面板", ref teleportOpen, new Vector2(520, 550));
        try
        {
            if (!visible) return;
            ImGui.InputTextWithHint("##teleport-filter", "搜索水晶／地区", ref teleportSearch, 128);
            ImGui.SameLine(); if (ImGui.Button("刷新")) RefreshTeleports();
            foreach (var item in teleportRows)
            {
                var row = Plugin.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(item.Id);
                var name = row?.PlaceName.Value.Name.ToString() ?? $"水晶 {item.Id}";
                if (!name.Contains(teleportSearch, StringComparison.OrdinalIgnoreCase)) continue;
                if (ImGui.Selectable($"{name}    {item.Cost:N0} 金币###tp{item.Id}:{item.SubIndex}", false, ImGuiSelectableFlags.None, new Vector2(0, 36 * ImGuiHelpers.GlobalScale)))
                    Native(() => GameBridge.Teleport(item.Id, item.SubIndex));
            }

        }
        finally { EndPanel(); }
    }

    private static void PercentageBar(float value, bool durability)
    {
        var color = durability && value < 25 ? new Vector4(.90f, .36f, .32f, 1)
            : durability && value < 50 ? new Vector4(.90f, .70f, .30f, 1)
            : new Vector4(.28f, .65f, .68f, 1);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, color);
        try { ImGui.ProgressBar(Math.Clamp(value / 100, 0, 1), new Vector2(-1, 24 * ImGuiHelpers.GlobalScale), $"{value:F0}%"); }
        finally { ImGui.PopStyleColor(); }
    }

    private void GameIcon(uint id, float size)
    {
        var texture = Plugin.Textures.GetFromGameIcon(new GameIconLookup(id)).GetWrapOrDefault();
        if (texture is null) ImGui.Dummy(new Vector2(size) * ImGuiHelpers.GlobalScale);
        else ImGui.Image(texture.Handle, new Vector2(size) * ImGuiHelpers.GlobalScale);
    }
}
