using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;

namespace CombatAI.Patches
{
    public static class Harmony_CastPositionFinder
    {
        private static Verb verb;
        private static Pawn pawn;
        private static Thing target;
        private static Map map;
        private static UInt64 targetFlags;
        private static IntVec3 targetPosition;
        private static float warmupTime;
        private static float range;
        //private static float tpsLevel;
        //private static bool tpsLow;
        private static AvoidanceTracker avoidanceTracker;
        private static AvoidanceTracker.AvoidanceReader avoidanceReader;
        private static SightTracker.SightReader sightReader;
        private static TurretTracker turretTracker;
        //private static LightingTracker lightingTracker;
        private static List<CompProjectileInterceptor> interceptors;
        private static Stopwatch stopwatch = new Stopwatch();

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.TryFindCastPosition))]
        public static class CastPositionFinder_TryFindCastPosition_Patch
        {
            public static void Prefix(CastPositionRequest newReq)
            {
                //tpsLevel = PerformanceTracker.TpsLevel;
                //tpsLow = PerformanceTracker.TpsCriticallyLow;
                stopwatch.Start();
                verb = newReq.verb;
                range = verb.EffectiveRange;
                pawn = newReq.caster;
                avoidanceTracker = pawn.Map.GetComp_Fast<AvoidanceTracker>();
                avoidanceTracker.TryGetReader(pawn, out avoidanceReader);
                warmupTime = verb?.verbProps.warmupTime ?? 1;
                warmupTime = Mathf.Clamp(warmupTime, 0.5f, 0.8f);
                map = newReq.caster?.Map;
                target = newReq.target;
                targetPosition = newReq.target.Position;
                targetFlags = newReq.target.GetCombatFlags();
                interceptors = map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor)
                                               .Select(t => t.TryGetComp<CompProjectileInterceptor>())
                                               .ToList();

                //lightingTracker = map.GetLightingTracker();

                if (map.ParentFaction != newReq.caster?.Faction)
                    turretTracker = map.GetComponent<TurretTracker>();
                if (newReq.caster != null && newReq.caster.Faction != null)
                    newReq.caster.GetSightReader(out sightReader);

                //Verb_LaunchProjectileCE.ShootLineScore = 0f;
            }

            public static void Postfix(IntVec3 dest, bool __result)
            {                
                if (__result && avoidanceTracker != null)
                    avoidanceTracker.Notify_CoverPositionSelected(pawn, dest);
                stopwatch.Stop();
                stopwatch.Reset();
                pawn = null;
                verb = null;
                map = null;
                sightReader = null;
                //lightingTracker = null;
                turretTracker = null;
                avoidanceTracker = null;
                avoidanceReader = null;
            }
        }

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.CastPositionPreference))]
        public static class CastPositionFinder_CastPositionPreference_Patch
        {
            public static void Postfix(IntVec3 c, ref float __result)
            {
                if (__result == -1)
                    return;

                float visibility = 0f;
                float sightCost = 0;
                if (sightReader != null)
                    sightCost = 4 - (visibility = sightReader.GetVisibility(c)) * 2f;
                if (sightCost > 0)
                {
                    __result += sightCost;
                    //if (lightingTracker.IsNight)
                    //    __result += 2 - lightingTracker.CombatGlowAt(c) * 2.0f;
                }
                //if (!tpsLow)
                //{
                    for (int i = 0; i < interceptors.Count; i++)
                    {
                        CompProjectileInterceptor interceptor = interceptors[i];
                        if (interceptor.Active && interceptor.parent.Position.DistanceToSquared(c) < interceptor.Props.radius * interceptor.Props.radius)
                        {
                            //if (interceptor.parent.Position.PawnsInRange(map, interceptor.Props.radius).All(p => p.HostileTo(pawn)))
                            //    __result -= 15.0f * tpsLevel;
                            //else
                                __result += 12f;
                        }
                    }
                if (avoidanceReader != null)
                    __result += 3 - Mathf.Min(avoidanceReader.GetProximity(c), 10) - avoidanceReader.GetDanger(c) / 2f;
                //}
                if (range > 0)
                    __result += (16 - Mathf.Abs(Mathf.Max(visibility * 0.7f * 0.7f, 0.4f) - c.DistanceToSquared(targetPosition) / (range * range)) * 16);
            }
        }
    }
}

