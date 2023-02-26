using Verse;
namespace CombatAI.Patches
{
    public static class HarmonyPatches
    {
        public static void Initialize()
        {
            // queue patches
            LongEventHandler.QueueLongEvent(Finder.Harmony.PatchAll, "CombatAI.Preparing", false, null);
            // manual patches
            MainMenuDrawer_Patch.Patch();
        }
    }
}
