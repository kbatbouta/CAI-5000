using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
    public static class RegionGrid_Patch
    {
        [HarmonyPatch(typeof(RegionGrid), nameof(RegionGrid.SetRegionAt))]
        private static class RegionGrid_SetRegionAt_Patch
        {
            public static void Prefix(RegionGrid __instance, IntVec3 c, Region reg)
            {
                __instance.map.AI().Notify_MapChanged();
                __instance.map.Sight().Notify_RegionChanged(c, reg);
            }
        }
    }
}
