using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public static class PawnPathUtility
    {
        public static IntVec3 GetMovingShiftedPosition(Pawn pawn, float ticksAhead)
        {
            if (TryGetCellIndexAhead(pawn, ticksAhead, out int index))
            {
                return pawn.pather.curPath.Peek(index);
            }
            return pawn.Position;
        }

        public static bool TryGetCellIndexAhead(Pawn pawn, float ticksAhead, out int index)
        {
            PawnPath path;
            if (!(pawn.pather?.moving ?? false) || (path = pawn.pather.curPath) == null || path.NodesLeftCount <= 1)
            {
                index = -1;
                return false;
            }
            index = Mathf.FloorToInt(Maths.Min(pawn.GetStatValue_Fast(StatDefOf.MoveSpeed, 900) * Mod_MoveSpeed.Mult * ticksAhead / 60f, path.NodesLeftCount - 1));
            return true;
        }

        public static bool TryGetSapperSubPath(this PawnPath path, Pawn pawn, List<IntVec3> store, int sightAhead, int sightStep, out IntVec3 cellBefore, out IntVec3 cellAhead, out bool enemiesAhead, out bool enemiesBefore, bool debugFlash = false)
        {
            cellBefore    = IntVec3.Invalid;
            cellAhead     = IntVec3.Invalid;
            enemiesAhead  = false;
            enemiesBefore = false;
            if (path == null || !path.Found || pawn == null || !pawn.TryGetSightReader(out SightTracker.SightReader reader))
            {
                return false;
            }
            Map       map  = pawn.Map;
            Area_Home home = map.areaManager.Get<Area_Home>();
            int       i    = path.curNodeIndex - 1;
            int       num  = 0;
            IntVec3   loc  = path.nodes[path.curNodeIndex];
            if (debugFlash)
            {
	            map.debugDrawer.debugCells.Clear();
            }
            while (i >= 0)
            {
                IntVec3 next = path.nodes[i];
                if (store.Count == 0 && reader.GetVisibilityToEnemies(next) > 0)
                {
                    enemiesBefore = true;
                }
                IntVec3 dLoc    = next - loc;
                bool    blocked = false;
                if (dLoc.x != 0 && dLoc.z != 0)
                {
                    IntVec3 s1 = loc + new IntVec3(dLoc.x, 0, 0);
                    IntVec3 s2 = loc + new IntVec3(0, 0, dLoc.z);
                    if (!s1.WalkableBy(pawn) && !s2.WalkableBy(pawn))
                    {
                        if (num == 0)
                        {
                            store.Clear();
                            cellBefore = loc;
                        }
                        num++;
                        blocked = true;
                        store.Add(Rand.Chance(0.5f) ? s1 : s2);
                    }
                    if (!blocked && num > 0)
                    {
                        cellAhead = s1.WalkableBy(pawn) ? s1 : s2;
                        break;
                    }
                    if (!next.WalkableBy(pawn))
                    {
                        if (num == 0)
                        {
                            store.Clear();
                            if (!s1.WalkableBy(pawn))
                            {
                                cellBefore = s2;
                            }
                            else if (!s2.WalkableBy(pawn))
                            {
                                cellBefore = s1;
                            }
                            else
                            {
                                cellBefore = Rand.Chance(0.5f) ? s1 : s2;
                            }
                        }
                        num++;
                        blocked = true;
                        store.Add(next);
                    }
                }
                else if (!next.WalkableBy(pawn))
                {
                    if (num == 0)
                    {
                        store.Clear();
                        cellBefore = loc;
                    }
                    num++;
                    blocked = true;
                    store.Add(next);
                }
                if (debugFlash)
                {
	                map.debugDrawer.FlashCell(next, blocked ? 0.99f : 0.01f, blocked ? "B" : "");
                }
                if (!blocked && num > 0)
                {
                    cellAhead = next;
                    break;
                }
                loc = next;
                i--;
            }
            if (store.Count > 0 && i < 0)
            {
	            return false;
            }
            if (debugFlash)
            {
	            int k = i;
	            while (k >= 0)
	            {
		            IntVec3 next = path.nodes[k];
		            map.debugDrawer.FlashCell(next, 0.5f, "+");
		            k--;
	            }
            }
            if (store.Count > 0)
            {
                int i0    = i;
                int limit = Maths.Max(i - sightAhead, 0);
                while (i >= limit)
                {
                    IntVec3 next = path.nodes[i];
                    if (reader.GetVisibilityToEnemies(next) > 0 || home != null && home.innerGrid[next])
                    {
                        enemiesAhead = true;
                        break;
                    }
                    i -= sightStep;
                }
                if (!enemiesAhead && home != null)
                {
                    i = i0;
                    while (i >= limit)
                    {
                        IntVec3 next = path.nodes[i];
                        if (home.innerGrid[next])
                        {
                            enemiesAhead = true;
                            break;
                        }
                        i -= sightStep;
                    }

                }
                return cellAhead.IsValid;
            }
            return store.Count > 0 && cellAhead.IsValid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool WalkableBy(this IntVec3 cell, Pawn pawn)
        {
//            if (!cell.WalkableBy(pawn.Map, pawn))
//            {
//                return false;
//            }
            Building building = cell.GetEdifice(pawn.Map);
            if (building != null && (building.def.Fillage == FillCategory.Full || building.def.passability == Traversability.Impassable))
            {
	            return false;
            }
            TerrainDef terrainDef = cell.GetTerrain(pawn.Map);
            if (terrainDef != null && terrainDef.passability == Traversability.Impassable)
            {
	            return false;
            }
            return true;
        }
    }
}
