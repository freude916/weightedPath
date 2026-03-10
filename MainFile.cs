using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

namespace weightedPath;

[ModInitializer(nameof(Initialize))]
public partial class WeightedPaths : Node
{
    internal const string ModId = "weightedPath";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    /// <summary>
    /// Configurable weights for each room type.
    /// </summary>
    public static WeightConfig Config { get; } = new();

    /// <summary>
    /// Calculated value for each map point (best path value from that point).
    /// </summary>
    public static Dictionary<MapCoord, double> PointValues { get; } = new();

    /// <summary>
    /// Maximum value for each row (for row-based color normalization).
    /// Key = row number, Value = max value in that row.
    /// </summary>
    public static Dictionary<int, double> RowMaxValues { get; } = new();

    /// <summary>
    /// All possible paths from current position.
    /// </summary>
    public static List<MapPath> AllPaths { get; private set; } = new();

    /// <summary>
    /// Minimum and maximum values for global color normalization.
    /// </summary>
    public static double MinValue { get; private set; }
    public static double MaxValue { get; private set; }

    /// <summary>
    /// Whether weight labels should be shown with colored backgrounds.
    /// </summary>
    public static bool ColoredWeights { get; set; } = true;

    private static Harmony? _harmony;

    public static void Initialize()
    {
        _harmony = new Harmony(ModId);
        _harmony.PatchAll();

        Logger.Info("WeightedPaths initialized");
    }

    /// <summary>
    /// Recalculates all path values. Should be called when:
    /// - Map is generated/loaded
    /// - Player moves to a new room
    /// - Weights are changed
    /// - Gold changes significantly
    /// </summary>
    public static void RecalculatePaths(IRunState runState)
    {
        PointValues.Clear();

        if (runState?.Map == null)
        {
            AllPaths = new List<MapPath>();
            return;
        }

        try
        {
            // Generate all possible paths
            AllPaths = MapPath.GenerateAll(runState);

            if (AllPaths.Count == 0)
            {
                return;
            }

            // Get current gold for shop valuation
            var currentGold = GetCurrentGold(runState);

            // Valuate each path
            foreach (var path in AllPaths)
            {
                path.Valuate(Config, currentGold);
            }

            // Calculate best value for each point
            foreach (var point in runState.Map.GetAllMapPoints())
            {
                var value = MapPath.GetBestValueForPoint(point, AllPaths, Config, currentGold);
                if (value > 0)
                {
                    PointValues[point.coord] = value;
                }
            }

            // Calculate min/max for color normalization and row max values
            RowMaxValues.Clear();
            if (PointValues.Count > 0)
            {
                MinValue = double.MaxValue;
                MaxValue = double.MinValue;

                foreach (var kvp in PointValues)
                {
                    var value = kvp.Value;
                    var row = kvp.Key.row;

                    if (value < MinValue) MinValue = value;
                    if (value > MaxValue) MaxValue = value;

                    // Track max value per row
                    if (!RowMaxValues.TryGetValue(row, out var currentMax) || value > currentMax)
                    {
                        RowMaxValues[row] = value;
                    }
                }

                // Ensure we have a range
                if (MaxValue - MinValue < 0.1)
                {
                    MaxValue = MinValue + 1;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Error recalculating paths: {ex.Message}");
        }
    }

    private static int GetCurrentGold(IRunState runState)
    {
        // Get gold from the first player
        foreach (var player in runState.Players)
        {
            return player.Gold;
        }
        return 0;
    }

    /// <summary>
    /// Gets the value for a specific map point.
    /// Returns null if the point has no calculated value.
    /// </summary>
    public static double? GetPointValue(MapCoord coord)
    {
        return PointValues.TryGetValue(coord, out var value) ? value : null;
    }

    /// <summary>
    /// Gets a color for displaying the value (red to green gradient).
    /// Uses global min/max normalization.
    /// </summary>
    public static Color GetValueColor(double value)
    {
        if (MaxValue <= MinValue) return Colors.White;

        var normalized = (value - MinValue) / (MaxValue - MinValue);

        // Red (low) to Yellow (mid) to Green (high)
        if (normalized < 0.5)
        {
            // Red to Yellow
            return new Color(1f, (float)(normalized * 2), 0f);
        }
        else
        {
            // Yellow to Green
            return new Color((float)(2 - normalized * 2), 1f, 0f);
        }
    }

    /// <summary>
    /// Gets a color based on the value's percentage relative to the max value in the same row.
    /// Red (0%) -> Yellow (50%) -> Green (100%)
    /// </summary>
    public static Color GetValueColorByRow(MapCoord coord, double value)
    {
        if (!RowMaxValues.TryGetValue(coord.row, out var rowMax) || rowMax <= 0)
        {
            return Colors.White;
        }

        var percentage = value / rowMax;  // 0.0 to 1.0

        // Red (0%) -> Yellow (50%) -> Green (100%)
        if (percentage < 0.5)
        {
            // Red to Yellow: increase green
            return new Color(1f, (float)(percentage * 2), 0f);
        }
        else
        {
            // Yellow to Green: decrease red
            return new Color((float)(2 - percentage * 2), 1f, 0f);
        }
    }
}
