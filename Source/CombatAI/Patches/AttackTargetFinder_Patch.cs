using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;

namespace CombatAI.Patches
{
    internal static class AttackTargetFinder_Patch
    {
        private static Map map;
        //private static List<CompProjectileInterceptor> interceptors;
        private static SightTracker.SightReader sightReader;
        private static TurretTracker turretTracker;
        //private static CompTacticalManager manager;
        //private static CombatReservationManager combatReservationManager;
        //private static bool tpsLow;

        [HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget")]
        internal static class AttackTargetFinder_BestAttackTarget_Patch
        {
            //private static bool EMPOnlyTargetsMechanoids()
            //{
            //    return false;
            //}

            //internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            //{
            //    var codes = instructions.ToList();
            //    for (int i = 0; i < instructions.Count(); i++)
            //    {
            //        if (codes[i].opcode == OpCodes.Call && ReferenceEquals(codes[i].operand, AccessTools.Method(typeof(VerbUtility), nameof(VerbUtility.IsEMP))))
            //        {
            //            codes[i - 2].opcode = OpCodes.Nop;
            //            codes[i - 1].opcode = OpCodes.Nop;
            //            codes[i].operand = typeof(AttackTargetFinder_BestAttackTarget_Patch).GetMethod(nameof(EMPOnlyTargetsMechanoids), AccessTools.all);
            //            break;
            //        }
            //    }
            //    return codes;
            //}

            internal static void Prefix(IAttackTargetSearcher searcher)
            {            
                // tpsLow = PerformanceTracker.TpsCriticallyLow;
                map = searcher.Thing?.Map;
                // combatReservationManager = map.GetComp_Fast<CombatReservationManager>();
                // interceptors = searcher.Thing?.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor)
                //                               .Select(t => t.TryGetComp<CompProjectileInterceptor>())
                //                               .ToList() ?? new List<CompProjectileInterceptor>();
                if (searcher.Thing is Pawn pawn)
                {
                    //manager = pawn.GetComp<CompTacticalManager>();
                    pawn.GetSightReader(out sightReader);

                    if (pawn.Faction.HostileTo(map.ParentFaction))
                    {
                        turretTracker = map.GetComp_Fast<TurretTracker>();
                    }
                }
            }

            internal static void Postfix()
            {
                map = null;
                sightReader = null;
                turretTracker = null;
                // interceptors.Clear();
                // interceptors = null;
                // combatReservationManager = null;
            }
        }

        [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.GetShootingTargetScore))]
        internal static class Harmony_AttackTargetFinder_GetShootingTargetScore
        {
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool finished = false;
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
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_AttackTargetFinder_GetShootingTargetScore), nameof(Harmony_AttackTargetFinder_GetShootingTargetScore.GetShootingTargetBaseScore)));
                            continue;
                        }
                    }
                    yield return codes[i];
                }
            }

            public static float GetShootingTargetBaseScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
            {
                float result = 60f;               
                float distSqr = target.Thing.Position.DistanceToSquared(searcher.Thing.Position);

                //if (combatReservationManager != null && combatReservationManager.Reserved(target.Thing, out List<Pawn> attackers))
                //{
                //    for (int i = 0; i < attackers.Count; i++)
                //    {
                //        Pawn attacker = attackers[i];
                //        if (attacker.stances?.curStance is Stance_Warmup)
                //            result -= 5f;
                //        if ((attacker.jobs?.curJob?.def.alwaysShowWeapon ?? false)
                //            && attacker.pather != null
                //            && attacker.pather.curPath?.NodesConsumedCount > attacker.pather.curPath?.NodesLeftCount)
                //            result -= 5f;
                //        if (attacker.Position.DistanceToSquared(target.Thing.Position) < 16 && distSqr > 255)
                //            result -= 2.5f;
                //    }
                //    result += 10 - attackers.Count * 3.5f;
                //}
                if (verb.IsMeleeAttack || verb.EffectiveRange <= 15)
                {
                    if (sightReader != null)
                    {
                        result += 10 - sightReader.GetVisibilityToEnemies(target.Thing.Position);
                    }
                    result += (16f * 16f - distSqr) / (16f * 16f) * 15;
                }                           
                //else
                //{
                //if (searcher.Thing.Map?.GetLightingTracker() is LightingTracker tracker)
                //    result *= tracker.CombatGlowAt(target.Thing.Position) * 0.5f;
                //if (map != null)
                //{
                //    Vector3 srcPos = searcher.Thing.Position.ToVector3();
                //    Vector3 trgPos = target.Thing.Position.ToVector3();

                //    for (int i = 0; i < interceptors.Count; i++)
                //    {
                //        CompProjectileInterceptor interceptor = interceptors[i];
                //        float radiusSqr = interceptor.Props.radius * interceptor.Props.radius;
                //        if (interceptor.Active)
                //        {
                //            if (interceptor.parent.Position.DistanceToSquared(target.Thing.Position) < radiusSqr)
                //            {
                //                if (interceptor.parent.Position.DistanceToSquared(searcher.Thing.Position) < radiusSqr)
                //                {
                //                    result += 60f;
                //                }
                //                else
                //                {
                //                    result -= 30;
                //                }
                //            }
                //            else if (interceptor.parent.Position.DistanceToSquared(searcher.Thing.Position) > radiusSqr
                //                  && interceptor.parent.Position.ToVector3().DistanceToSegmentSquared(srcPos, trgPos, out _) < radiusSqr)
                //            {
                //                result -= 30;
                //            }
                //        }
                //    }
                //}
                //}
                return result;
            }
        }
    }
}

