using System;
using Verse;
namespace CombatAI.Patches
{
    public static class HarmonyPatches
    {
        public static void Initialize()
        {
            // queue patches
            LongEventHandler.QueueLongEvent(PatchAll, "CombatAI.Preparing", false, null);
            // manual patches
            MainMenuDrawer_Patch.Patch();
        }

        private static void PatchAll()
        {
            Log.Message("ISMA: Applying patches");
            Finder.Harmony.PatchAll();
        }
    }
}
