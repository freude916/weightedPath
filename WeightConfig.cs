using System;
using System.Collections.Generic;

namespace weightedPath;

/// <summary>
/// Stores configurable weights for each room type.
/// Higher weight = more desirable room type.
/// </summary>
public class WeightConfig
{
    public double Monster { get; set; } = 1.5;
    public double Elite { get; set; } = 3.0;
    public double Unknown { get; set; } = 1.5;
    public double RestSite { get; set; } = 3.0;
    public double Treasure { get; set; } = 0.0;
    public double Shop { get; set; } = 1.0; // Per 100 gold

    private static readonly Dictionary<string, Action<WeightConfig, double>> Setters = new()
    {
        ["Monster"] = (c, v) => c.Monster = v,
        ["Elite"] = (c, v) => c.Elite = v,
        ["Unknown"] = (c, v) => c.Unknown = v,
        ["RestSite"] = (c, v) => c.RestSite = v,
        ["Treasure"] = (c, v) => c.Treasure = v,
        ["Shop"] = (c, v) => c.Shop = v,
    };

    private static readonly Dictionary<string, Func<WeightConfig, double>> Getters = new()
    {
        ["Monster"] = c => c.Monster,
        ["Elite"] = c => c.Elite,
        ["Unknown"] = c => c.Unknown,
        ["RestSite"] = c => c.RestSite,
        ["Treasure"] = c => c.Treasure,
        ["Shop"] = c => c.Shop,
    };

    public double GetWeight(string roomType)
    {
        return Getters.TryGetValue(roomType, out var getter) ? getter(this) : 0.0;
    }

    public void SetWeight(string roomType, double value)
    {
        if (Setters.TryGetValue(roomType, out var setter))
        {
            setter(this, Math.Round(value, 1));
        }
    }

    public void AdjustWeight(string roomType, double delta)
    {
        if (Getters.TryGetValue(roomType, out var getter) && Setters.TryGetValue(roomType, out var setter))
        {
            setter(this, Math.Max(0, Math.Round(getter(this) + delta, 1)));
        }
    }

    public static IEnumerable<string> RoomTypes => Getters.Keys;
}
