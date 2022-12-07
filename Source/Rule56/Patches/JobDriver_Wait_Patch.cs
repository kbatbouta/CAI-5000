using System;
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
				if (__instance.job.def.alwaysShowWeapon && __instance.pawn.mindState?.enemyTarget != null && __instance.job.def == JobDefOf.Wait_Combat && __instance.GetType() == typeof(JobDriver_Wait))
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
						if (__instance.pawn.mindState?.enemyTarget is Pawn enemy)
						{
							ThingComp_CombatAI comp = __instance.pawn.GetComp_Fast<ThingComp_CombatAI>();
							if (comp?.waitJob == __instance.job)
							{
								if (__instance.job.verbToUse != null)
								{
									if (__instance.pawn.stances?.curStance is Stance_Warmup || __instance.job.verbToUse.Bursting || Mod_CE.IsAimingCE(__instance.job.verbToUse))
									{
										return JobCondition.Ongoing;
									}
									if (__instance.job.verbToUse.CanHitTarget(PawnPathUtility.GetMovingShiftedPosition(enemy, 40)))
									{
										return JobCondition.Ongoing;
									}
									if (__instance.job.verbToUse.CanHitTarget(PawnPathUtility.GetMovingShiftedPosition(enemy, 80)))
									{
										return JobCondition.Ongoing;
									}
								}
								comp.Notify_WaitJobEnded();
								comp.waitJob = null;
								return JobCondition.Succeeded;
							}
						}
						return JobCondition.Ongoing;
					});
				}
			}
		}
	}
}
