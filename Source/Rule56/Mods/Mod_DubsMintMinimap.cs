using System.Reflection;
using HarmonyLib;
using Verse;
namespace CombatAI
{
    [LoadIf("dubwise.dubsmintminimap")]
    public class Mod_DubsMintMinimap
    {
        public static bool active;

        [Unsaved]
        private static MapComponent_FogGrid fogGrid;

        [LoadNamed("DubsMintMinimap.MainTabWindow_MiniMap:Fogged", LoadableType.Method, new[]
        {
            typeof(Thing)
        })]
        public static MethodInfo Fogged;

        [LoadNamed("DubsMintMinimap.MainTabWindow_MiniMap:DrawAllPawns", LoadableType.Method)]
        public static MethodInfo DrawAllPawns;

        [RunIf(loaded: true)]
        private static void OnActive()
        {
            Finder.Harmony.Patch(DrawAllPawns, new HarmonyMethod(AccessTools.Method(typeof(Mod_DubsMintMinimap), nameof(PreDrawAllPawns))), new HarmonyMethod(AccessTools.Method(typeof(Mod_DubsMintMinimap), nameof(PostDrawAllPawns))));
            Finder.Harmony.Patch(Fogged, new HarmonyMethod(AccessTools.Method(typeof(Mod_DubsMintMinimap), nameof(IsFogged))));
        }

        private static void PreDrawAllPawns()
        {
            if (Finder.Settings.FogOfWar_Enabled)
            {
                fogGrid = Find.CurrentMap?.GetComp_Fast<MapComponent_FogGrid>() ?? null;
            }
        }

        private static void PostDrawAllPawns()
        {
            fogGrid = null;
        }

        private static bool IsFogged(Thing thing, ref bool __result)
        {
            if (fogGrid != null && fogGrid.IsFogged(thing.Position))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
