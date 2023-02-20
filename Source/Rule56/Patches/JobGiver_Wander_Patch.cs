using CombatAI.Comps;
using HarmonyLib;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
    public static class JobGiver_Wander_Patch
    {
        [HarmonyPatch(typeof(JobGiver_Wander), nameof(JobGiver_Wander.TryGiveJob))]
        private static class JobGiver_Wander_TryGiveJob_Patch
        {
            public static bool Prefix(JobGiver_Wander __instance, Pawn pawn)
            {
                // skip if the pawn is firing or warming up
                if (pawn.stances?.curStance is Stance_Warmup)
                {
                    return false;
                }
                // don't skip unless it's JobGiver_WanderNearDutyLocation
                if (!(__instance is JobGiver_WanderNearDutyLocation))
                {
                    return true;
                }
                ThingComp_CombatAI comp = pawn.GetComp_Fast<ThingComp_CombatAI>();
                if (comp != null)
                {
                    if(comp.data.InterruptedRecently(600) || comp.data.RetreatedRecently(600))
                    {
                        return false;
                    }
                    if (comp.sightReader != null && comp.sightReader.GetVisibilityToEnemies(pawn.Position) > 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
