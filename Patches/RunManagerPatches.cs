using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace weightedPath.Patches;

/// <summary>
/// Patches RunManager to recalculate paths when player moves to a new location.
/// </summary>
[HarmonyPatch(typeof(RunManager))]
public static class RunManagerPatches
{
    /// <summary>
    /// Recalculate paths when entering a new map coordinate.
    /// </summary>
    [HarmonyPatch("EnterMapCoordInternal")]
    [HarmonyPostfix]
    public static void EnterMapCoordInternal_Postfix()
    {
        var state = RunManager.Instance.DebugOnlyGetState();
        if (state != null)
        {
            WeightedPaths.RecalculatePaths(state);
        }
    }
}
