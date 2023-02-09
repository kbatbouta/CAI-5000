using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
    public class Pawn_JobTracker_Patch
    {
        [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
        public static class Pawn_JobTracker_TryTakeOrderedJob_Patch
        {
            public static void Postfix(Pawn_JobTracker __instance)
            {
                if (__instance.pawn.Faction.IsPlayerSafe() && __instance.pawn.GetComp<ThingComp_CombatAI>() is var comp)
                {
                    comp.forcedTarget = LocalTargetInfo.Invalid;
                }
            }
        } 
    }
}
