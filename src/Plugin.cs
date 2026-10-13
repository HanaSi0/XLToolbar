using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace XLToolbar;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface Interface { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IClientState Client { get; private set; } = null!;
    [PluginService] internal static IPlayerState Player { get; private set; } = null!;
    [PluginService] internal static IDataManager Data { get; private set; } = null!;
    [PluginService] internal static IDtrBar Dtr { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] internal static IObjectTable Objects { get; private set; } = null!;
    [PluginService] internal static ITextureProvider Textures { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    internal Configuration Config { get; }
    private readonly UserInterface ui;

    public Plugin()
    {
        Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Normalize();
        ui = new UserInterface(this);
        Commands.AddHandler("/xltoolbar", new CommandInfo(OnCommand)
        { HelpMessage = "打开顶部工具条设置；/xltoolbar toggle 切换显示。" });
        Interface.UiBuilder.Draw += Draw;
        Interface.UiBuilder.OpenConfigUi += OpenSettings;
        Interface.UiBuilder.OpenMainUi += OpenSettings;
        Client.Logout += OnLogout;
    }

    internal void Save() { Config.Normalize(); Interface.SavePluginConfig(Config); ui.Invalidate(); }
    private void Draw() => ui.Draw();
    private void OpenSettings() => ui.SettingsOpen = true;
    private void OnLogout(int type, int code) => ui.Reset();
    private void OnCommand(string command, string arguments)
    {
        if (arguments.Trim().Equals("toggle", StringComparison.OrdinalIgnoreCase))
        { Config.Enabled = !Config.Enabled; Save(); }
        else OpenSettings();
    }

    public void Dispose()
    {
        Client.Logout -= OnLogout;
        Interface.UiBuilder.Draw -= Draw;
        Interface.UiBuilder.OpenConfigUi -= OpenSettings;
        Interface.UiBuilder.OpenMainUi -= OpenSettings;
        Commands.RemoveHandler("/xltoolbar");
    }
}
