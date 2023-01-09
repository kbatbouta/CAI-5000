using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Mono.Unix.Native;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
	internal static class AttackTargetFinder_Patch
	{
		private static			AIType					 aiType;
		private static          Map                      map;
		private static          Pawn                     searcherPawn;
		private static          Faction                  searcherFaction;
		private static          Verb                     searcherVerb;
		private static          float                    dmg05;
		private static          ProjectileProperties     projectile;
		private static          SightTracker.SightReader sightReader;
		private static          DamageReport             damageReport;
		private static readonly Dictionary<int, float>   distDict = new Dictionary<int, float>(256);

		[HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget))]
		internal static class AttackTargetFinder_BestAttackTarget_Patch
		{
			internal static void Prefix(IAttackTargetSearcher searcher)
			{
				map = searcher.Thing?.Map;

				if (searcher.Thing is Pawn pawn && !pawn.RaceProps.Animal && (aiType = pawn.GetAIType()) != AIType.vanilla)
				{
					distDict.Clear();
					damageReport = DamageUtility.GetDamageReport(searcher.Thing);
					searcherPawn = pawn;
					searcherVerb = pawn.CurrentEffectiveVerb;
					pawn.TryGetSightReader(out sightReader);
					if (sightReader != null)
					{
						int num = 0;
//						if (Find.Selector.SelectedPawns.Contains(pawn))
//						{
//							map.debugDrawer.FlashCell(pawn.Position, 0.1f, "s");
//						}
						Func<Region, int, bool> action = (region, depth) =>
						{
							List<Thing> things = region.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);
							depth = Maths.Min(depth, 45);
							for (int i = 0; i < things.Count; i++)
							{
								if (things[i] != null)
								{
									num++;
									distDict[things[i].thingIDNumber] = depth;
//									if (Find.Selector.SelectedPawns.Contains(pawn))
//									{
//										map.debugDrawer.FlashCell(things[i].Position, 0.9f, $"{depth}");
//									}
								}
							}
							return num >= 32;
						};
						float               costConst = !Mod_CE.active ? 7.5f : 2.5f;
						Func<Region, float> cost = region =>
						{
							return Maths.Min(sightReader.GetRegionAbsVisibilityToEnemies(region), 10) * Mathf.Clamp(sightReader.GetRegionThreat(region) + 0.5f, 1.0f, 2.0f) * costConst;
						};
						RegionFlooder.Flood(pawn.Position, pawn.Position, pawn.Map, action, null,  cost, maxRegions: !Finder.Performance.TpsCriticallyLow ? 200 : 75);
					}
					if (searcherVerb != null && !searcherVerb.IsMeleeAttack && (projectile = searcherVerb.GetProjectile()?.projectile ?? null) != null)
					{
						dmg05 = projectile.damageAmountBase / 2f;
					}
				}
				searcherFaction = searcher.Thing?.Faction ?? null;
			}

			internal static void Postfix()
			{
				map          = null;
				searcherFaction = null;
				searcherVerb = null;
				dmg05 = 0;
				projectile = null;
				damageReport = default(DamageReport);
				searcherPawn = null;
				sightReader  = null;
				distDict.Clear();
			}
		}

		[HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.GetShootingTargetScore))]
		internal static class Harmony_AttackTargetFinder_GetShootingTargetScore
		{
			internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				List<CodeInstruction> codes    = instructions.ToList();
				bool                  finished = false;
				for (int i = 0; i < instructions.Count(); i++)
				{
					if (!finished)
					{
						if (codes[i].opcode == OpCodes.Ldc_R4)
						{
							finished = true;
							yield return new CodeInstruction(OpCodes.Ldarg_0);
							yield return new CodeInstruction(OpCodes.Ldarg_1);
							yield return new CodeInstruction(OpCodes.Ldarg_2);
							yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_AttackTargetFinder_GetShootingTargetScore), nameof(GetTargetBaseScore)));
							continue;
						}
					}
					yield return codes[i];
				}
			}

			public static float GetTargetBaseScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
			{
				float result = 60f;
				if (Finder.Settings.Targeter_Enabled && sightReader != null && searcherPawn != null)
				{
					if (distDict.TryGetValue(target.Thing.thingIDNumber, out float intCost))
					{
						result += 45 - intCost;
					}
					else if(target.Thing is Building_Turret)
					{
						result += 22;
					}
					if (verb.IsMeleeAttack || verb.EffectiveRange <= 15)
					{
						if (sightReader.GetAbsVisibilityToEnemies(target.Thing.Position) > sightReader.GetAbsVisibilityToFriendlies(target.Thing.Position) + 1)
						{
							result -= 30f * Finder.P50;
						}
						if (sightReader.GetVisibilityToEnemies(target.Thing.Position) > 3)
						{
							result -= 15f * Finder.P50;
						}
						result += sightReader.GetEnemyDirection(target.Thing.Position).sqrMagnitude - Mathf.Pow(sightReader.GetVisibilityToEnemies(target.Thing.Position), 2);
					}
					if (target.Thing is Pawn enemy)
					{
						if (damageReport.IsValid)
						{
							ArmorReport armorReport = enemy.GetArmorReport();
							float       diff        = 0f;
							if (Mod_CE.active)
							{
								diff = Mathf.Clamp01(Maths.Max(damageReport.adjustedBlunt / (armorReport.Blunt + 1f), damageReport.adjustedSharp / (armorReport.Sharp + 1f), 0f));
							}
							else
							{
								diff = Mathf.Clamp01(1 - Maths.Max(armorReport.Blunt - damageReport.adjustedBlunt, armorReport.Sharp - damageReport.adjustedSharp, 0f));
							}
							result += 8f * diff;
						}
						Thing targeted;
						if (enemy.stances?.stagger != null && enemy.stances.stagger.Staggered)
						{
							result += 12;
						}
						if (!verb.IsMeleeAttack)
						{
							if (enemy.CurrentEffectiveVerb?.IsMeleeAttack ?? false)
							{
								if (searcherFaction != null && (targeted = enemy.jobs?.curJob?.targetA.Thing)?.Faction == searcherFaction)
								{
									result += 8 + enemy.Position.DistanceToSquared(targeted.Position) < 150 ? 16 : 0;
								}
							}
							if (enemy.pather?.MovingNow ?? false)
							{
								result += 16;
							}
						}
						if (!enemy.HasWeaponVisible())
						{
							result -= 8;
						}
					}
				}
				return result;
			}
		}
	}
}
