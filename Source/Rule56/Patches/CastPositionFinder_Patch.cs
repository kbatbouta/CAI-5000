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
using static CombatAI.CellFlooder;
using System.Reflection.Emit;
using System.Reflection;

namespace CombatAI.Patches
{
    public static class Harmony_CastPositionFinder
    {
        private static Verb verb;
        private static Pawn pawn;
        private static Thing target;
        private static Map map;        
        private static IntVec3 targetPosition;
        private static float warmupTime;
        private static float range;        
        private static TGrid<float> tGrid;
        private static AvoidanceTracker avoidanceTracker;
        private static AvoidanceTracker.AvoidanceReader avoidanceReader;
        private static SightTracker.SightReader sightReader;       

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.TryFindCastPosition))]
        public static class CastPositionFinder_TryFindCastPosition_Patch
        {
            private static FieldInfo fBestSpotPref = AccessTools.Field(typeof(CastPositionFinder), nameof(CastPositionFinder.bestSpotPref));

            public static void Prefix(CastPositionRequest newReq)
            {
                if (newReq.caster != null)
                {
                    newReq.wantCoverFromTarget = true;
                    map = newReq.caster?.Map;
                    verb = newReq.verb;
                    range = verb.EffectiveRange;
                    pawn = newReq.caster;
                    tGrid = map.GetTGrid();
                    avoidanceTracker = pawn.Map.GetComp_Fast<AvoidanceTracker>();
                    avoidanceTracker.TryGetReader(pawn, out avoidanceReader);
                    newReq.caster.GetSightReader(out sightReader);
                    warmupTime = verb?.verbProps.warmupTime ?? 1;
                    warmupTime = Mathf.Clamp(warmupTime, 0.5f, 0.8f);                    
                    target = newReq.target;
                    targetPosition = target.Position;
                }
            }

            public static void Postfix(IntVec3 dest, bool __result)
            {
                if (__result && avoidanceTracker != null)
                {
                    avoidanceTracker.Notify_CoverPositionSelected(pawn, dest);
                }
                if (tGrid != null)
                {
                    tGrid.Reset();
                }
                tGrid = null;
                avoidanceTracker = null;
                avoidanceReader = null;
                sightReader = null;                
                pawn = null;
                verb = null;
                target = null;
                map = null;                                       
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;                
                for (int i = 0;i < codes.Count; i++)
                {
                    yield return codes[i];
                    if (!finished)
                    {
                        if (codes[i].OperandIs(fBestSpotPref))
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Ldloc_0, 0);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CastPositionFinder_TryFindCastPosition_Patch), nameof(CastPositionFinder_TryFindCastPosition_Patch.FloodCellRect)));
                        }
                    }
                }
            }

            private static void FloodCellRect(CellRect rect)
            {
                if (sightReader != null)
                {
                    //bool pawnSelected = Find.Selector.SelectedPawns?.Contains(pawn) ?? false;
                    IntVec3 root = pawn.Position;
                    map.GetCellFlooder().Flood(root,
                        (node) =>
                        {
                            tGrid[node.cell] = (node.dist - node.distAbs) / (node.distAbs + 1f);                            
                        },
                        (cell) =>
                        {
                            Vector2 dir = sightReader.GetEnemyDirection(cell);
                            IntVec3 adjustedLoc;
                            if (dir.sqrMagnitude < 4)
                            {
                                adjustedLoc = targetPosition;
                            }
                            else
                            {
                                adjustedLoc = cell + new IntVec3((int)dir.x, 0, (int)dir.y);                                
                            }                            
                            return sightReader.GetVisibilityToEnemies(cell) - Verse.CoverUtility.CalculateOverallBlockChance(cell, adjustedLoc, map);
                        },
                        (cell) =>
                        {
                            return rect.Contains(cell) && cell.WalkableBy(map, pawn) && map.reservationManager.CanReserve(pawn, cell);
                        }
                    ,maxDist: Mathf.Max(rect.Height, rect.Width));
                }
            }
        }

        [HarmonyPatch(typeof(CastPositionFinder), nameof(CastPositionFinder.CastPositionPreference))]
        public static class CastPositionFinder_CastPositionPreference_Patch
        {
            public static void Postfix(IntVec3 c, ref float __result)
            {
                if (__result == -1)
                {
                    return;
                }
                if (sightReader != null && !tGrid.IsSet(c))
                {
                    __result = -1;
                }
                else
                {
                    __result -= tGrid[c];
                }
                //bool pawnSelected = Find.Selector.SelectedPawns?.Contains(pawn) ?? false;
                //if (pawnSelected)
                //{
                //    map.debugDrawer.FlashCell(c, tGrid[c] / 10f, text: $"{Math.Round(tGrid[c], 2)} {Math.Round(__result, 2)}");
                //}
            }
        }
    }
}

