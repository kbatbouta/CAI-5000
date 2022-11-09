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
            public static void Postfix(Pawn pawn, ref Job __result)
            {
                pawn.GetSightReader(out SightTracker.SightReader reader);
                Verb verb = pawn.CurrentEffectiveVerb;
                if (pawn.Faction != null && ((verb?.IsMeleeAttack ?? false) || (verb?.EffectiveRange < 15)) && reader != null && __result.targetA.Cell.DistanceToSquared(pawn.Position) > 225)
                {
                    IntVec3 position = pawn.Position;
                    if (reader.GetVisibilityToEnemies(__result.targetA.Cell) > 3 || reader.GetAbsVisibilityToEnemies(__result.targetA.Cell) > 10 || __result.targetA.Cell.DistanceToSquared(position) > 2500)
                    {
                        float nearestDist = 1e5f;
                        Pawn nearestAlly = null;
                        foreach(Pawn ally in position.ThingsInRange(pawn.Map, Utilities.TrackedThingsRequestCategory.Pawns, 25).Where(p => (p.Faction == pawn.Faction || !(p.Faction?.HostileTo(pawn.Faction) ?? true))))
                        {                            
                            float distSqr = ally.Position.DistanceToSquared(position);
                            if (distSqr < nearestDist && (ally.equipment?.PrimaryEq?.PrimaryVerb?.EffectiveRange > 15))
                            {
                                nearestDist = distSqr;
                                nearestAlly = ally;
                            }
                        }
                        if (nearestAlly != null)
                        {
                            pawn.mindState.duty = new PawnDuty(DutyDefOf.Escort, nearestAlly, 25);
                            pawn.mindState.duty.locomotion = LocomotionUrgency.Sprint;                                                                         
                           __result = null;
                           // Log.Message($"{pawn} got a change in duty");
                        }
                    }
                }                                
            }
        }
    }
}

