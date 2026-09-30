using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace PortraitAtelier;

/// <summary>
/// Narrow, user-invoked bridge to the portrait editor. This never reads or writes game state
/// unless the user presses Capture or Apply while the portrait editor is open.
/// </summary>
internal sealed unsafe class PortraitEditorBridge
{
    private readonly ICondition condition;

    public PortraitEditorBridge(ICondition condition) => this.condition = condition;

    public bool TryCapture(out PortraitPreset? preset, out string error)
    {
        preset = null;
        error = "";
        if (!condition[ConditionFlag.EditingPortrait])
        {
            error = "Open the in-game Portrait Editor before capturing a portrait.";
            return false;
        }

        var editor = AgentBannerEditor.Instance();
        if (editor == null || editor->EditorState == null || editor->EditorState->CharaView == null)
        {
            error = "The portrait editor is not ready yet. Wait for the character preview to load and try again.";
            return false;
        }

        var state = editor->EditorState;
        var data = stackalloc ExportedPortraitData[1];
        state->CharaView->ExportPortraitData(data);
        preset = new PortraitPreset
        {
            CameraPosition = new Half4(data->CameraPosition.X, data->CameraPosition.Y, data->CameraPosition.Z, data->CameraPosition.W),
            CameraTarget = new Half4(data->CameraTarget.X, data->CameraTarget.Y, data->CameraTarget.Z, data->CameraTarget.W),
            ImageRotation = data->ImageRotation,
            CameraZoom = data->CameraZoom,
            BannerTimeline = data->BannerTimeline,
            AnimationProgress = data->AnimationProgress,
            Expression = data->Expression,
            HeadDirection = new Half2(data->HeadDirection.X, data->HeadDirection.Y),
            EyeDirection = new Half2(data->EyeDirection.X, data->EyeDirection.Y),
            DirectionalRed = data->DirectionalLightingColorRed,
            DirectionalGreen = data->DirectionalLightingColorGreen,
            DirectionalBlue = data->DirectionalLightingColorBlue,
            DirectionalBrightness = data->DirectionalLightingBrightness,
            DirectionalVerticalAngle = data->DirectionalLightingVerticalAngle,
            DirectionalHorizontalAngle = data->DirectionalLightingHorizontalAngle,
            AmbientRed = data->AmbientLightingColorRed,
            AmbientGreen = data->AmbientLightingColorGreen,
            AmbientBlue = data->AmbientLightingColorBlue,
            AmbientBrightness = data->AmbientLightingBrightness,
            BackgroundId = data->BannerBg,
            FrameId = state->BannerEntry.BannerFrame,
            DecorationId = state->BannerEntry.BannerDecoration,
        };

        return true;
    }

    public bool TryApply(PortraitPreset preset, out string error)
    {
        error = "";
        if (!condition[ConditionFlag.EditingPortrait])
        {
            error = "Open the in-game Portrait Editor before applying a look.";
            return false;
        }

        var editor = AgentBannerEditor.Instance();
        if (editor == null || editor->EditorState == null || editor->EditorState->CharaView == null)
        {
            error = "The portrait editor is not ready yet. Wait for the character preview to load and try again.";
            return false;
        }

        var state = editor->EditorState;
        var previousFrame = state->BannerEntry.BannerFrame;
        var previousDecoration = state->BannerEntry.BannerDecoration;
        var frameApplied = preset.FrameId == previousFrame || state->SetFrame(preset.FrameId);
        var decorationApplied = frameApplied
            && (preset.DecorationId == previousDecoration || state->SetAccent(preset.DecorationId));
        if (!frameApplied || !decorationApplied)
        {
            if (preset.FrameId != previousFrame)
                state->SetFrame(previousFrame);
            if (preset.DecorationId != previousDecoration)
                state->SetAccent(previousDecoration);
            error = "The preset's frame or decoration was unavailable; the editor's current frame and decoration were kept.";
        }

        state->BannerEntry.BannerBg = preset.BackgroundId;
        var data = stackalloc ExportedPortraitData[1];
        state->CharaView->ExportPortraitData(data);

        Set(ref data->CameraPosition, preset.CameraPosition);
        Set(ref data->CameraTarget, preset.CameraTarget);
        data->ImageRotation = preset.ImageRotation;
        data->CameraZoom = preset.CameraZoom;
        data->BannerTimeline = preset.BannerTimeline;
        data->AnimationProgress = preset.AnimationProgress;
        data->Expression = preset.Expression;
        Set(ref data->HeadDirection, preset.HeadDirection);
        Set(ref data->EyeDirection, preset.EyeDirection);
        data->DirectionalLightingColorRed = preset.DirectionalRed;
        data->DirectionalLightingColorGreen = preset.DirectionalGreen;
        data->DirectionalLightingColorBlue = preset.DirectionalBlue;
        data->DirectionalLightingBrightness = preset.DirectionalBrightness;
        data->DirectionalLightingVerticalAngle = preset.DirectionalVerticalAngle;
        data->DirectionalLightingHorizontalAngle = preset.DirectionalHorizontalAngle;
        data->AmbientLightingColorRed = preset.AmbientRed;
        data->AmbientLightingColorGreen = preset.AmbientGreen;
        data->AmbientLightingColorBlue = preset.AmbientBlue;
        data->AmbientLightingBrightness = preset.AmbientBrightness;
        data->BannerBg = preset.BackgroundId;

        // The selected frame and decoration belong to the editor's BannerEntry, separate from
        // ExportedPortraitData. The style generator preserves both from the captured base.
        state->CharaView->ImportPortraitData(data);
        state->CharaView->ApplyCameraPositions();
        state->SetHasChanged(true);
        return true;
    }

    private static void Set(ref FFXIVClientStructs.FFXIV.Common.Math.HalfVector4 target, Half4 value)
    {
        target.X = value.X;
        target.Y = value.Y;
        target.Z = value.Z;
        target.W = value.W;
    }

    private static void Set(ref FFXIVClientStructs.FFXIV.Common.Math.HalfVector2 target, Half2 value)
    {
        target.X = value.X;
        target.Y = value.Y;
    }
}
