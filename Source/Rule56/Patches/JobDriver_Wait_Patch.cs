using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
    public static class JobDriver_Wait_Patch
    {
        [HarmonyPatch(typeof(JobDriver_Wait), nameof(JobDriver_Wait.MakeNewToils))]
        private static class JobDriver_Wait_MakeNewToils_Patch
        {
            public static void Postfix(JobDriver_Wait __instance)
            {
                if (__instance.job.Is(JobDefOf.Wait_Combat) && !__instance.pawn.Faction.IsPlayerSafe())
                {
                    if (__instance.job.targetC.IsValid)
                    {
                        __instance.rotateToFace = TargetIndex.C;
                    }
                    __instance.AddFailCondition(() =>
                    {
                        if (!__instance.pawn.IsHashIntervalTick(30) || GenTicks.TicksGame - __instance.startTick < 30)
                        {
                            return false;
                        }
                        Verb verb = __instance.job.verbToUse ?? __instance.pawn.CurrentEffectiveVerb;
                        if (verb == null || verb.WarmingUp || verb.Bursting || __instance.pawn.Faction.IsPlayerSafe())
                        {
                            // just skip the fail check if something is not right.
                            return false;
                        }
                        LocalTargetInfo target = verb.currentTarget.IsValid ? verb.currentTarget : __instance.pawn.mindState?.enemyTarget ?? null;
                        if (target.IsValid)
                        {
                            if (target.Thing is Pawn { Dead: false, Downed: false } pawn)
                            {
                                if (verb.CanHitTarget(PawnPathUtility.GetMovingShiftedPosition(pawn, 60)))
                                {
                                    return false;
                                }
                            }
                            else if (verb.CanHitTarget(target))
                            {
                                return false;
                            }
                            return true;
                        }
                        return __instance.job.endIfCantShootTargetFromCurPos;
                    });
                }
            }
        }
    }
}
