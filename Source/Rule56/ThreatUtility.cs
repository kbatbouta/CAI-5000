using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI
{
	public static class ThreatUtility
	{
		public static void CalculateThreat(Pawn selPawn, List<Thing> inEnemies, out float possibleDmg, out float possibleDmgDistance, out float possibleDmgWarmup, out Thing nearestEnemy, out float nearestEnemyDist, out Pawn nearestEnemyMelee, out float nearestEnemyMeleeDist, ref int progress)
		{
			CalculateThreat(selPawn, inEnemies, selPawn.GetArmorReport(), null, null, out possibleDmg, out possibleDmgDistance, out possibleDmgWarmup, out nearestEnemy, out nearestEnemyDist, out nearestEnemyMelee, out nearestEnemyMeleeDist, ref progress);
		}

		public static void CalculateThreat(Pawn selPawn, List<Thing> inEnemies, ArmorReport inArmorReport, List<Thing> outEnemiesRanged, List<Thing> outEnemiesMelee, out float possibleDmg, out float possibleDmgDistance, out float possibleDmgWarmup, out Thing nearestEnemy, out float nearestEnemyDist, out Pawn nearestEnemyMelee, out float nearestEnemyMeleeDist, ref int progress)
		{
			nearestEnemyDist      = 1e6f;
			nearestEnemy          = null;
			nearestEnemyMeleeDist = 1e6f;
			nearestEnemyMelee     = null;
			IntVec3 selPos = selPawn.Position;
			possibleDmg         = 0;
			possibleDmgWarmup   = 0;
			possibleDmgDistance = 0;
			for (int i = 0; i < inEnemies.Count; i++)
			{
				Thing enemy = inEnemies[i];
#if DEBUG_REACTION
				if (enemy == null)
				{
					Log.Error("Found null thing (2)");
					continue;
				}
#endif
				// For debugging and logging.
				progress = 80;
				if (GetEnemyAttackTargetId(enemy) == selPawn.thingIDNumber)
				{
					DamageReport damageReport = DamageUtility.GetDamageReport(enemy);
					Pawn         enemyPawn    = enemy as Pawn;
					if (damageReport.IsValid && (enemyPawn == null || enemyPawn.mindState?.MeleeThreatStillThreat == false))
					{
						float dist = enemyPawn == null ? selPawn.DistanceTo_Fast(enemy) : selPos.DistanceTo_Fast(PawnPathUtility.GetMovingShiftedPosition(enemyPawn, 120f));
						if (dist < nearestEnemyDist)
						{
							nearestEnemyDist = dist;
							nearestEnemy     = enemy;
						}
						if (!damageReport.primaryIsRanged)
						{
							if (dist < nearestEnemyMeleeDist)
							{
								nearestEnemyMeleeDist = dist;
								nearestEnemyMelee     = enemyPawn;
							}
							outEnemiesMelee?.Add(enemy);
						}
						progress = 81;
						float damage = damageReport.SimulatedDamage(inArmorReport);
						if (!damageReport.primaryIsRanged)
						{
							damage *= (5f - Mathf.Clamp(Maths.Sqrt_Fast(dist * dist, 4), 0f, 5f)) / 5f;
						}
						possibleDmg         += damage;
						possibleDmgDistance += dist;
						if (damageReport.primaryIsRanged)
						{
							possibleDmgWarmup += damageReport.primaryVerbProps.warmupTime;
							outEnemiesRanged?.Add(enemy);
						}
						progress = 82;
					}
				}
			}
		}
		
		internal static int GetEnemyAttackTargetId(Thing enemy)
		{
			if (!TKVCache<Thing, LocalTargetInfo, int>.TryGet(enemy, out int attackTarget, 15) || attackTarget == -1)
			{
				Verb enemyVerb = enemy.TryGetAttackVerb();
				if (enemyVerb == null || enemyVerb is Verb_CastPsycast || enemyVerb is Verb_CastAbility)
				{
					attackTarget = -1;
				}
				else if (!enemyVerb.IsMeleeAttack && enemyVerb.currentTarget is { IsValid: true, HasThing: true } && (enemyVerb.WarmingUp && enemyVerb.WarmupTicksLeft < 60 || enemyVerb.Bursting))
				{
					attackTarget = enemyVerb.currentTarget.Thing.thingIDNumber;
				}
				else if (enemyVerb.IsMeleeAttack && enemy is Pawn enemyPawn && enemyPawn.CurJobDef.Is(JobDefOf.AttackMelee) && enemyPawn.CurJob.targetA.IsValid)
				{
					attackTarget = enemyPawn.CurJob.targetA.Thing.thingIDNumber;
				}
				else
				{
					attackTarget = -1;
				}
				TKVCache<Thing, LocalTargetInfo, int>.Put(enemy, attackTarget);
			}
			return attackTarget;
		}
	}
}
