using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace weightedPath.Patches;

/// <summary>
/// Settings panel for adjusting path weights.
/// </summary>
public partial class WeightSettingsPanel : PanelContainer
{
    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(240, 340);

        // Panel style
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.14f, 0.97f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = new Color(0.5f, 0.5f, 0.6f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
        AddThemeStyleboxOverride("panel", style);

        var marginContainer = new MarginContainer();
        marginContainer.AddThemeConstantOverride("margin_left", 12);
        marginContainer.AddThemeConstantOverride("margin_right", 12);
        marginContainer.AddThemeConstantOverride("margin_top", 10);
        marginContainer.AddThemeConstantOverride("margin_bottom", 10);

        var mainVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        mainVBox.AddThemeConstantOverride("separation", 4);

        // Title
        var title = new Label
        {
            Text = "Path Weights",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", Colors.White);
        mainVBox.AddChild(title);

        mainVBox.AddChild(CreateSeparator());

        // Weight rows
        foreach (var roomType in WeightConfig.RoomTypes)
        {
            mainVBox.AddChild(CreateWeightRow(roomType));
        }

        mainVBox.AddChild(CreateSeparator());

        // Toggle
        var toggleRow = CreateToggleRow("Colored", WeightedPaths.ColoredWeights);
        mainVBox.AddChild(toggleRow);

        // Buttons
        var buttonRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttonRow.AddThemeConstantOverride("separation", 12);

        var resetBtn = CreateButton("Reset", new Color(0.9f, 0.35f, 0.35f));
        resetBtn.Pressed += ResetToDefaults;
        buttonRow.AddChild(resetBtn);

        var closeBtn = CreateButton("Close", new Color(0.6f, 0.6f, 0.65f));
        closeBtn.Pressed += () => Visible = false;
        buttonRow.AddChild(closeBtn);

        mainVBox.AddChild(buttonRow);

        marginContainer.AddChild(mainVBox);
        AddChild(marginContainer);
    }

    private HSeparator CreateSeparator()
    {
        var sep = new HSeparator();
        sep.AddThemeColorOverride("separator_color", new Color(0.35f, 0.35f, 0.4f));
        return sep;
    }

    private Button CreateButton(string text, Color accent)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(90, 32)
        };
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.AddThemeColorOverride("font_color", Colors.White);
        btn.AddThemeColorOverride("font_hover_color", accent);

