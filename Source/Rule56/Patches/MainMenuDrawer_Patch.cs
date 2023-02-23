using CombatAI.Gui;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class MainMenuDrawer_Patch
    {
        private static bool quickSetupInited = false; 
        
        [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.Init))]
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
        
        [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
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
