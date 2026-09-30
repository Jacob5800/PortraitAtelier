using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace PortraitAtelier;

public sealed class Plugin : IDalamudPlugin
{
    private const string Command = "/portraitatelier";
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly WindowSystem windowSystem = new("PortraitAtelier");
    private readonly PluginConfig config;
    private readonly MainWindow mainWindow;

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, ICondition condition)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        config = pluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
        mainWindow = new MainWindow(config, SaveConfig, new PortraitEditorBridge(condition));
        windowSystem.AddWindow(mainWindow);

        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += ToggleWindow;
        commandManager.AddHandler(Command, new CommandInfo((_, _) => ToggleWindow())
        {
            HelpMessage = "Open the portrait style gallery."
        });
    }

    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi -= ToggleWindow;
        commandManager.RemoveHandler(Command);
        windowSystem.RemoveAllWindows();
    }

    private void ToggleWindow() => mainWindow.Toggle();

    private void SaveConfig() => pluginInterface.SavePluginConfig(config);
}

public sealed class PluginConfig
{
    public List<string> FavoriteStyleIds { get; set; } = [];
}
