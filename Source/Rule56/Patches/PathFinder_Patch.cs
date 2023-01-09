using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using CombatAI.Comps;
using CombatAI.Statistics;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
	public static class PathFinder_Patch
	{

		[HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning))]
		private static class PathFinder_FindPath_Patch
		{
			//private static IGridBufferedWriter gridWriter;
			private static			AIType							 aiType;
			private static          ThingComp_CombatAI               comp;
			private static          DataWriter_Path                  pathWriter;
			private static          bool                             dump;
			private static          bool                             dig;
			private static          Pawn                             pawn;
			private static          Map                              map;
			private static          PathFinder                       instance;
			private static          SightTracker.SightReader         sightReader;
			private static          AvoidanceTracker.AvoidanceReader avoidanceReader;
			private static          bool                             raiders;
			private static          int                              counter;
			private static          ArmorReport                      armor;
			private static          bool                             isPlayer;
			private static          float                            threatAtDest;
			private static          float                            visibilityAtDest;
			private static          float                            multiplier = 1.0f;
			private static readonly List<IntVec3>                    blocked    = new List<IntVec3>(128);
			private static          bool                             fallbackCall;

			private static TraverseParms original_traverseParms;
			private static PathEndMode   origina_peMode;

			[HarmonyPriority(int.MaxValue)]
			internal static bool Prefix(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, ref TraverseParms traverseParms, PathEndMode peMode, ref PathFinderCostTuning tuning, out bool __state)
			{
				if (fallbackCall)
				{
					return __state = true;
				}
				dump = false;
				if (Finder.Settings.Pather_Enabled && (aiType = traverseParms.pawn?.GetAIType() ?? AIType.legacy) != AIType.vanilla && (pawn = traverseParms.pawn) != null && pawn.Faction != null && (pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid || pawn.RaceProps.Insect))
				{
					original_traverseParms = traverseParms;
					origina_peMode         = peMode;
					// prepare the modifications
					instance = __instance;
					map      = __instance.map;
					pawn     = traverseParms.pawn;
					isPlayer = pawn.Faction.IsPlayerSafe();
					if (!isPlayer)
					{
						multiplier = 1;
					}
					else if (!pawn.Drafted)
					{
						multiplier = 0.75f;
					}
					else
					{
						multiplier = 0.25f;
					}
					// make tankier pawns unless affect.
					armor = pawn.GetArmorReport();
					if (armor.createdAt != 0)
					{
						multiplier = Maths.Max(multiplier, 1 - armor.TankInt, 0.25f);
					}
					// retrive CE elements
					pawn.TryGetSightReader(out sightReader);
					if (sightReader != null)
					{
						sightReader.armor = armor;
						threatAtDest      = sightReader.GetThreat(dest.Cell) * Finder.Settings.Pathfinding_DestWeight;

					}
					pawn.Map.GetComp_Fast<AvoidanceTracker>().TryGetReader(pawn, out avoidanceReader);
					comp = pawn.GetComp_Fast<ThingComp_CombatAI>();
					/*
                     * dump pathfinding data
                     */
					if (dump = Finder.Settings.Debug_DebugDumpData && !isPlayer && sightReader != null && avoidanceReader != null && Finder.Settings.Debug && Prefs.DevMode)
					{
						//if (gridWriter == null)
						//{
						//    gridWriter = new IGridBufferedWriter(__instance.map, "pathing", "path_1", new string[]
						//    {
						//    "pref", "enRel", "enAbs", "frRel", "frAbs", "path", "dang"
						//    }, new Type[]
						//    {
						//    typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float)
						//    });
						//}
						//gridWriter.Clear();
						if (pathWriter == null)
						{
							pathWriter = new DataWriter_Path("pathing_csv", "pather_1");
						}
						pathWriter.Clear();
					}

					float miningSkill = pawn.skills?.GetSkill(SkillDefOf.Mining)?.Level ?? 0f;
					if (dig = Finder.Settings.Pather_KillboxKiller && !dump && comp != null && !comp.TookDamageRecently(360) && sightReader != null && sightReader.GetAbsVisibilityToEnemies(pawn.Position) == 0 && pawn.RaceProps.Humanlike && pawn.HostileTo(map.ParentFaction) && (pawn.mindState?.duty?.def == DutyDefOf.AssaultColony || pawn.mindState?.duty?.def == DutyDefOf.AssaultThing || pawn.mindState?.duty?.def == DutyDefOf.HuntEnemiesIndividual))
					{
						raiders = true;
						TraverseParms parms = traverseParms;
						parms.canBashDoors  = true;
						parms.canBashFences = true;
						parms.mode          = TraverseMode.PassAllDestroyableThings;
						parms.maxDanger     = Danger.Unspecified;
						traverseParms       = parms;
						if (tuning == null)
						{
							tuning                                     = new PathFinderCostTuning();
							tuning.costBlockedDoor                     = 34;
							tuning.costBlockedDoorPerHitPoint          = 0;
							tuning.costBlockedWallBase                 = (int)Maths.Max(10 * Maths.Max(13 - miningSkill, 0), 24);
							tuning.costBlockedWallExtraForNaturalWalls = (int)Maths.Max(45 * Maths.Max(10 - miningSkill, 0), 45);
							tuning.costBlockedWallExtraPerHitPoint     = Maths.Max(3 - miningSkill, 0);
							tuning.costOffLordWalkGrid                 = 0;
						}
					}
					// get the visibility at the destination
					if (sightReader != null)
					{
						if (!Finder.Performance.TpsCriticallyLow)
						{
							visibilityAtDest = Maths.Min(sightReader.GetVisibilityToEnemies(dest.Cell) * Finder.Settings.Pathfinding_DestWeight, 5);
						}
						else
						{
							visibilityAtDest = Maths.Min(sightReader.GetVisibilityToEnemies(dest.Cell) * 0.875f, 5);
						}
					}
					raiders |= pawn.HostileTo(Faction.OfPlayerSilentFail);
					counter =  0;
					return __state = true;
				}
				__state = false;
				Reset();
				return true;
			}

			[HarmonyPriority(int.MinValue)]
			public static void Postfix(PathFinder __instance, ref PawnPath __result, bool __state, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode, PathFinderCostTuning tuning)
			{
				if (fallbackCall)
				{
					return;
				}
				if (dump)
				{
					if (pathWriter != null)
					{
						pathWriter.Write();
					}
				}
				if (__state)
				{
					if (Finder.Settings.Pather_KillboxKiller && dig && __result != null && !__result.nodes.NullOrEmpty() && (pawn?.RaceProps.Humanlike ?? false))
					{
						blocked.Clear();
						Thing blocker;
						if (__result.TryGetSapperSubPath(pawn, blocked, 15, 3, out IntVec3 cellBefore, out bool enemiesAhead, out bool enemiesBefore) && blocked.Count > 0 && (blocker = blocked[0].GetEdifice(map)) != null && pawn.mindState?.duty?.def != DutyDefOf.Sapper && pawn.CurJob?.def != JobDefOf.Mine)
						{
							if (tuning != null && (!enemiesAhead || enemiesBefore))
							{
								try
								{
									__result.Dispose();
									fallbackCall                               = true;
									dig                                        = false;
									tuning.costBlockedWallBase                 = Maths.Max(tuning.costBlockedWallBase * 3, 128);
									tuning.costBlockedWallExtraForNaturalWalls = Maths.Max(tuning.costBlockedWallExtraForNaturalWalls * 3, 128);
									tuning.costBlockedWallExtraPerHitPoint     = Maths.Max(tuning.costBlockedWallExtraPerHitPoint * 4, 4);
									__result                                   = __instance.FindPath(start, dest, original_traverseParms, origina_peMode, tuning);
								}
								catch (Exception er)
								{
									Log.Error($"ISMA: Error occured in FindPath fallback call {er}");
								}
								finally
								{
									fallbackCall = false;
								}
							}
							else
							{
								Job job = DigUtility.PassBlockerJob(pawn, blocker, cellBefore, true, true);
								if (job != null)
								{
									job.playerForced       = true;
									job.expiryInterval     = 3600;
									job.maxNumMeleeAttacks = 300;
									pawn.jobs.StopAll();
									pawn.jobs.StartJob(job, JobCondition.InterruptForced);
									if (enemiesAhead)
									{
										int     count       = 0;
										int     countTarget = Rand.Int % 6 + 4 + Maths.Min(blocked.Count, 10);
										Faction faction     = pawn.Faction;
										Predicate<Thing> validator = t =>
										{
											if (count < countTarget && t.Faction == faction && t is Pawn ally && !ally.Destroyed
											    && !ally.CurJobDef.Is(JobDefOf.Mine)
											    && ally.mindState?.duty?.def != DutyDefOf.Escort
											    && (sightReader == null || sightReader.GetAbsVisibilityToEnemies(ally.Position) == 0)
											    && ally.skills?.GetSkill(SkillDefOf.Mining).Level < 10)
											{
												ThingComp_CombatAI comp = ally.GetComp_Fast<ThingComp_CombatAI>();
												if (comp?.duties != null && comp.duties?.Any(DutyDefOf.Escort) == false)
												{
													Pawn_CustomDutyTracker.CustomPawnDuty custom = CustomDutyUtility.Escort(ally, pawn, 20, 100, 300 * blocked.Count + Rand.Int % 1000);
													if (custom != null)
													{
														custom.duty.locomotion = LocomotionUrgency.Sprint;
														comp.duties.StartDuty(custom);
													}
												}
												count++;
												return count == countTarget;
											}
											return false;
										};
										Verse.GenClosest.RegionwiseBFSWorker(pawn.Position, map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.InteractionCell, TraverseParms.For(pawn), validator, null, 1, 10, 40, out int _);
									}
								}
							}
						}
					}
					AvoidanceTracker tracker = pawn.Map.GetComp_Fast<AvoidanceTracker>();
					if (tracker != null)
					{
						tracker.Notify_PathFound(pawn, __result);
					}
				}
				Reset();
			}

			public static void Reset()
			{
				avoidanceReader  = null;
				raiders          = false;
				multiplier       = 1f;
				sightReader      = null;
				counter          = 0;
				dig              = false;
				threatAtDest     = 0;
				armor            = default(ArmorReport);
				instance         = null;
				visibilityAtDest = 0f;
				map              = null;
				pawn             = null;
			}

			/*
			 * Search for the vairable that is initialized by the value from the avoid grid or search for
			 * ((i > 3) ? num9 : num8) + num15;
			 *          
			 */
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
			{
				List<CodeInstruction> codes     = instructions.ToList();
				bool                  finished1 = false;
				for (int i = 0; i < codes.Count; i++)
				{
					if (!finished1)
					{
						if (codes[i].opcode == OpCodes.Stloc_S && codes[i].operand is LocalBuilder builder1 && builder1.LocalIndex == 48)
						{
							finished1 = true;
							yield return new CodeInstruction(OpCodes.Ldloc_S, 45).MoveLabelsFrom(codes[i]).MoveBlocksFrom(codes[i]); // index of cell around curIndex
							yield return new CodeInstruction(OpCodes.Ldloc_S, 3); // curIndex
							yield return new CodeInstruction(OpCodes.Ldloc_S, 15); // open cell num (after enqueue)
							yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_FindPath_Patch), nameof(GetCostOffsetAt)));
							yield return new CodeInstruction(OpCodes.Add);
						}
					}
					yield return codes[i];
				}
			}

			private static int GetCostOffsetAt(int index, int parentIndex, int openNum)
			{
				if (map != null)
				{
					int   value      = 0;
					float visibility = 0f;
					float threat     = 0f;
					float path       = 1f;
					if (sightReader != null)
					{
						visibility = sightReader.GetVisibilityToEnemies(index);
						if (visibility > visibilityAtDest)
						{
							value  += (int)(visibility * 65);
							threat =  sightReader.GetThreat(index);
							if (threat >= threatAtDest)
							{
								value += (int)(threat * 64f);
							}
						}
						if (!isPlayer)
						{
							MetaCombatAttribute attributes = sightReader.GetMetaAttributes(index);
							if ((attributes & MetaCombatAttribute.AOELarge) != MetaCombatAttribute.None)
							{
								path = 2f;
							}
						}
					}
					if (avoidanceReader != null && !isPlayer && Finder.Settings.Flank_Enabled)
					{
						if (value > 3)
						{
							value += (int)(avoidanceReader.GetPath(index) * Maths.Min(visibility, 5) * 21f * path);
						}
						else
						{
							value += (int)(Maths.Min(avoidanceReader.GetPath(index), 4) * 42 * path);
						}
						float danger = avoidanceReader.GetDanger(index);
						if (danger > 0)
						{
							value += 23;
						}
						value += (int)danger;
					}
					if (dump)
					{
						/*
						 * fields
						 * "pref", "enRel", "enAbs", "frRel", "frAbs", "path", "dang"
						 */
						//if (gridWriter != null)
						//{
						//    gridWriter["pref"].SetValue(value, index);
						//    gridWriter["enRel"].SetValue(sightReader.GetVisibilityToEnemies(index), index);
						//    gridWriter["enAbs"].SetValue(sightReader.GetAbsVisibilityToEnemies(index), index);
						//    gridWriter["frRel"].SetValue(sightReader.GetVisibilityToFriendlies(index), index);
						//    gridWriter["frAbs"].SetValue(sightReader.GetAbsVisibilityToFriendlies(index), index);
						//    gridWriter["path"].SetValue(avoidanceReader.GetPath(index), index);
						//    gridWriter["dang"].SetValue(avoidanceReader.GetDanger(index), index);
						//}
						if (pathWriter != null)
						{
							pathWriter.Push(new DataWriter_Path.PathCell
							{
								pref  = value,
								enRel = sightReader.GetVisibilityToEnemies(index),
								enAbs = sightReader.GetAbsVisibilityToEnemies(index),
								frRel = sightReader.GetVisibilityToFriendlies(index),
								frAbs = sightReader.GetAbsVisibilityToFriendlies(index),
								dang  = avoidanceReader.GetDanger(index),
								prox  = avoidanceReader.GetProximity(index),
								path  = avoidanceReader.GetPath(index)
							});
						}
					}
					//Log.Message($"{value} {sightReader != null} {sightReader.hostile != null} {sightReader.GetVisibility(index)} {sightReader.hostile.GetSignalStrengthAt(index)}");
					//map.debugDrawer.FlashCell(map.cellIndices.IndexToCell(index), sightReader.hostile.GetSignalStrengthAt(index), $"{value}_ ");
					if (value > 10f)
					{
						counter++;
						//
						// TODO make this into a maxcost -= something system
						float l1 = 350 * (1f - Mathf.Lerp(0f, 0.75f, counter / (openNum + 1f))) * (1f - Maths.Min(openNum, 5000) / 7500);
						float l2 = 250 * (1f - Mathf.Clamp01(PathFinder.calcGrid[parentIndex].knownCost / 2500));
						//we use this so the game doesn't die
						float v = Maths.Min(value, l1 + l2) * multiplier * 1;
						//map.debugDrawer.FlashCell(map.cellIndices.IndexToCell(index), v, $" {l1 + l2}");                        
						return (int)(Maths.Min(value, l1 + l2) * multiplier * Finder.P75);
						//return (int)(Maths.Min(value, 1000f) * factionMultiplier * 1);
					}
				}
				return 0;
			}
		}
	}
}
