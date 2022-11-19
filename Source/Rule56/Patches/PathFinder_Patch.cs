using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using CombatAI.Comps;
using CombatAI.Statistics;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityEngine.UI;
using Verse;
using Verse.AI;

namespace CombatAI.Patches
{
    public static class PathFinder_Patch {

        [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning) })]
        static class PathFinder_FindPath_Patch
        {
            //private static IGridBufferedWriter gridWriter;
            private static DataWriter_Path pathWriter;
            private static bool dump;
            private static Pawn pawn;
            private static Map map;
            private static PathFinder instance;
            private static SightTracker.SightReader sightReader;
            private static AvoidanceTracker.AvoidanceReader avoidanceReader;            
            private static bool raiders;            
            private static int counter;
            // private static bool crouching;
            // private static bool tpsLow;
            // private static float tpsLevel;
            //private static int pawnBlockingCost;
            private static bool isPlayer;
            private static float visibilityAtDest;
            private static float factionMultiplier = 1.0f;

            internal static bool Prefix(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, ref TraverseParms traverseParms, PathEndMode peMode, ref PathFinderCostTuning tuning, out bool __state)
            {
                dump = false;
                //pawnBlockingCost = 175;
                if (Finder.Settings.Pather_Enabled && (pawn = traverseParms.pawn) != null && pawn.Faction != null && (pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid || pawn.RaceProps.Insect))
                {                    
                    //Log.Message($"{traverseParms.pawn}");
                    // prepare the performance parameters.
                    //tpsLevel = PerformanceTracker.TpsLevel;
                    //tpsLow = PerformanceTracker.TpsCriticallyLow;

                    // prepare the modifications
                    instance = __instance;
                    map = __instance.map;
                    pawn = traverseParms.pawn;
                    // fix for player pawns and drafted pawns
                    isPlayer = pawn.Faction.IsPlayerSafe();

                    factionMultiplier = isPlayer ? (pawn.Drafted ? 0.25f : 0.75f) : 1.0f;                    
                    // retrive CE elements                    
                    pawn.GetSightReader(out sightReader);
                    pawn.Map.GetComp_Fast<AvoidanceTracker>().TryGetReader(pawn, out avoidanceReader);

                    /*  
                     * dump pathfinding data
                     */
                    if (dump = !isPlayer && sightReader != null && avoidanceReader != null && Finder.Settings.Debug && Finder.Settings.Debug_DebugDumpData && Prefs.DevMode)
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
                    if (Finder.Settings.Pather_KillboxKiller && !dump && pawn.RaceProps.Humanlike && pawn.HostileTo(map.ParentFaction) && (pawn.mindState?.duty?.def == DutyDefOf.AssaultColony || pawn.mindState?.duty?.def == DutyDefOf.AssaultThing || pawn.mindState?.duty?.def == DutyDefOf.HuntEnemiesIndividual))
                    {
                        raiders = true;
                        //factionMultiplier = 1;
                        TraverseParms parms = traverseParms;
                        parms.canBashDoors = true;
                        parms.canBashFences = true;
                        parms.mode = TraverseMode.PassAllDestroyableThings;
                        parms.maxDanger = Danger.Unspecified;
                        traverseParms = parms;
                        if (tuning == null)
                        {
                            tuning = new PathFinderCostTuning();                            
                            tuning.costBlockedDoor = 34;
                            tuning.costBlockedDoorPerHitPoint = 0;
                            tuning.costBlockedWallBase = (int)Maths.Max(15 * Maths.Max(10 - miningSkill, 0), 24);
                            tuning.costBlockedWallExtraForNaturalWalls = (int)Maths.Max(45 * Maths.Max(15 - miningSkill, 0), 45);
                            tuning.costBlockedWallExtraPerHitPoint = Maths.Max(6 - miningSkill, 0);
                            tuning.costOffLordWalkGrid = 0;
                        }
                    }
                   
                    //pawn.Map.GetComp_Fast<SightTracker>().TryGetReader(pawn, out sightReader);

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
                        //Verb verb = pawn.GetWeaponVerbWithFallback();
                        //if (verb != null)
                        //{
                        //    if (verb.verbProps.range > 20)
                        //    {
                        //        visibilityAtDest *= 1.225f;
                        //    }
                        //    else if (verb.verbProps.range > 10)
                        //    {
                        //        visibilityAtDest *= 0.425f;
                        //    }
                        //    else
                        //    {
                        //        visibilityAtDest *= 0.275f;
                        //    }
                        //}
                        //else
                        //{
                        //    visibilityAtDest *= 0.50f;
                        //}
                    }
                    // get wether this is a raider
                    raiders |= pawn.HostileTo(Faction.OfPlayerSilentFail);
                    //if (raiders)
                    //{
                    //    //pawnBlockingCost = 1200;
                    //}
                    // Run normal if we're not being suppressed, running for cover, crouch-walking or not actually moving to another cell
                    //CompSuppressable comp = pawn?.TryGetCompFast<CompSuppressable>();
                    //if (comp == null || !comp.isSuppressed || comp.IsCrouchWalking || pawn.CurJob?.def == CE_JobDefOf.RunForCover || start == dest.Cell && peMode == PathEndMode.OnCell)
                    //{
                        __state = true;
                    //    crouching = comp?.IsCrouchWalking ?? false;
                        counter = 0;
                        return true;
                    //}
                    //
                    // Make all destinations unreachable
                    //__state = false;
                    //__result = PawnPath.NotFound;
                    //return false;
                }
                __state = false;
                Reset();
                return true;
            }

            public static void Postfix(PathFinder __instance, PawnPath __result, bool __state)
            {
                if (dump)
                {
                    //if (gridWriter != null)
                    //{
                    //    gridWriter.Write();
                    //}
                    if (pathWriter != null)
                    {
                        pathWriter.Write();
                    }
                }
                if (__state)
                {
                    if (Finder.Settings.Pather_KillboxKiller && __result != null && !__result.nodes.NullOrEmpty() && (pawn?.RaceProps.Humanlike ?? false))
                    {
                        //ThingComp_CombatAI comp = pawn.GetComp_Fast<ThingComp_CombatAI>();
                        //if (comp != null && comp.TryStartMiningJobs(__result))
                        //{
                        //}
                        //if (pawn.GetComp_Fast())
                        IntVec3 cellBefore;
                        Thing thing = __result.FirstBlockingBuilding(out cellBefore, pawn);
                        if (thing != null && pawn.mindState?.duty?.def != DutyDefOf.Sapper && pawn.CurJob?.def != JobDefOf.Mine)
                        {
                            Job job = DigUtility.PassBlockerJob(pawn, thing, cellBefore, true, true);
                            if (job != null)
                            {
                                job.playerForced = true;
                                job.expiryInterval = 3600;
                                job.maxNumMeleeAttacks = 300;
                                //if (__result.nodes.Count > 0)
                                //{
                                //    Pawn_CustomDutyTracker.CustomPawnDuty sapper = new Pawn_CustomDutyTracker.CustomPawnDuty();
                                //    sapper.duty = new PawnDuty(DutyDefOf.Sapper, __result.nodes[0]);
                                //    pawn.GetComp_Fast<Comps.ThingComp_CombatAI>()?.duties?.StartDuty(sapper, true);
                                //}
                                pawn.jobs.StopAll();
                                pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                                if (Rand.Chance(0.8f))
                                {
                                    //GenClosest.ClosestThingReachable(pawn.Position, map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.InteractionCell, TraverseParms.For(pawn), maxDistance:)
                                    int count = 0;
                                    int countTarget = Rand.Int % 3 + 1;
                                    Faction faction = pawn.Faction;
                                    Predicate<Thing> validator = (t) =>
                                    {
                                        if (count < countTarget && t.Faction == faction && t is Pawn ally && !ally.Destroyed
                                        && ally.mindState?.duty?.def != DutyDefOf.Escort
                                        && (sightReader == null || sightReader.GetAbsVisibilityToEnemies(ally.Position) == 0)
                                        && ally.skills?.GetSkill(SkillDefOf.Mining).Level < 10
                                        && GenTicks.TicksGame - ally.LastAttackTargetTick > 60)
                                        {
                                            Comps.ThingComp_CombatAI comp = ally.GetComp_Fast<Comps.ThingComp_CombatAI>();
                                            if (comp?.duties != null && comp.duties?.Any(DutyDefOf.Escort) == false)
                                            {
                                                Pawn_CustomDutyTracker.CustomPawnDuty custom = CustomDutyUtility.Escort(ally, pawn, 50, 100, Rand.Int % 500 + 1500, 0, true, DutyDefOf.AssaultColony);
                                                if (custom != null)
                                                {
                                                    comp.duties.StartDuty(custom, true);
                                                }
                                            }
                                            count++;
                                            return count == countTarget; 
                                        }
                                        return false;
                                    };
                                    Verse.GenClosest.RegionwiseBFSWorker(pawn.Position, map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.InteractionCell, TraverseParms.For(pawn), validator, null, 1, 4, 25f, out int _);
                                    //List<Pawn> allies = pawn.Position.(map, Utilities.TrackedThingsRequestCategory.Pawns, 10f)
                                    //    .Where(t => t.Faction == pawn.Faction && pawn.CanReach(t, PathEndMode.InteractionCell, Danger.Unspecified))
                                    //    .Select(t => t as Pawn)
                                    //    .Where(p => !p.Destroyed && p.mindState?.duty?.def != DutyDefOf.Escort && (sightReader == null || sightReader.GetAbsVisibilityToEnemies(p.Position) == 0) && p.skills?.GetSkill(SkillDefOf.Mining).Level < 10 && GenTicks.TicksGame - p.LastAttackTargetTick > 60).ToList();
                                    //if (allies != null && allies.Count > 0)
                                    //{
                                    //    foreach (Pawn ally in allies.TakeRandom(Rand.Int % 3 + 1))
                                    //    {
                                    //if (ally != null)
                                    //{
                                    //    Comps.ThingComp_CombatAI comp = ally.GetComp_Fast<Comps.ThingComp_CombatAI>();
                                    //    if (comp?.duties != null && comp.duties?.Any(DutyDefOf.Escort) == false)
                                    //    {
                                    //        Pawn_CustomDutyTracker.CustomPawnDuty custom = CustomDutyUtility.Escort(ally, pawn, 50, 100, Rand.Int % 500 + 1500, 0, true, DutyDefOf.AssaultColony);
                                    //        if (custom != null)
                                    //        {
                                    //            comp.duties.StartDuty(custom, true);
                                    //        }
                                    //    }
                                    //}
                                    //    }
                                    //}
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
                //avoidanceTracker = null;
                avoidanceReader = null;
                //lightingTracker = null;
                raiders = false;
                sightReader = null;
                counter = 0;
                instance = null;
                visibilityAtDest = 0f;
                map = null;
                pawn = null;
            }

            /*
             * Search for the vairable that is initialized by the value from the avoid grid or search for
             * ((i > 3) ? num9 : num8) + num15;
             *          
             */
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished1 = false;
                //bool finished2 = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    //if (!finished2)
                    //{
                    //    if(codes[i].opcode == OpCodes.Ldc_I4 && codes[i].OperandIs(175))
                    //    {
                    //        finished2 = true;
                    //        yield return new CodeInstruction(OpCodes.Ldloc_S, 45);
                    //        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_FindPath_Patch), nameof(PathFinder_FindPath_Patch.GetPawnBlockingCost))).MoveBlocksFrom(codes[i]).MoveLabelsFrom(codes[i]);
                    //        continue;
                    //    }
                    //}
                    if (!finished1)
                    {
                        if (codes[i].opcode == OpCodes.Stloc_S && codes[i].operand is LocalBuilder builder1 && builder1.LocalIndex == 48)
                        {
                            finished1 = true;
                            yield return new CodeInstruction(OpCodes.Ldloc_S, 45).MoveLabelsFrom(codes[i]).MoveBlocksFrom(codes[i]); // index of cell around curIndex
                            yield return new CodeInstruction(OpCodes.Ldloc_S, 3); // curIndex
                            yield return new CodeInstruction(OpCodes.Ldloc_S, 15); // open cell num (after enqueue)
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PathFinder_FindPath_Patch), nameof(PathFinder_FindPath_Patch.GetCostOffsetAt)));
                            yield return new CodeInstruction(OpCodes.Add);
                        }
                    }
                    yield return codes[i];
                }
                //if (finished)
                //    Log.Message("CAI: Patched pather!");
            }

            //private static int GetPawnBlockingCost(int index)
            //{
            //    if (!raiders)
            //    {
            //        return 175;
            //    }
            //    IntVec3 cell = map.cellIndices.IndexToCell(index);
            //    foreach (Pawn pawn in cell.GetThingList(map).Where(t => t is Pawn))
            //    {
            //        if (pawn.stances?.curStance is Stance_Warmup)
            //        {
            //            return 1200;
            //        }
            //    }                
            //    return 175;
            //}

            private static int GetCostOffsetAt(int index, int parentIndex, int openNum)
            {
                if (map != null)
                {                    
                    var value = 0;
                    var visibility = 0f;
                    if (sightReader != null)
                    {                        
                        visibility = sightReader.GetVisibilityToEnemies(index);
                        if (visibility > visibilityAtDest)
                        {
                            value += (int)(visibility * 65);
                            //if (visibility > 5)
                            //{
                            //    value += (int)(visibility * 300);
                            //}
                            //else
                            //{
                            //    value += (int)(visibility * 90);
                            //}
                        }
                    }
                    if (avoidanceReader != null && !isPlayer)
                    {
                        if(value > 3)
                        {
                            value += (int)(avoidanceReader.GetPath(index) * Maths.Min(visibility, 5) * 21f);
                        }
                        else
                        {                           
                            value += (int)(Maths.Min(avoidanceReader.GetPath(index), 4) * 42);
                        }
                        float danger = avoidanceReader.GetDanger(index);
                        if(danger > 0)
                        {
                            value += 23;
                        }
                        value += Mathf.CeilToInt(danger);
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
                            pathWriter.Push(new DataWriter_Path.PathCell()
                            {
                                pref = value,
                                enRel = sightReader.GetVisibilityToEnemies(index),
                                enAbs = sightReader.GetAbsVisibilityToEnemies(index),
                                frRel = sightReader.GetVisibilityToFriendlies(index),
                                frAbs = sightReader.GetAbsVisibilityToFriendlies(index),
                                dang = avoidanceReader.GetDanger(index),
                                prox = avoidanceReader.GetProximity(index),
                                path = avoidanceReader.GetPath(index),
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
                        var l1 = 350 * (1f - Mathf.Lerp(0f, 0.75f, counter / (openNum + 1f))) * (1f - Maths.Min(openNum, 5000) / (7500));
                        var l2 = 250 * (1f - Mathf.Clamp01(PathFinder.calcGrid[parentIndex].knownCost / 2500));
                        //we use this so the game doesn't die
                        var v = (Maths.Min(value, l1 + l2) * factionMultiplier * 1);
                        //map.debugDrawer.FlashCell(map.cellIndices.IndexToCell(index), v, $" {l1 + l2}");                        
                        return (int)(Maths.Min(value, l1 + l2) * factionMultiplier * Finder.P50);
                        //return (int)(Maths.Min(value, 1000f) * factionMultiplier * 1);
                    }
                }
                return 0;
            }
        }
    }
}