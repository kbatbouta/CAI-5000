using HarmonyLib;
using RimWorld;
namespace CombatAI.Patches
{
    public static class MainTabWindow_Architect_Patch
    {
        [HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.CacheDesPanels))]
        private static class MainTabWindow_Architect_CacheDesPanels_Patch
        {
            public static void Postfix(MainTabWindow_Architect __instance)
            {
                if (!Finder.Settings.FogOfWar_Enabled)
                {
                    __instance.desPanelsCached.RemoveAll(t => t.def == CombatAI_DesignationCategoryDefOf.Intelligences);
                }
            }
        }
    }
}
