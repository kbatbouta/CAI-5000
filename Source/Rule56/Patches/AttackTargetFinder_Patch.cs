using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
	internal static class AttackTargetFinder_Patch
	{
		private static          Map                      map;
		private static          Pawn                     searcherPawn;
		private static          Faction                  searcherFaction;
		private static          SightTracker.SightReader sightReader;
		private static          DamageReport             damageReport;
		private static readonly Dictionary<int, float>   distDict = new Dictionary<int, float>(256);

		public static void ClearCache()
		{
			distDict.Clear();
		}

		[HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget))]
		internal static class AttackTargetFinder_BestAttackTarget_Patch
		{
			internal static void Prefix(IAttackTargetSearcher searcher)
			{
				map = searcher.Thing?.Map;

				if (searcher.Thing is Pawn pawn && !pawn.RaceProps.Animal)
				{
					distDict.Clear();
					damageReport = DamageUtility.GetDamageReport(searcher.Thing);
					searcherPawn = pawn;
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
					if (target.Thing is Building_Turret)
					{
						result += 22;
					}
					if (target.Thing is Pawn enemy)
					{
						if (damageReport.IsValid)
						{
							ArmorReport armorReport = enemy.GetArmorReport();
							float       diff;
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
						if (!TKVCache<int, AttackTargetFinderCache, int>.TryGet(enemy.thingIDNumber, out int offset, 45))
						{
							offset = 0;
							if (enemy.stances?.stagger != null && enemy.stances.stagger.Staggered)
							{
								offset += 12;
							}
							DamageReport enemyReport = DamageUtility.GetDamageReport(enemy);
							if (enemyReport is { IsValid: true, primaryIsRanged: false })
							{
								Thing targeted;
								if (searcherFaction != null && (targeted = enemy.jobs?.curJob?.targetA.Thing)?.Faction == searcherFaction)
								{
									offset += 8 + enemy.Position.DistanceToSquared(targeted.Position) < 150 ? 16 : 0;
								}
							}
							if (enemy.pather?.MovingNow ?? false)
							{
								offset += 16;
							}
							TKVCache<int, AttackTargetFinderCache, int>.Put(enemy.thingIDNumber, offset);
						}
						result += offset;
					}
				}
				return result;
			}
		}

		[UsedImplicitly]
		private sealed class AttackTargetFinderCache
		{
		}
	}
}
