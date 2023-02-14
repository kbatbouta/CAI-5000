using CombatAI.Comps;
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
				if (__instance.job.Is(JobDefOf.Wait_Combat) && __instance.job.endIfCantShootTargetFromCurPos && !__instance.pawn.Faction.IsPlayerSafe())
				{
					if (__instance.job.targetC.IsValid)
					{
						__instance.rotateToFace = TargetIndex.C;
					}
					__instance.AddEndCondition(() =>
					{
						if (!__instance.pawn.IsHashIntervalTick(30) || GenTicks.TicksGame - __instance.startTick < 30)
						{
							return JobCondition.Ongoing;
						}
						Verb verb = __instance.pawn.CurrentEffectiveVerb;
						if (verb == null || __instance.pawn.Faction.IsPlayerSafe())
						{
							// just skip if something is not right.
							return JobCondition.Ongoing;
						}
						LocalTargetInfo target = verb.currentTarget.IsValid ? verb.currentTarget : (__instance.pawn.mindState?.enemyTarget ?? null);
						if (target.IsValid)
						{
							if (verb.CanHitTarget(target))
							{
								return JobCondition.Ongoing;
							}
							if (target.Thing is Pawn { Dead: false, Downed: false } pawn && verb.CanHitTarget(PawnPathUtility.GetMovingShiftedPosition(pawn, 60)))
							{
								return JobCondition.Ongoing;
							}
							return JobCondition.Succeeded;
						}
						return JobCondition.Succeeded;
					});
				}
			}
		}
	}
}
