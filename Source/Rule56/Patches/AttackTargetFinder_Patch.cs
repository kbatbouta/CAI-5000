using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
	internal static class AttackTargetFinder_Patch
	{
		private static Map                      map;
		private static Pawn                     searcherPawn;
		private static Faction                  searcherFaction;
		private static Verb                     searcherVerb;
		private static float                    dmg05;
		private static ProjectileProperties     projectile;
		private static SightTracker.SightReader sightReader;
		private static DamageReport             damageReport;

		[HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget))]
		internal static class AttackTargetFinder_BestAttackTarget_Patch
		{
			internal static void Prefix(IAttackTargetSearcher searcher)
			{
				map = searcher.Thing?.Map;

				if (searcher.Thing is Pawn pawn && !pawn.RaceProps.Animal)
				{
					damageReport = DamageUtility.GetDamageReport(searcher.Thing);
					searcherPawn = pawn;
					searcherVerb = pawn.CurrentEffectiveVerb;
					if (searcherVerb != null && !searcherVerb.IsMeleeAttack && (projectile = searcherVerb.GetProjectile()?.projectile ?? null) != null)
					{
						dmg05 = projectile.damageAmountBase / 2f;
					}
					pawn.TryGetSightReader(out sightReader);
				}
				searcherFaction = searcher.Thing?.Faction ?? null;
			}

			internal static void Postfix()
			{
				map          = null;
				damageReport = default(DamageReport);
				searcherPawn = null;
				sightReader  = null;
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
							//var armor = armorReport.GetArmor(projectile.damageDef);
							//var damage = damageReport.GetAdjustedDamage(projectile.damageDef.armorCategory);
							//if (projectile.armorPenetrationBase > 0)
							//{
							//    result += Mathf.Lerp(0f, 12f, damageReport.ad);
							//if (Find.Selector.SelectedPawns.Contains(searcher.Thing as Pawn))
							//{
							//    map.debugDrawer.FlashCell(target.Thing.Position, diff, $"{diff}");
							//}                            
							//else
							//{
							//    result -= Maths.Min(armor * 0.5f, 8f);
							//}
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
