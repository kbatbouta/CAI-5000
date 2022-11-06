using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;
using static CombatAI.SightTracker;

namespace CombatAI.Patches
{
    public static class Harmony_CastPositionFinder
    {
        //private static Verb verb;
        //private static Pawn pawn;
        //private static Thing target;
        //private static Map map;
        //private static UInt64 targetFlags;
        //private static IntVec3 targetPosition;
        //private static float warmupTime;
        //private static float range;
        //private static float tpsLevel;
        //private static bool tpsLow;        
        private static AvoidanceTracker.AvoidanceReader avoidanceReader;
        private static SightTracker.SightReader sightReader;
        //private static TurretTracker turretTracker;
        //private static LightingTracker lightingTracker;
        //private static List<CompProjectileInterceptor> interceptors;
        //private static Stopwatch stopwatch = new Stopwatch();

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.TryFindCastPosition))]
        public static class CastPositionFinder_TryFindCastPosition_Patch
        {
            private static bool ShouldSkip(Pawn pawn, Thing target) =>
                pawn == null || target == null || pawn.IsColonist || !pawn.HasWeaponVisible() || pawn.Position.DistanceToSquared(target.Position) >= 64 * 64;

            public static bool Prefix(CastPositionRequest newReq, ref bool __result, ref IntVec3 dest)
            {
                Pawn pawn = newReq.caster;
                Thing target = newReq.target;
                if (ShouldSkip(pawn, target))
                {
                    return true;
                }
                Map map = pawn.Map;
                pawn.GetSightReader(out sightReader);
                pawn.GetAvoidanceTracker(out avoidanceReader);
                Verb verb = newReq.verb;
                CellFlooder flooder = pawn.Map.GetCellFlooder();
                IntVec3 startLoc = pawn.Position;
                IntVec3 destLoc = target.Position;                
                IntVec3 bestCell = IntVec3.Invalid;
                float bestCellVisibility = 1e8f;
                float bestCellScore = 1e8f;
                flooder.Flood(startLoc,
                    (node) =>
                    {
                        if (!verb.CanHitTargetFrom(node.cell, targ: target))
                        {
                            return;
                        }
                        float c = (node.dist - node.distAbs) / (node.distAbs + 1f);
                        if (c < bestCellScore)
                        {
                            float v = sightReader.GetVisibilityToEnemies(node.cell);
                            if (v < bestCellVisibility)
                            {
                                bestCellScore = c;
                                bestCellVisibility = v;
                                bestCell = node.cell;
                            }
                        }
                        //map.debugDrawer.FlashCell(node.cell, c / 40f, text: $"{Math.Round(c, 2)}");
                    },
                    (cell) =>
                    {
                        return sightReader.GetVisibilityToEnemies(cell);
                    },
                    (cell) =>
                    {
                        return (newReq.validator == null || newReq.validator(cell))                        
                        && cell.WalkableBy(map, pawn)
                        && map.reservationManager.CanReserve(pawn, cell);
                    }
                );
                dest = bestCell;
                return !dest.IsValid;
            }
        }

        //[HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.TryFindCastPosition))]
        //public static class CastPositionFinder_TryFindCastPosition_Patch
        //{
        //    public static void Prefix(CastPositionRequest newReq)
        //    {
        //        if (newReq.caster != null)
        //        {
        //            //tpsLevel = PerformanceTracker.TpsLevel;
        //            //tpsLow = PerformanceTracker.TpsCriticallyLow;
        //            //stopwatch.Start();
        //            verb = newReq.verb;
        //            range = verb.EffectiveRange;
        //            pawn = newReq.caster;
        //            avoidanceTracker = pawn.Map.GetComp_Fast<AvoidanceTracker>();
        //            avoidanceTracker.TryGetReader(pawn, out avoidanceReader);
        //            warmupTime = verb?.verbProps.warmupTime ?? 1;
        //            warmupTime = Mathf.Clamp(warmupTime, 0.5f, 0.8f);
        //            map = newReq.caster?.Map;
        //            target = newReq.target;
        //            targetPosition = newReq.target.Position;
        //            targetFlags = newReq.target.GetCombatFlags();
        //            //interceptors = map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor)
        //            //                               .Select(t => t.TryGetComp<CompProjectileInterceptor>())
        //            //                               .ToList();
        //            newReq.caster.GetSightReader(out sightReader);
        //            //lightingTracker = map.GetLightingTracker();
        //            //if (map.ParentFaction != newReq.caster?.Faction)
        //            //    turretTracker = map.GetComponent<TurretTracker>();                    


        //            //Verb_LaunchProjectileCE.ShootLineScore = 0f;
        //        }
        //    }

        //    public static void Postfix(IntVec3 dest, bool __result)
        //    {
        //        if (__result && avoidanceTracker != null)
        //        {
        //            avoidanceTracker.Notify_CoverPositionSelected(pawn, dest);                    
        //        }
        //        avoidanceTracker = null;
        //        //stopwatch.Stop();
        //        //stopwatch.Reset();
        //        pawn = null;
        //        verb = null;
        //        target = null;
        //        map = null;
        //        sightReader = null;
        //        // if (interceptors != null)
        //        // {
        //        //  interceptors.Clear();
        //        //  interceptors = null;
        //        // }
        //        // lightingTracker = null;
        //        // turretTracker = null;         
        //    }
        //}

        //[HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.CastPositionPreference))]
        //public static class CastPositionFinder_CastPositionPreference_Patch
        //{
        //    public static void Postfix(IntVec3 c, ref float __result)
        //    {
        //        if (__result == -1)
        //        {
        //            return;
        //        }
        //        //float visibility = 0f;
        //        //float sightCost = 0;
        //        //if (sightReader != null)
        //        //{
        //        //    sightCost = 4 - (visibility = sightReader.GetVisibility(c)) * 2f;
        //        //}
        //        //if (sightCost > 0)
        //        //{
        //        //__result += sightCost;
        //        //if (lightingTracker.IsNight)
        //        //    __result += 2 - lightingTracker.CombatGlowAt(c) * 2.0f;
        //        //}
        //        //if (!tpsLow)
        //        //{
        //        //for (int i = 0; i < interceptors.Count; i++)
        //        //{
        //        //    CompProjectileInterceptor interceptor = interceptors[i];
        //        //    if (interceptor.Active && interceptor.parent.Position.DistanceToSquared(c) < interceptor.Props.radius * interceptor.Props.radius)
        //        //    {
        //        //        //if (interceptor.parent.Position.PawnsInRange(map, interceptor.Props.radius).All(p => p.HostileTo(pawn)))
        //        //        //    __result -= 15.0f * tpsLevel;
        //        //        //else
        //        //        __result += 12f;
        //        //    }
        //        //}
        //        //}
        //        if (sightReader != null)
        //        {
        //            __result += 5 - sightReader.GetVisibilityToEnemies(c) * 15 - sightReader.GetAbsVisibilityToEnemies(c) / 4f;                    
        //        }
        //        if (avoidanceReader != null)
        //        {
        //            __result += 5 - Mathf.Min(avoidanceReader.GetProximity(c) * 1.5f, 5) - avoidanceReader.GetDanger(c) / 2f;
        //        }
        //        if (range > 0)
        //        {
        //            __result += 10 - Mathf.Abs((range * 0.8f - c.DistanceTo(targetPosition)) / range) * 10;
        //        }
        //        if (__result < 0)
        //        {
        //            __result = -1;
        //        }
        //    }
        //}
    }
}

