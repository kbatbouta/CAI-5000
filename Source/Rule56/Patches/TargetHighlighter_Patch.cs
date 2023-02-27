using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
namespace CombatAI.Patches
{
    public static class TargetHighlighter_Patch
    {
        [HarmonyPatch(typeof(TargetHighlighter), nameof(TargetHighlighter.Highlight))]
        private static class TargetHighlighter_Highlight
        {
            public static bool Prefix(GlobalTargetInfo target)
            {
                if (Finder.Settings.FogOfWar_Enabled && target is { IsValid: true, IsMapTarget: true } && target.Map != null)
                {
                    return !target.Map?.GetComp_Fast<MapComponent_FogGrid>()?.IsFogged(target.Cell) ?? true;
                }
                return true;
            }
        }
    }
}
