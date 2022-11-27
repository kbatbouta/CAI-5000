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
		//[HarmonyPatch(typeof(JobDriver_Wait), nameof(JobDriver_Wait.MakeNewToils))]
		//private static class JobDriver_Wait_MakeNewToils_Patch
		//{
		//	public static void Postfix(JobDriver_Wait __instance)
		//	{
		//		if (__instance.job.def.alwaysShowWeapon && __instance.pawn.mindState?.enemyTarget != null && __instance.job.def == JobDefOf.Wait_Combat && __instance.GetType() == typeof(JobDriver_Wait))
		//		{
		//			__instance.AddEndCondition(() =>
		//			{
		//				if (!__instance.pawn.IsHashIntervalTick(30))
		//				{
		//					return JobCondition.Ongoing;
		//				}
		//				if (__instance.pawn.mindState?.enemyTarget is Pawn enemy)
		//				{
		//					ThingComp_CombatAI comp = __instance.pawn.GetComp_Fast<ThingComp_CombatAI>();
		//					if (comp.waitJob == __instance.job)
		//					{
		//						//if(enemy.jobs?.curJob != null)
		//						//if (__instance.TargetB.IsValid && (enemy.jobs.curJob.targetA.Thing == __instance.TargetB.Thing || enemy.jobs.curJob.targetA.Cell == __instance.TargetB.Cell))
		//						//{		
		//						//	return JobCondition.Ongoing;
		//						//}
		//						if (__instance.job.verbToUse != null)
		//						{
		//							if (__instance.job.verbToUse.CanHitTarget(enemy.GetMovingShiftedPosition(15)))
		//							{
		//								return JobCondition.Ongoing;
		//							}
		//							if (__instance.job.verbToUse.CanHitTarget(enemy.GetMovingShiftedPosition(120)))
		//							{
		//								return JobCondition.Ongoing;
		//							}
		//						}
		//						//__instance.Map.debugDrawer.FlashCell(__instance.pawn.Position, duration: 100);
		//						return JobCondition.Succeeded;
		//					}
		//				}						
		//				return JobCondition.Ongoing;
		//			});
		//		}
		//	}
		//}
	}
}