        var normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.18f, 0.22f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        var hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.28f, 0.28f, 0.32f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            BorderColor = accent,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1
        };

        btn.AddThemeStyleboxOverride("normal", normalStyle);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", hoverStyle);

        return btn;
    }

    private HBoxContainer CreateWeightRow(string roomType)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 6);

        // Label (left side, expands to fill space)
        var label = new Label
        {
            Text = roomType,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", GetRoomTypeColor(roomType));

        // Right side: [-] value [+]
        var rightContainer = new HBoxContainer();
        rightContainer.AddThemeConstantOverride("separation", 4);

        var decBtn = new Button { Text = "-" };
        StyleSmallButton(decBtn);
        decBtn.Pressed += () => AdjustWeight(roomType, -0.5);

        var valueLabel = new Label
        {
            Name = "ValueLabel",
            Text = WeightedPaths.Config.GetWeight(roomType).ToString("F1"),
            CustomMinimumSize = new Vector2(40, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        valueLabel.AddThemeFontSizeOverride("font_size", 14);
        valueLabel.AddThemeColorOverride("font_color", Colors.White);

        var incBtn = new Button { Text = "+" };
        StyleSmallButton(incBtn);
        incBtn.Pressed += () => AdjustWeight(roomType, 0.5);

        rightContainer.AddChild(decBtn);
        rightContainer.AddChild(valueLabel);
        rightContainer.AddChild(incBtn);

        row.AddChild(label);
        row.AddChild(rightContainer);

        return row;
    }

    private void StyleSmallButton(Button btn)
    {
        btn.CustomMinimumSize = new Vector2(24, 22);
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.AddThemeColorOverride("font_color", Colors.White);
        btn.AddThemeColorOverride("font_hover_color", Colors.Yellow);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        btn.AddThemeStyleboxOverride("normal", style);

        var hover = new StyleBoxFlat
        {
            BgColor = new Color(0.25f, 0.25f, 0.3f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        btn.AddThemeStyleboxOverride("hover", hover);
    }

    private HBoxContainer CreateToggleRow(string text, bool initial)
    {
        var row = new HBoxContainer();

        var toggle = new CheckBox { ButtonPressed = initial };
        toggle.Toggled += pressed =>
        {
            WeightedPaths.ColoredWeights = pressed;
            RefreshAllMapLabels();
        };

        var label = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", Colors.LightGray);

        row.AddChild(toggle);
        row.AddChild(label);

        return row;
    }

    private void AdjustWeight(string roomType, double delta)
    {
        WeightedPaths.Config.AdjustWeight(roomType, delta);

        // Update value label
        var margin = GetChild<MarginContainer>(0);
        var vbox = margin.GetChild<VBoxContainer>(0);
        foreach (var child in vbox.GetChildren())
        {
            if (child is HBoxContainer row && row.GetChildCount() >= 2)
            {
                var label = row.GetChild<Label>(0);
                if (label?.Text == roomType)
                {
                    var rightContainer = row.GetChild<HBoxContainer>(1);
                    var value = rightContainer.GetChild<Label>(1);
                    value.Text = WeightedPaths.Config.GetWeight(roomType).ToString("F1");
                    break;
                }
            }
        }

        // Recalculate and refresh
        var state = RunManager.Instance.DebugOnlyGetState();
        if (state != null)
        {
            WeightedPaths.RecalculatePaths(state);
        }
        RefreshAllMapLabels();
    }

    private void RefreshAllMapLabels()
    {
        var mapScreen = NMapScreen.Instance;
        if (mapScreen == null) return;

        var dict = Traverse.Create(mapScreen).Field<Dictionary<MapCoord, NMapPoint>>("_mapPointDictionary").Value;
        if (dict == null) return;

        foreach (var kvp in dict)
        {
            if (kvp.Value is NNormalMapPoint np)
            {
                NNormalMapPointPatches.RefreshWeightLabel(np);
            }
        }
    }

    private void ResetToDefaults()
    {
        WeightedPaths.Config.Monster = 1.5;
        WeightedPaths.Config.Elite = 3.0;
        WeightedPaths.Config.Unknown = 1.5;
        WeightedPaths.Config.RestSite = 3.0;
        WeightedPaths.Config.Treasure = 0.0;
        WeightedPaths.Config.Shop = 1.0;

        var state = RunManager.Instance.DebugOnlyGetState();
        if (state != null)
        {
            WeightedPaths.RecalculatePaths(state);
        }
        RefreshAllMapLabels();

        // Rebuild panel
        var mapScreen = NMapScreen.Instance;
        if (mapScreen == null) return;

        QueueFree();
        var newPanel = new WeightSettingsPanel();
        newPanel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        newPanel.OffsetLeft = -250;
        newPanel.OffsetTop = -400;
        newPanel.OffsetRight = -10;
        newPanel.OffsetBottom = -60;
        mapScreen.AddChild(newPanel);
        NMapScreenPatches.SetPanel(newPanel);
    }

    private static Color GetRoomTypeColor(string roomType) => roomType switch
    {
        "Monster" => new Color(1.0f, 0.65f, 0.1f),
        "Elite" => new Color(0.2f, 0.9f, 0.9f),
        "Unknown" => new Color(0.65f, 0.65f, 0.7f),
        "RestSite" => new Color(0.95f, 0.35f, 0.35f),
        "Treasure" => new Color(1.0f, 0.85f, 0.2f),
        "Shop" => new Color(1.0f, 0.8f, 0.2f),
        _ => Colors.White
    };
}
