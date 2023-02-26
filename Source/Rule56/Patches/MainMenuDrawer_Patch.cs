using CombatAI.Gui;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class MainMenuDrawer_Patch
    {
        private static bool quickSetupInited;

        public static void Patch()
        {
            Finder.Harmony.Patch(AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.Init)),
                                 postfix: new HarmonyMethod(AccessTools.Method(typeof(MainMenuDrawer_Init_Patch), nameof(MainMenuDrawer_Init_Patch.Postfix))));
            Finder.Harmony.Patch(AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls)),
                                 prefix: new HarmonyMethod(AccessTools.Method(typeof(MainMenuDrawer_DoMainMenuControls_Patch), nameof(MainMenuDrawer_DoMainMenuControls_Patch.Prefix))));
        }
        
        private static class MainMenuDrawer_Init_Patch
        {
            public static void Postfix()
            {
                if (!quickSetupInited && !Finder.Settings.FinishedQuickSetup && !Find.WindowStack.IsOpen<Window_QuickSetup>())
                {
                    quickSetupInited = true;
                    Find.WindowStack.Add(new Window_QuickSetup());
                }
            }
        }
        
        private static class MainMenuDrawer_DoMainMenuControls_Patch
        {
            public static bool Prefix()
            {
                if (quickSetupInited && Find.WindowStack.IsOpen<Window_QuickSetup>())
                {
                    return false;
                }
                return true;
            }
        }
    }
}
