using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using static CombatAI.SightTracker;

namespace CombatAI.Patches
{
    public static class Harmony_CastPositionFinder
    {
        private static bool                             ai_fightEnemies;
        private static Verb                             verb;
        private static Pawn                             pawn;
        private static Thing                            target;
        private static Map                              map;
        private static bool                             skipped;
        private static IntVec3                          targetPosition;
        private static IntVec3                          dutyDest;
        private static float                            warmupTime;
        private static float                            range;
        private static CastPositionRequest              request;
        private static ISGrid<float>                    grid;
        private static InterceptorTracker               interceptors;
        private static AvoidanceTracker                 avoidanceTracker;
        private static AvoidanceTracker.AvoidanceReader avoidanceReader;
        private static SightReader                      sightReader;

        [HarmonyPatch(typeof(JobGiver_AIFightEnemies), nameof(JobGiver_AIFightEnemies.TryFindShootingPosition))]
        public static class JobGiver_AIFightEnemies_TryFindShootingPosition_Patch
        {
            public static void Prefix()
            {
                ai_fightEnemies = true;
            }
        }

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.TryFindCastPosition))]
        public static class CastPositionFinder_TryFindCastPosition_Patch
        {
            private static readonly FieldInfo fBestSpotPref = AccessTools.Field(typeof(CastPositionFinder), nameof(CastPositionFinder.bestSpotPref));

            public static void Prefix(ref CastPositionRequest newReq)
            {
                if (ai_fightEnemies)
                {
                    ai_fightEnemies           = false;
                    newReq.maxRangeFromTarget = 0;
                }
                skipped = true;
                if (newReq.caster != null && Finder.Settings.Caster_Enabled && newReq.target != null && (newReq.maxRangeFromTarget == 0 || newReq.maxRangeFromTarget * newReq.maxRangeFromTarget > newReq.caster.Position.DistanceToSquared(newReq.target.Position)) && newReq.maxRangeFromLocus == 0)
                {
                    if ((pawn = newReq.caster) != null && !(pawn.RaceProps?.Animal ?? true) && (pawn.mindState?.duty == null || pawn.mindState.duty.def != DutyDefOf.Sapper && pawn.mindState.duty.def != DutyDefOf.Breaching))
                    {
                        if (pawn.Faction.IsPlayerSafe() && pawn.AI()?.forcedTarget.IsValid == false)
                        {
                            goto skip;
                        }
                        map              = newReq.caster?.Map;
                        verb             = newReq.verb;
                        range            = verb.EffectiveRange;
                        map              = pawn.Map;
                        avoidanceTracker = pawn.Map.GetComp_Fast<AvoidanceTracker>();
                        avoidanceTracker.TryGetReader(pawn, out avoidanceReader);
                        dutyDest = pawn.TryGetNextDutyDest(request.maxRangeFromCaster);
                        if (avoidanceReader != null)
                        {
                            grid = map.GetFloatGrid();
                            grid.Reset();
                            newReq.caster.TryGetSightReader(out sightReader);
                            if (sightReader != null)
                            {
                                sightReader.armor          = pawn.GetArmorReport();
                                request                    = newReq;
                                interceptors               = map.GetComp_Fast<MapComponent_CombatAI>().interceptors;
                                warmupTime                 = verb?.verbProps.warmupTime ?? 1;
                                warmupTime                 = Mathf.Clamp(warmupTime, 0.5f, 0.8f);
                                target                     = newReq.target;
                                targetPosition             = target.Position;
                                newReq.wantCoverFromTarget = true;
                                skipped                    = false;
                                //
                                // map.debugDrawer.FlashCell(pawn.Position, 1, "dude", 100);
                                return;
                            }
                        }
                    }
                }
            skip:
                pawn             = null;
                grid             = null;
                avoidanceTracker = null;
                avoidanceReader  = null;
                sightReader      = null;
                verb             = null;
                target           = null;
                map              = null;
                skipped          = true;
            }

            public static void Postfix(IntVec3 dest, bool __result)
            {
                if (__result && avoidanceTracker != null)
                {
                    avoidanceTracker.Notify_CoverPositionSelected(pawn, dest);
                }
                if (grid != null)
                {
                    grid.Reset();
                }
                skipped          = true;
                grid             = null;
                interceptors     = null;
                avoidanceTracker = null;
                avoidanceReader  = null;
                sightReader      = null;
                pawn             = null;
                verb             = null;
                target           = null;
                map              = null;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes    = instructions.ToList();
                bool                  finished = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    yield return codes[i];
                    if (!finished)
                    {
                        if (codes[i].OperandIs(fBestSpotPref))
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Ldloc_0, 0);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CastPositionFinder_TryFindCastPosition_Patch), nameof(FloodCellRect)));
                        }
                    }
                }
            }

            private static void FloodCellRect(CellRect rect)
            {
                if (!skipped && sightReader != null)
                {
                    IntVec3 root             = pawn.Position;
                    float   rootVis          = sightReader.GetVisibilityToEnemies(root);
                    float   rootThreat       = sightReader.GetThreat(request.locus);
                    float   effectiveRange   = verb?.EffectiveRange > 0 ? verb.EffectiveRange : -1;
                    float   rootDutyDestDist = dutyDest.IsValid ? dutyDest.DistanceTo(pawn.Position) : -1;
                    map.GetCellFlooder().Flood(root,
                                               node =>
                                               {
                                                   float val = (node.dist - node.distAbs) / (node.distAbs + 1f) * 2 + (sightReader.GetVisibilityToEnemies(node.cell) - rootVis) * 0.5f + Maths.Min(avoidanceReader.GetProximity(node.cell), 2f) + Maths.Min(avoidanceReader.GetDanger(node.cell), 1f) - interceptors.grid.Get(node.cell) * 4 + (sightReader.GetThreat(node.cell) - rootThreat) * 0.5f;
                                                   if (rootDutyDestDist > 0)
                                                   {
                                                       val += Mathf.Clamp((Maths.Sqrt_Fast(dutyDest.DistanceToSquared(node.cell), 3) - rootDutyDestDist) * 0.25f, -0.5f, 0.5f);
                                                   }
                                                   if (effectiveRange > 0)
                                                   {
                                                       val += 2f * Mathf.Abs(effectiveRange - Maths.Sqrt_Fast(node.cell.DistanceToSquared(targetPosition), 5)) / effectiveRange;
                                                   }
                                                   grid[node.cell] = val;
                                               },
                                               cell =>
                                               {
	                                               return (sightReader.GetVisibilityToEnemies(cell) - rootVis) * 2.5f - interceptors.grid.Get(cell) + (sightReader.GetThreat(cell) - rootThreat) * 0.25f;
                                               },
                                               cell =>
                                               {
                                                   return rect.Contains(cell) && cell.WalkableBy(map, pawn) && map.reservationManager.CanReserve(pawn, cell);
                                               }
                                               , Maths.Max(rect.Height, rect.Width) * 2);
                }
            }
        }

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.CastPositionPreference))]
        public static class CastPositionFinder_CastPositionPreference_Patch
        {
            public static void Postfix(IntVec3 c, ref float __result)
            {
                if (!skipped)
                {
                    if (__result == -1)
                    {
                        return;
                    }
                    if (sightReader != null)
                    {
                        if (!grid.IsSet(c))
                        {
                            __result = -1;
                        }
                        else
                        {
                            __result = __result - grid[c] * Finder.P50 + 1.5f;
                        }
                    }
                }
            }
        }
    }
}
