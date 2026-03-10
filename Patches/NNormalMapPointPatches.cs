using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace weightedPath.Patches;

/// <summary>
/// Patches NNormalMapPoint to render weight values on map nodes.
/// </summary>
[HarmonyPatch(typeof(NNormalMapPoint))]
public static class NNormalMapPointPatches
{
    private const string WeightBackgroundName = "WeightBackground";
    private const string WeightLabelName = "WeightInnerLabel";

    /// <summary>
    /// Add weight label after the map point is ready.
    /// </summary>
    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    public static void Ready_Postfix(NNormalMapPoint __instance)
    {
        AddOrUpdateWeightLabel(__instance);
    }

    /// <summary>
    /// Called externally to refresh weight labels after path calculation.
    /// </summary>
    public static void RefreshWeightLabel(NNormalMapPoint instance)
    {
        AddOrUpdateWeightLabel(instance);
    }

    private static void AddOrUpdateWeightLabel(NNormalMapPoint instance)
    {
        var point = instance.Point;
        if (point == null) return;

        var value = WeightedPaths.GetPointValue(point.coord);
        if (value == null)
        {
            RemoveWeightLabel(instance);
            return;
        }

        var existingBackground = instance.GetNodeOrNull<PanelContainer>(WeightBackgroundName);

        if (existingBackground != null)
        {
            // Update existing label directly (no recreate)
            var existingLabel = existingBackground.GetNodeOrNull<Label>(WeightLabelName);
            if (existingLabel != null)
            {
                existingLabel.Text = FormatValue(value.Value);
                existingLabel.Modulate = WeightedPaths.GetValueColor(value.Value);
            }
            else
            {
                // Label missing, create it
                var newLabel = CreateInnerLabel(value.Value);
                existingBackground.AddChild(newLabel);
            }

            // Update border color
            var style = existingBackground.GetThemeStylebox("panel") as StyleBoxFlat;
            if (style != null)
            {
                style.BorderColor = WeightedPaths.GetValueColor(value.Value);
            }
        }
        else
        {
            // Create new
            var background = CreateWeightBackground(value.Value);
            instance.AddChild(background);
        }
    }

    private static void RemoveWeightLabel(NNormalMapPoint instance)
    {
        instance.GetNodeOrNull<PanelContainer>(WeightBackgroundName)?.QueueFree();
    }

    private static Label CreateInnerLabel(double value)
    {
        var label = new Label
        {
            Name = WeightLabelName,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = FormatValue(value),
            Modulate = WeightedPaths.GetValueColor(value)
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0));
        label.AddThemeConstantOverride("outline_size", 1);
        return label;
    }

    private static PanelContainer CreateWeightBackground(double value)
    {
        var background = new PanelContainer { Name = WeightBackgroundName };

        // Style
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f),
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            BorderColor = WeightedPaths.GetValueColor(value),
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 2,
            ContentMarginBottom = 2
        };
        background.AddThemeStyleboxOverride("panel", style);

        var innerLabel = CreateInnerLabel(value);
        background.AddChild(innerLabel);

        // Position below the map point icon
        background.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        background.OffsetLeft = -20;
        background.OffsetRight = 20;
        background.OffsetTop = 4;
        background.OffsetBottom = 20;

        return background;
    }

    private static string FormatValue(double value)
    {
        return value.ToString("F1");
    }

    /// <summary>
    /// Refresh when state changes.
    /// </summary>
    [HarmonyPatch("RefreshState")]
    [HarmonyPostfix]
    public static void RefreshState_Postfix(NNormalMapPoint __instance)
    {
        AddOrUpdateWeightLabel(__instance);
    }
}