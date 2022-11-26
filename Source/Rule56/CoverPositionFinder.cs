using System;
using static CombatAI.AvoidanceTracker;
using Verse;
using UnityEngine;
using Verse.AI;
using static CombatAI.SightTracker;
using static CombatAI.CellFlooder;

namespace CombatAI
{
    public static class CoverPositionFinder
    {      
        public static bool TryFindCoverPosition(CoverPositionRequest request, out IntVec3 coverCell)
        {
            request.caster.GetSightReader(out SightReader sightReader);            
            request.caster.GetAvoidanceTracker(out AvoidanceReader avoidanceReader);
            if (sightReader == null || avoidanceReader == null)
            {
                coverCell = IntVec3.Invalid;
                return false;
            }
            Map map = request.caster.Map;
            Pawn caster = request.caster;
            if (request.locus == IntVec3.Zero)
            {
                request.locus = request.caster.Position;
                request.maxRangeFromLocus = request.maxRangeFromCaster;
            }
            if (request.maxRangeFromLocus == 0)
            {
                request.maxRangeFromLocus = float.MaxValue;
            }
            InterceptorTracker interceptors = map.GetComp_Fast<MapComponent_CombatAI>().interceptors;
            float maxDistSqr = request.maxRangeFromLocus * request.maxRangeFromLocus;
            CellFlooder flooder = map.GetCellFlooder();
            IntVec3 enemyLoc = request.target.Cell;
            IntVec3 bestCell = IntVec3.Invalid;
            float rootVis = sightReader.GetVisibilityToEnemies(request.locus);
			float bestCellVisibility = 1e8f;
            float bestCellScore = 1e8f;
            bool tpsLow = Finder.Performance.TpsCriticallyLow;
            flooder.Flood(request.locus,
                (node) =>
                {
                    if ((!tpsLow && request.verb != null && !request.verb.CanHitTargetFrom(node.cell, enemyLoc)) || maxDistSqr < request.locus.DistanceToSquared(node.cell) || !map.reservationManager.CanReserve(caster, node.cell))
                    {
                        return;
                    }
                    float c = (node.dist - node.distAbs) / (node.distAbs + 1f) - CoverUtility.TotalSurroundingCoverScore(node.cell, map) - interceptors.grid.Get(node.cell) * 2;
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
                    //map.debugDrawer.FlashCell(node.cell, c / 5f, text: $"{Math.Round(c, 2)}");
                },
                (cell) =>
                {
                    return (sightReader.GetVisibilityToEnemies(cell) - rootVis) * 2 - (request.checkBlockChance ? CoverUtility.CalculateOverallBlockChance(cell, enemyLoc, map) : 0) - interceptors.grid.Get(cell);
                },
                (cell) =>
                {
                    return (request.validator == null || request.validator(cell)) && cell.WalkableBy(map, caster);
                },
                (int) Maths.Min(request.maxRangeFromLocus, 30)
            );
            coverCell = bestCell;
            return bestCell.IsValid;
        }

        public static bool TryFindRetreatPosition(CoverPositionRequest request, out IntVec3 coverCell)
        {
            request.caster.GetSightReader(out SightReader sightReader);
            request.caster.GetAvoidanceTracker(out AvoidanceReader avoidanceReader);
            if (sightReader == null || avoidanceReader == null)
            {
                coverCell = IntVec3.Invalid;
                return false;
            }
            Map map = request.caster.Map;
            Pawn caster = request.caster;
            if (request.locus == IntVec3.Zero)
            {
                request.locus = request.caster.Position;
                request.maxRangeFromLocus = request.maxRangeFromCaster;
            }
            if (request.maxRangeFromLocus == 0)
            {
                request.maxRangeFromLocus = float.MaxValue;
            }
			InterceptorTracker interceptors = map.GetComp_Fast<MapComponent_CombatAI>().interceptors;
			float maxDistSqr = request.maxRangeFromLocus * request.maxRangeFromLocus;
            CellFlooder flooder = map.GetCellFlooder();
            IntVec3 enemyLoc = request.target.Cell;
            IntVec3 bestCell = IntVec3.Invalid;
			float rootVis = sightReader.GetVisibilityToEnemies(request.locus);
			float rootVisFriendlies = sightReader.GetVisibilityToFriendlies(request.locus);
			float bestCellDist = request.locus.DistanceToSquared(enemyLoc);
            float bestCellScore = 1e8f;
            flooder.Flood(request.locus,
                (node) =>
                {
                    if (maxDistSqr < request.locus.DistanceToSquared(node.cell) || !map.reservationManager.CanReserve(caster, node.cell))
                    {
                        return;
                    }
                    float c = (node.dist - node.distAbs) / (node.distAbs + 1f) + avoidanceReader.GetProximity(node.cell) * 0.5f - CoverUtility.TotalSurroundingCoverScore(node.cell, map) * 0.5f - interceptors.grid.Get(node.cell);
                    if (c < bestCellScore)
                    {
                        float d = node.cell.DistanceToSquared(enemyLoc);
                        if (d > bestCellDist)
                        {
                            bestCellScore = c;
                            bestCellDist = d;
                            bestCell = node.cell;
                        }
                    }
                    //map.debugDrawer.FlashCell(node.cell, c / 5f, text: $"{Math.Round(c, 2)}");
                },
                (cell) =>
                {
                    return (sightReader.GetVisibilityToEnemies(cell) - rootVis) * 2 - (rootVisFriendlies - sightReader.GetVisibilityToFriendlies(cell)) * - (request.checkBlockChance ? CoverUtility.CalculateOverallBlockChance(cell, enemyLoc, map) : 0) - interceptors.grid.Get(cell);
                },
                (cell) =>
                {
                    return (request.validator == null || request.validator(cell)) && cell.WalkableBy(map, caster);
                },
                (int)Maths.Min(request.maxRangeFromLocus, 30)
            );
            coverCell = bestCell;
            return bestCell.IsValid;
        }
    }
}

