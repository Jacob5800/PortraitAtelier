using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PortraitAtelier;

internal sealed class MainWindow : Window
{
    private readonly PluginConfig config;
    private readonly Action saveConfig;
    private IReadOnlyList<PortraitStyle> styles = PortraitStyle.Featured;
    private Task<StyleCatalogResult>? catalogRefreshTask;
    private string search = "";
    private string shareCodeInput = "";
    private string generatedCode = "";
    private string status = "Open the game's Portrait Editor and capture its current settings.";
    private PortraitPreset? generatedPreset;
    private readonly PortraitEditorBridge editorBridge;
    private PortraitStyle selectedStyle;
    private PortraitPreset? basePreset;
    private bool basePresetFromCode;
    private float strength = 0.85f;
    private int variation;
    private bool favoritesOnly;

    public MainWindow(PluginConfig config, Action saveConfig, PortraitEditorBridge editorBridge) : base("Portrait Atelier")
    {
        this.config = config;
        this.saveConfig = saveConfig;
        this.editorBridge = editorBridge;
        selectedStyle = PortraitStyle.Featured[0];
        Size = new Vector2(820, 710);
        SizeCondition = ImGuiCond.FirstUseEver;
        Flags = ImGuiWindowFlags.NoCollapse;
    }

    public override void Draw()
    {
        CompleteCatalogRefresh();
        ImGui.TextWrapped("Generate a new portrait look from your current in-game portrait. The generator preserves your pose, expression, animation, frame and decoration while changing lighting and camera distance.");
        ImGui.TextDisabled("Applying changes only the open editor. It never presses the game's save button.");
        ImGui.Separator();

        DrawBasePreset();
        ImGui.Separator();
        DrawStylePicker();
        ImGui.Separator();
        DrawGenerator();
        ImGui.Separator();
        DrawOutput();
    }

    private void DrawBasePreset()
    {
        ImGui.Text("1. Capture your current portrait");
        ImGui.TextDisabled("Open the in-game Portrait Editor, then capture its settings. Capture only reads the editor when you click.");
        if (ImGui.Button("Capture current portrait"))
        {
            if (editorBridge.TryCapture(out var captured, out var error))
            {
                basePreset = captured;
                basePresetFromCode = false;
                generatedCode = "";
                generatedPreset = null;
                variation = 0;
                status = "Base captured. Choose a look below.";
            }
            else
            {
                basePreset = null;
                status = error;
            }
        }

        ImGui.SameLine();
        ImGui.TextDisabled(basePreset is null ? "No portrait captured" : $"Base ready · pose {basePreset.BannerTimeline} · expression {basePreset.Expression}");

        ImGui.SetNextItemWidth(560f);
        ImGui.InputTextWithHint("##baseShareCode", "Or paste a Portrait Helper v1 code", ref shareCodeInput, 512);
        ImGui.SameLine();
        if (ImGui.Button("Use code as base"))
        {
            if (PortraitPreset.TryParse(shareCodeInput, out var imported, out var error))
            {
                basePreset = imported;
                basePresetFromCode = true;
                generatedPreset = null;
                generatedCode = "";
                variation = 0;
                status = "Preset code loaded as the base. Choose a look and generate a variation.";
            }
            else
            {
                status = error;
            }
        }

        if (basePresetFromCode && basePreset is not null && ImGui.Button("Apply imported code as-is"))
        {
            if (editorBridge.TryApply(basePreset, out var error))
                status = AppendApplyWarning("Applied the imported preset to the open editor. Review it there and use the game's save button if you want to keep it.", error);
            else
                status = error;
        }
    }

    private void DrawStylePicker()
    {
        config.FavoriteStyleIds ??= [];
        ImGui.Text("2. Choose a featured look");
        ImGui.SetNextItemWidth(280f);
        ImGui.InputTextWithHint("##styleSearch", "Search looks...", ref search, 128);
        ImGui.SameLine();
        if (ImGui.Button(favoritesOnly ? "★ Favorites" : "☆ Favorites"))
            favoritesOnly = !favoritesOnly;
        ImGui.SameLine();
        if (ImGui.Button(catalogRefreshTask is null ? "Refresh styles" : "Refreshing styles...") && catalogRefreshTask is null)
        {
            catalogRefreshTask = StyleCatalogClient.DownloadAsync();
            status = "Checking the public style catalog...";
        }

        var availableWidth = Math.Max(ImGui.GetContentRegionAvail().X, 200f);
        var leftWidth = Math.Clamp(availableWidth * 0.44f, 250f, 340f);
        if (ImGui.BeginChild("##stylePicker", new Vector2(leftWidth, 195f), true))
        {
            foreach (var style in GetVisibleStyles())
            {
                ImGui.PushID(style.Id);
                var favorite = config.FavoriteStyleIds.Contains(style.Id);
                if (ImGui.Selectable($"{(favorite ? "★ " : "☆ ")}{style.Name}##style", style.Id == selectedStyle.Id))
                    selectedStyle = style;
                ImGui.SameLine();
                ImGui.TextDisabled(style.Category);
                ImGui.PopID();
            }
        }
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##styleInfo", new Vector2(0, 195f), true))
        {
            ImGui.Text(selectedStyle.Name);
            ImGui.TextDisabled(selectedStyle.Category);
            ImGui.TextWrapped(selectedStyle.Description);
            ImGui.Spacing();
            DrawSwatch("Key light", selectedStyle.DirectionalColor);
            ImGui.SameLine();
            DrawSwatch("Fill light", selectedStyle.AmbientColor);
            ImGui.Spacing();

            var favorite = config.FavoriteStyleIds.Contains(selectedStyle.Id);
            if (ImGui.Button(favorite ? "Remove favorite" : "Add favorite"))
            {
                if (favorite)
                    config.FavoriteStyleIds.Remove(selectedStyle.Id);
                else
                    config.FavoriteStyleIds.Add(selectedStyle.Id);
                saveConfig();
            }
        }
        ImGui.EndChild();
    }

