using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class FloatMenuMakerMap_Patch
    {
        [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.PawnGotoAction))]
        public static class FloatMenuMakerMap_PawnGotoAction_Patch
        {
            public static void Postfix(Pawn pawn)
            {
                if (pawn.Faction.IsPlayerSafe())
                {
                    pawn.GetComp<ThingComp_CombatAI>().forcedTarget = LocalTargetInfo.Invalid;
                }
            }
        }
    }
}
