using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
	public static class JobGiver_AIGotoNearestHostile_Patch
	{
		[HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), nameof(JobGiver_AIGotoNearestHostile.TryGiveJob))]
		private static class JobGiver_AIGotoNearestHostile_TryGiveJob_Patch
		{
			public static bool Prefix(Pawn pawn, ref Job __result)
			{
				if (pawn.TryGetSightReader(out SightTracker.SightReader reader))
				{
					Thing nearestEnemy = null;
					WeightedRegionFlooder.Flood(pawn.Position,
					                            pawn.mindState.enemyTarget == null ? pawn.Position : pawn.mindState.enemyTarget.Position, pawn.Map,
					                            (region, depth) =>
					                            {
						                            if (reader.GetRegionAbsVisibilityToEnemies(region) > 0)
						                            {
							                            List<Thing> things = region.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);
							                            if (things != null)
							                            {
								                            for (int i = 0; i < things.Count; i++)
								                            {
									                            Thing thing = things[i];
									                            Pawn  other;
									                            if (thing is IAttackTarget target && !target.ThreatDisabled(pawn) && AttackTargetFinder.IsAutoTargetable(target) && thing.HostileTo(pawn) && ((other = thing as Pawn) == null || other.IsCombatant()))
									                            {
										                            nearestEnemy = thing;
										                            return true;
									                            }
								                            }
							                            }
						                            }
						                            return false;
					                            },
					                            cost: region =>
					                            {
						                            return Maths.Min(reader.GetRegionAbsVisibilityToEnemies(region), 8 * Finder.P50) * 10;
					                            });
					if (nearestEnemy != null)
					{
						Job job = JobMaker.MakeJob(JobDefOf.Goto, nearestEnemy);
						job.checkOverrideOnExpire = true;
						job.expiryInterval        = 500;
						job.collideWithPawns      = true;
						__result                  = job;
						return false;
					}
				}
				return true;
			}
		} 
	}
}
