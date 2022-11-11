using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
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
            private static float visibilityAtDest;
            private static float factionMultiplier = 1.0f;

            internal static bool Prefix(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, ref TraverseParms traverseParms, PathEndMode peMode, ref PathFinderCostTuning tuning, out bool __state)
            {
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
                    factionMultiplier = pawn.Faction.IsPlayer ? (pawn.Drafted ? 0.25f : 0.75f) : 1.0f;                    
                    // retrive CE elements                    
                    pawn.GetSightReader(out sightReader);
                    pawn.Map.GetComp_Fast<AvoidanceTracker>().TryGetReader(pawn, out avoidanceReader);

                    if ((Finder.Settings.Debug && pawn.IsColonistPlayerControlled)|| pawn.mindState?.duty?.def == DutyDefOf.AssaultColony || pawn.mindState?.duty?.def == DutyDefOf.AssaultThing || pawn.mindState?.duty?.def == DutyDefOf.HuntEnemiesIndividual)
                    {
                        raiders = true;
                        //factionMultiplier = 1;
                        //TraverseParms parms = traverseParms;
                        //parms.canBashDoors = true;
                        //parms.canBashFences = true;
                        //parms.mode = TraverseMode.PassAllDestroyableThings;
                        //parms.maxDanger = Danger.Unspecified;
                        //traverseParms = parms;
                        //if (tuning == null)
                        //{
                        //    tuning = new PathFinderCostTuning();
                        //    tuning.costBlockedDoor = 15;
                        //    tuning.costBlockedDoorPerHitPoint = 0;
                        //    tuning.costBlockedWallBase = 42;
                        //    tuning.costBlockedWallExtraForNaturalWalls = 42;
                        //    tuning.costBlockedWallExtraPerHitPoint = 0;
                        //    tuning.costOffLordWalkGrid = 0;
                        //}
                    }
                   
                    //pawn.Map.GetComp_Fast<SightTracker>().TryGetReader(pawn, out sightReader);

                    // get the visibility at the destination
                    if (sightReader != null)
                    {
                        visibilityAtDest = Mathf.Min(sightReader.GetVisibilityToEnemies(dest.Cell) * 0.85f, 5);
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
                if (__state)
                {
                    IntVec3 cellBefore;
                    Thing thing = __result.FirstBlockingBuilding(out cellBefore, pawn);
                    if (thing != null)
                    {
                        Job job = DigUtility.PassBlockerJob(pawn, thing, cellBefore, true, true);
                        if (job != null)
                        {
                            pawn.jobs.StopAll();
                            pawn.jobs.StartJob(job, JobCondition.InterruptOptional);
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
                            value += (int)(visibility * 45);
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
                    if (value > 0)
                    {
                        if (avoidanceReader != null)
                        {
                            var avoidance = (avoidanceReader.GetProximity(index) * 15f + avoidanceReader.GetDanger(index) * 10);
                            if (visibility <= 0f)
                            {
                                value += (int)avoidance;
                            }
                            else
                            {
                                value += (int)Mathf.Min(avoidance, visibility * 12.75f);
                            }                            
                        }
                        //if (lightingTracker != null)
                        //    value += (int)(lightingTracker.CombatGlowAt(map.cellIndices.IndexToCell(index)) * 25f);
                    }
                    else
                    {
                        if (avoidanceReader != null)
                        {
                            value += (int)(avoidanceReader.GetProximity(index) / 16 * 90);
                        }
                    }
                    //Log.Message($"{value} {sightReader != null} {sightReader.hostile != null} {sightReader.GetVisibility(index)} {sightReader.hostile.GetSignalStrengthAt(index)}");
                    //map.debugDrawer.FlashCell(map.cellIndices.IndexToCell(index), sightReader.hostile.GetSignalStrengthAt(index), $"{value}_ ");
                    if (value > 10f)
                    {
                        counter++;
                        //
                        // TODO make this into a maxcost -= something system
                        var l1 = 450 * (1f - Mathf.Lerp(0f, 0.75f, counter / (openNum + 1f))) * (1f - Mathf.Min(openNum, 5000) / (7500));
                        var l2 = 250 * (1f - Mathf.Clamp01(PathFinder.calcGrid[parentIndex].knownCost / 2500));
                        //we use this so the game doesn't die
                        var v = (Mathf.Min(value, l1 + l2) * factionMultiplier * 1);
                        //map.debugDrawer.FlashCell(map.cellIndices.IndexToCell(index), v, $" _{v}");
                        return (int)(Mathf.Min(value, l1 + l2) * factionMultiplier * 1);                        
                    }
                }
                return 0;
            }
        }
    }
}