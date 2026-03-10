using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace weightedPath.Patches;

/// <summary>
/// Patches NMapScreen to recalculate paths and add settings button.
/// </summary>
[HarmonyPatch(typeof(NMapScreen))]
public static class NMapScreenPatches
{
    private static Button? _settingsButton;
    private static WeightSettingsPanel? _settingsPanel;
    private static bool _buttonCreated;

    /// <summary>
    /// Recalculate paths when the map is loaded/initialized.
    /// Also create settings button here since _runState is set in Initialize.
    /// </summary>
    [HarmonyPatch(nameof(NMapScreen.Initialize))]
    [HarmonyPostfix]
    public static void Initialize_Postfix(NMapScreen __instance, RunState runState)
    {
        WeightedPaths.Logger.Info("Map screen initialized, recalculating paths");
        WeightedPaths.RecalculatePaths(runState);

        // Create settings button after initialization
        if (!_buttonCreated)
        {
            CreateSettingsButton(__instance);
            _buttonCreated = true;
        }
    }

    /// <summary>
    /// Recalculate paths when a new map is set, and refresh all weight labels.
    /// </summary>
    [HarmonyPatch(nameof(NMapScreen.SetMap))]
    [HarmonyPostfix]
    public static void SetMap_Postfix(NMapScreen __instance, ActMap map)
    {
        var runState = Traverse.Create(__instance).Field<RunState>("_runState").Value;
        if (runState != null)
        {
            WeightedPaths.Logger.Info("Map set, recalculating paths");
            WeightedPaths.RecalculatePaths(runState);
            RefreshAllWeightLabels(__instance);
        }
    }

    private static void CreateSettingsButton(NMapScreen instance)
    {
        if (_settingsButton != null) return;

        _settingsButton = new Button
        {
            Name = "WeightSettingsButton",
            Text = "⚙",
            CustomMinimumSize = new Vector2(44, 44)
        };
        _settingsButton.FocusMode = Control.FocusModeEnum.Click;

        // Position in bottom-right corner
        // For BottomRight preset, offsets are relative to bottom-right, so use negative values
        _settingsButton.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        _settingsButton.OffsetLeft = -59;  // -44 (width) - 15 (margin)
        _settingsButton.OffsetTop = -59;   // -44 (height) - 15 (margin)
        _settingsButton.OffsetRight = -15;
        _settingsButton.OffsetBottom = -15;

        // Styling
        _settingsButton.AddThemeColorOverride("font_color", Colors.White);
        _settingsButton.AddThemeColorOverride("font_hover_color", new Color(1f, 0.85f, 0.3f));
        _settingsButton.AddThemeFontSizeOverride("font_size", 20);

        var normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f, 0.92f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = new Color(0.5f, 0.5f, 0.55f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
        var hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.25f, 0.25f, 0.35f, 0.95f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = new Color(1f, 0.85f, 0.3f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };

        _settingsButton.AddThemeStyleboxOverride("normal", normalStyle);
        _settingsButton.AddThemeStyleboxOverride("hover", hoverStyle);
        _settingsButton.AddThemeStyleboxOverride("pressed", hoverStyle);

        _settingsButton.Pressed += ToggleSettingsPanel;

        instance.AddChild(_settingsButton);
        WeightedPaths.Logger.Info("Settings button created successfully");
    }

    private static void ToggleSettingsPanel()
    {
        var mapScreen = NMapScreen.Instance;
        if (mapScreen == null) return;

        if (_settingsPanel != null)
        {
            _settingsPanel.Visible = !_settingsPanel.Visible;
        }
        else
        {
            _settingsPanel = new WeightSettingsPanel();
            _settingsPanel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
            // Panel is 240x340
            _settingsPanel.OffsetLeft = -250;
            _settingsPanel.OffsetTop = -400;
            _settingsPanel.OffsetRight = -10;
            _settingsPanel.OffsetBottom = -60;
            mapScreen.AddChild(_settingsPanel);
        }
    }

    public static void SetPanel(WeightSettingsPanel panel)
    {
        _settingsPanel = panel;
    }

    private static void RefreshAllWeightLabels(NMapScreen instance)
    {
        var dict = Traverse.Create(instance).Field<Dictionary<MapCoord, NMapPoint>>("_mapPointDictionary").Value;
        if (dict == null)
        {
            WeightedPaths.Logger.Warn("Could not get map point dictionary");
            return;
        }

        int refreshed = 0;
        foreach (var kvp in dict)
        {
            if (kvp.Value is NNormalMapPoint normalPoint)
            {
                NNormalMapPointPatches.RefreshWeightLabel(normalPoint);
                refreshed++;
            }
        }
        WeightedPaths.Logger.Info($"Refreshed {refreshed} weight labels");
    }

    /// <summary>
    /// Clean up when map screen is closed.
    /// </summary>
    [HarmonyPatch("CleanUp")]
    [HarmonyPrefix]
    public static void CleanUp_Prefix()
    {
        _settingsPanel?.QueueFree();
        _settingsPanel = null;
        _settingsButton?.QueueFree();
        _settingsButton = null;
        _buttonCreated = false;
    }
}
