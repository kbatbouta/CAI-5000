using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;
using System.Reflection;

namespace CombatAI.Patches
{
    public static class JobGiver_AIGotoNearestHostile_Patch
    {        
        [HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), nameof(JobGiver_AIGotoNearestHostile.TryGiveJob))]
        static class JobGiver_AIGotoNearestHostile_TryGiveJob_Patch
        {
            static MethodInfo mCanReach = AccessTools.Method(typeof(ReachabilityUtility), nameof(ReachabilityUtility.CanReach));

            //public static void Postfix(Pawn pawn, ref Job __result)
            //{               
            //    //if (__result != null && __result.targetA.Cell.IsValid)
            //    //{
            //    //    PathFinderCostTuning tuning = new PathFinderCostTuning();
            //    //    tuning.costBlockedDoor = 15;
            //    //    tuning.costBlockedDoorPerHitPoint = 0;
            //    //    tuning.costBlockedWallBase = 30;
            //    //    tuning.costBlockedWallExtraForNaturalWalls = 30;
            //    //    tuning.costBlockedWallExtraPerHitPoint = 0;
            //    //    tuning.costOffLordWalkGrid = 0;
            //    //    using (PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, __result.targetA, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, true, true), tuning: tuning))
            //    //    {                        
            //    //        IntVec3 cellBefore;
            //    //        Thing thing = path.FirstBlockingBuilding(out cellBefore, pawn);
            //    //        if (thing != null)
            //    //        {
            //    //            Job job = DigUtility.PassBlockerJob(pawn, thing, cellBefore, true, true);
            //    //            if (job != null)
            //    //            {                                
            //    //                Log.Message($"DUDE FOR {pawn}!");
            //    //                __result = job;
            //    //            }
            //    //        }
            //    //    }
            //    //}
            //}

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;
                for(int i= 0;i < codes.Count; i++)
                {
                    if (!finished)
                    {
                        if (codes[i + 1].opcode == OpCodes.Call && codes[i + 1].OperandIs(mCanReach))
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                            continue;
                        }
                    }
                    yield return codes[i];
                }                
            }

            //public static void Postfix(Pawn pawn, ref Job __result)
            //{
            //    pawn.GetSightReader(out SightTracker.SightReader reader);
            //    Verb verb = pawn.CurrentEffectiveVerb;
            //    if (pawn.Faction != null && ((verb?.IsMeleeAttack ?? false) || (verb?.EffectiveRange < 15)) && reader != null && __result.targetA.Cell.DistanceToSquared(pawn.Position) > 225)
            //    {
            //        IntVec3 position = pawn.Position;
            //        if (reader.GetVisibilityToEnemies(__result.targetA.Cell) > 3 || reader.GetAbsVisibilityToEnemies(__result.targetA.Cell) > 10 || __result.targetA.Cell.DistanceToSquared(position) > 2500)
            //        {
            //            float nearestDist = 1e5f;
            //            Pawn nearestAlly = null;
            //            foreach (Pawn ally in position.ThingsInRange(pawn.Map, Utilities.TrackedThingsRequestCategory.Pawns, 25).Where(p => (p.Faction == pawn.Faction || !(p.Faction?.HostileTo(pawn.Faction) ?? true))))
            //            {
            //                float distSqr = ally.Position.DistanceToSquared(position);
            //                if (distSqr < nearestDist && (ally.equipment?.PrimaryEq?.PrimaryVerb?.EffectiveRange > 15))
            //                {
            //                    nearestDist = distSqr;
            //                    nearestAlly = ally;
            //                }
            //            }
            //            if (nearestAlly != null)
            //            {
            //                pawn.mindState.duty = new PawnDuty(DutyDefOf.Escort, nearestAlly, 25);
            //                pawn.mindState.duty.locomotion = LocomotionUrgency.Sprint;
            //                __result = null;
            //            }
            //        }
            //    }
            //}
        }
   }
}