    private void DrawGenerator()
    {
        ImGui.Text("3. Generate a variation");
        ImGui.SetNextItemWidth(330f);
        ImGui.SliderFloat("Style strength", ref strength, 0f, 1f, "%.0f%%", ImGuiSliderFlags.None);
        strength = Math.Clamp(strength, 0f, 1f);
        ImGui.TextDisabled("Higher strength moves the lighting and crop closer to the selected look.");

        if (ImGui.Button("Generate"))
            GenerateNext();
        ImGui.SameLine();
        if (ImGui.Button("Generate another take"))
            GenerateNext();
        ImGui.SameLine();
        ImGui.TextDisabled(variation == 0 ? "Take 1" : $"Take {variation + 1}");
    }

    private void DrawOutput()
    {
        ImGui.Text("4. Apply or share the look");
        if (!string.IsNullOrWhiteSpace(status))
            ImGui.TextWrapped(status);

        if (string.IsNullOrWhiteSpace(generatedCode))
        {
            ImGui.TextDisabled("Generate a look to see its import code.");
            return;
        }

        if (ImGui.Button("Apply to open Portrait Editor"))
        {
            if (generatedPreset is null)
            {
                status = "Generate a look before applying it.";
            }
            else if (editorBridge.TryApply(generatedPreset, out var error))
            {
                status = AppendApplyWarning("Applied to the open editor. Review it there and use the game's own save button if you want to keep it.", error);
            }
            else
            {
                status = error;
            }
        }

        ImGui.InputTextMultiline("##generatedCode", ref generatedCode, 2048, new Vector2(-1f, 72f), ImGuiInputTextFlags.ReadOnly);
        if (ImGui.Button("Copy code"))
        {
            ImGui.SetClipboardText(generatedCode);
            status = "Copied a compatible Portrait Helper code. You can share it or import it with HaselTweaks.";
        }
    }

    private void GenerateNext()
    {
        if (basePreset is null)
        {
            status = "Capture a portrait from the open editor before generating a look.";
            return;
        }

        var styled = basePreset.WithStyle(selectedStyle, strength, variation);
        generatedPreset = styled;
        generatedCode = styled.ToShareCode();
        variation++;
        status = $"Generated {selectedStyle.Name}. Your original pose, expression, banner elements and animation are retained.";
    }

    private IReadOnlyList<PortraitStyle> GetVisibleStyles()
    {
        var query = search.Trim();
        return styles
            .Where(style => !favoritesOnly || config.FavoriteStyleIds.Contains(style.Id))
            .Where(style => string.IsNullOrWhiteSpace(query)
                || style.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || style.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                || style.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private void CompleteCatalogRefresh()
    {
        if (catalogRefreshTask is not { IsCompleted: true } task)
            return;

        catalogRefreshTask = null;
        var result = task.GetAwaiter().GetResult();
        if (!result.Success)
        {
            status = result.Message;
            return;
        }

        var byId = PortraitStyle.Featured.ToDictionary(style => style.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var style in result.Styles)
            byId[style.Id] = style;
        styles = byId.Values.OrderBy(style => style.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        status = $"Style catalog refreshed: {styles.Count} looks available.";
    }

    private static void DrawSwatch(string label, Rgb color)
    {
        ImGui.ColorButton($"##{label}", color.ToColor(), ImGuiColorEditFlags.NoTooltip, new Vector2(22f, 18f));
        ImGui.SameLine();
        ImGui.TextDisabled($"{label}: {color}");
    }

    private static string AppendApplyWarning(string message, string warning) =>
        string.IsNullOrWhiteSpace(warning) ? message : $"{message} {warning}";
}
