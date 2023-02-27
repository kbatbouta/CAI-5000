using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
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
                    Thing         nearestEnemy = null;
                    TraverseParms parms        = new TraverseParms();
                    parms.pawn          = pawn;
                    parms.canBashDoors  = pawn.Faction == null || pawn.HostileTo(pawn.Map.ParentFaction);
                    parms.canBashFences = !pawn.RaceProps.Animal && parms.canBashDoors;
                    parms.mode          = TraverseMode.PassDoors;
                    parms.maxDanger     = Danger.Deadly;
                    RegionFlooder.Flood(pawn.Position, pawn.mindState.enemyTarget?.Position ?? IntVec3.Invalid, pawn.Map,
                                        (region, score, depth) =>
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
                                                        if (thing is IAttackTarget target && !target.ThreatDisabled(pawn) && AttackTargetFinder.IsAutoTargetable(target) && thing.HostileTo(pawn) && ((other = thing as Pawn) == null || other.IsCombatant()) && pawn.CanReach(thing, PathEndMode.InteractionCell, Danger.Deadly, true, true, TraverseMode.PassDoors))
                                                        {
                                                            nearestEnemy = thing;
                                                            return true;
                                                        }
                                                    }
                                                }
                                            }
                                            return false;
                                        },
                                        validator: region => region.Allows(parms, false),
                                        cost: region => Maths.Min(reader.GetRegionAbsVisibilityToEnemies(region), 8 * Finder.P50) * 10, maxRegions: 512);
                    if (nearestEnemy != null && pawn.CanReach(nearestEnemy, PathEndMode.InteractionCell, Danger.Deadly, true, true, TraverseMode.PassDoors))
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.Goto, nearestEnemy);
                        job.canBashDoors          = pawn.Faction == null || pawn.HostileTo(pawn.Map.ParentFaction);
                        job.canBashFences         = !pawn.RaceProps.Animal && job.canBashDoors;
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
