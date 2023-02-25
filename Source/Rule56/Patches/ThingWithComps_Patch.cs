using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
    public static class ThingWithComps_Patch
    {
        [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.Destroy))]
        private static class ThingWithComps_Destroy_Patch
        {
            public static void Prefix(ThingWithComps __instance)
            {
                CompCache.Notify_ThingDestroyed(__instance);
            }
        }
    }
}
