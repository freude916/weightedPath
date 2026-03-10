using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace weightedPath;

/// <summary>
/// Represents a path through the map from a starting point to the boss.
/// Calculates weighted value based on room types and configurable weights.
/// </summary>
public class MapPath : List<MapPoint>
{
    public double Value { get; private set; }
    public double EstimatedGold { get; private set; }

    /// <summary>
    /// Generates all possible paths from the current position to the boss.
    /// </summary>
    public static List<MapPath> GenerateAll(IRunState runState)
    {
        var paths = new List<MapPath>();
        var map = runState.Map;

        if (map == null) return paths;

        MapPoint? start = runState.CurrentMapPoint;
        if (start == null)
        {
            // Use starting points if not yet on the map
            foreach (var startPoint in map.startMapPoints)
            {
                GeneratePathsFrom(startPoint, map.BossMapPoint, new List<MapPoint>(), paths);
            }
        }
        else
        {
            GeneratePathsFrom(start, map.BossMapPoint, new List<MapPoint>(), paths);
        }

        return paths;
    }

    private static void GeneratePathsFrom(
        MapPoint current,
        MapPoint target,
        List<MapPoint> currentPath,
        List<MapPath> results)
    {
        if (current == null) return;

        currentPath.Add(current);

        // Check if we've reached the boss or end of map
        if (current.PointType == MapPointType.Boss || current.Children.Count == 0)
        {
            results.Add(new MapPath(currentPath));
        }
        else
        {
            foreach (var child in current.Children)
            {
                if (!currentPath.Contains(child))
                {
                    GeneratePathsFrom(child, target, new List<MapPoint>(currentPath), results);
                }
            }
        }
    }

    private MapPath(List<MapPoint> points)
    {
        AddRange(points);
    }

    public MapPath() { }

    /// <summary>
    /// Calculates the value of this path based on room weights.
    /// </summary>
    public void Valuate(WeightConfig config, int currentGold)
    {
        Value = 0.0;
        EstimatedGold = currentGold;

        foreach (var point in this)
        {
            var roomValue = CalculateRoomValue(point.PointType, config);
            Value += roomValue;
        }
    }

    private double CalculateRoomValue(MapPointType pointType, WeightConfig config)
    {
        return pointType switch
        {
            MapPointType.Monster => config.Monster + AddGold(15),
            MapPointType.Elite => config.Elite + AddGold(30),
            MapPointType.Unknown => config.Unknown,
            MapPointType.RestSite => config.RestSite,
            MapPointType.Treasure => config.Treasure + AddGold(18),
            MapPointType.Shop => CalculateShopValue(config),
            MapPointType.Boss => 0,
            MapPointType.Ancient => 0,
            _ => 0
        };
    }

    private double AddGold(double amount)
    {
        EstimatedGold += amount;
        return 0;
    }

    private double CalculateShopValue(WeightConfig config)
    {
        if (config.Shop <= 0 || EstimatedGold <= 0) return 0;
        return (EstimatedGold / 100.0) * config.Shop;
    }

    /// <summary>
    /// Gets the best value for a specific map point across all paths containing it.
    /// This represents the remaining value from that point to the end of the path.
    /// </summary>
    public static double GetBestValueForPoint(MapPoint point, List<MapPath> paths, WeightConfig config, int currentGold)
    {
        var bestValue = 0.0;

        foreach (var path in paths)
        {
            var index = path.IndexOf(point);
            if (index < 0) continue;

            // Calculate remaining value from this point onward
            var remainingValue = 0.0;
            var estimatedGold = (double)currentGold;

            // First, calculate gold up to this point
            for (int i = 0; i < index; i++)
            {
                AddGoldForRoom(path[i].PointType, ref estimatedGold);
            }

            // Then calculate value from this point to end
            for (int i = index; i < path.Count; i++)
            {
                remainingValue += CalculateRoomValueStatic(path[i].PointType, config, ref estimatedGold);
            }

            if (remainingValue > bestValue)
            {
                bestValue = remainingValue;
            }
        }

        return bestValue;
    }

    private static void AddGoldForRoom(MapPointType pointType, ref double estimatedGold)
    {
        switch (pointType)
        {
            case MapPointType.Monster:
                estimatedGold += 15;
                break;
            case MapPointType.Elite:
                estimatedGold += 30;
                break;
            case MapPointType.Treasure:
                estimatedGold += 18;
                break;
        }
    }

    private static double CalculateRoomValueStatic(MapPointType pointType, WeightConfig config, ref double estimatedGold)
    {
        return pointType switch
        {
            MapPointType.Monster => config.Monster + AddGoldStatic(ref estimatedGold, 15),
            MapPointType.Elite => config.Elite + AddGoldStatic(ref estimatedGold, 30),
            MapPointType.Unknown => config.Unknown,
            MapPointType.RestSite => config.RestSite,
            MapPointType.Treasure => config.Treasure + AddGoldStatic(ref estimatedGold, 18),
            MapPointType.Shop => CalculateShopValueStatic(config, ref estimatedGold),
            MapPointType.Boss => 0,
            MapPointType.Ancient => 0,
            _ => 0
        };
    }

    private static double AddGoldStatic(ref double estimatedGold, double amount)
    {
        estimatedGold += amount;
        return 0;
    }

    private static double CalculateShopValueStatic(WeightConfig config, ref double estimatedGold)
    {
        if (config.Shop <= 0 || estimatedGold <= 0) return 0;
        return (estimatedGold / 100.0) * config.Shop;
    }
}
