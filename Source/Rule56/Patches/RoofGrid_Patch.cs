using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
    public static class RoofGrid_Patch
    {
        [HarmonyPatch(typeof(Verse.RoofGrid), nameof(Verse.RoofGrid.SetRoof))]
        private static class RoofGrid_SetRoof_Patch
        {
            public static void Prefix(Verse.RoofGrid __instance, IntVec3 c, RoofDef def)
            {
                __instance.map.GetComp_Fast<RoofGrid>().SetRoof(c, def);
            }
        }
    }
}
