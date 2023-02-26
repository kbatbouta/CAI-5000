using System;
using Verse;
namespace CombatAI.Patches
{
    public static class HarmonyPatches
    {
        public static void Initialize()
        {
            // queue patches
            LongEventHandler.QueueLongEvent(PatchAll, "CombatAI.Preparing", false, OnError);
            // manual patches
            MainMenuDrawer_Patch.Patch();
        }

        private static void PatchAll()
        {
            Log.Message("ISMA: Applying patches");
            Finder.Harmony.PatchAll();
        }

        private static void OnError(Exception exception)
        {
            Log.Error(exception.ToString());
            Log.Error(exception.Message);
        }
    }
}
