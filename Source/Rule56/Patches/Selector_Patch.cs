using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class Selector_Patch
    {
        [HarmonyPatch(typeof(Selector), nameof(Selector.SelectInternal))]
        public static class Selector__Patch
        {
            public static bool Prefix(object obj)
            {
                Map map;
                if (Finder.Settings.FogOfWar_Enabled && obj is Pawn pawn && !pawn.Destroyed && pawn.Spawned && (map = pawn.Map) != null)
                {
                    return !map.GetComp_Fast<MapComponent_FogGrid>().IsFogged(pawn.Position);
                }
                return true;
            }
        }
    }
}
