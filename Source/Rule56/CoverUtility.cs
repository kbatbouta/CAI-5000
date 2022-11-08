using System;
using static CombatAI.AvoidanceTracker;
using Verse;
using UnityEngine;

namespace CombatAI
{
    public static class CoverUtility
    {
        private static SightTracker.SightReader sightReader;
        private static AvoidanceTracker.AvoidanceReader avoidanceReader;


        public static bool TryGetBestCoverCell(this Pawn pawn, TargetInfo enemy, float radius, out IntVec3 coverCell, Func<IntVec3, bool> validator = null)
        {
            pawn.GetSightReader(out sightReader);
            pawn.GetAvoidanceTracker(out avoidanceReader);
            coverCell = IntVec3.Invalid;
            if (sightReader != null && avoidanceReader != null)
            {
                coverCell = GetNearbyCover(pawn, enemy.Cell, radius, true, validator);                
            }
            sightReader = null;
            avoidanceReader = null;
            return coverCell.IsValid;
        }

        public static bool TryGetBestCoverCell(this Pawn pawn, float radius, out IntVec3 coverCell, Func<IntVec3, bool> validator = null)
        {
            pawn.GetSightReader(out sightReader);
            pawn.GetAvoidanceTracker(out avoidanceReader);
            coverCell = IntVec3.Invalid;
            if (sightReader != null && avoidanceReader != null)
            {
                Vector2 dir = sightReader.GetEnemyDirection(pawn.Position);
                IntVec3 enemyCell = pawn.Position + new IntVec3((int)dir.x, 0, (int)dir.y);
                coverCell = GetNearbyCover(pawn, enemyCell, radius, false, validator);
            }           
            sightReader = null;
            avoidanceReader = null;
            return coverCell.IsValid;
        }  

        private static IntVec3 GetNearbyCover(Pawn pawn, IntVec3 enemyLoc, float radius, bool checkBlockChance, Func<IntVec3, bool> validator = null)
        {
            Map map = pawn.Map;            
            CellFlooder flooder = pawn.Map.GetCellFlooder();
            IntVec3 pawnLoc = pawn.Position;            
            IntVec3 bestCell = IntVec3.Invalid;            
            float bestCellVisibility = 1e8f;
            float bestCellScore = 1e8f;           
            flooder.Flood(pawnLoc,
                (node) =>
                {
                    float c = (node.dist - node.distAbs) / (node.distAbs + 1f) - (checkBlockChance ? Verse.CoverUtility.CalculateOverallBlockChance(node.cell, enemyLoc, map) : 0);
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
                    map.debugDrawer.FlashCell(node.cell, c / 5f, text: $"{Math.Round(c, 2)}");
                },
                (cell) =>
                {
                    return sightReader.GetVisibilityToEnemies(cell) - (checkBlockChance ? Verse.CoverUtility.CalculateOverallBlockChance(cell, enemyLoc, map) : 0);
                },
                (cell) =>
                {
                    return (validator == null || validator(cell))
                    && cell.WalkableBy(map, pawn)
                    && map.reservationManager.CanReserve(pawn, cell);
                }
            );
            return bestCell;
        }
    }
}

