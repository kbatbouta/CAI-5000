using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using static CombatAI.AvoidanceTracker;
using static CombatAI.SightTracker;

namespace CombatAI
{
    public static class CombatAI_Utility
    {
		private static readonly Dictionary<int, Pair<int, float>> speedCache = new Dictionary<int, Pair<int, float>>(256);
        private static readonly Dictionary<int, Pair<int, float>> aggroCache = new Dictionary<int, Pair<int, float>>(256);
      
		public static bool Is<T>(this T def, T other) where T : Def
		{
			return def != null && other != null && def == other;
		}

		public static float GetAggroMul(this Pawn pawn)
        {
			if (speedCache.TryGetValue(pawn.thingIDNumber, out var store) && GenTicks.TicksGame - store.First <= 600)
			{
				return store.second;
			}
			float aggro = pawn.GetStatValue(CombatAI_StatDefOf.CombatAI_AggroMul);
			speedCache[pawn.thingIDNumber] = new Pair<int, float>(GenTicks.TicksGame, aggro);
			return aggro;
		}

		public static float GetMoveSpeed(this Pawn pawn)
		{
			if (speedCache.TryGetValue(pawn.thingIDNumber, out var store) && GenTicks.TicksGame - store.First <= 600)
			{
				return store.second;
			}
			float speed = pawn.GetStatValue(StatDefOf.MoveSpeed);
			speedCache[pawn.thingIDNumber] = new Pair<int, float>(GenTicks.TicksGame, speed);
			return speed;
		}

		public static IntVec3 GetMovingShiftedPosition(this Pawn pawn, float ticksAhead)
		{
			if (TryGetCellIndexAhead(pawn, ticksAhead, out int index))
			{
				return pawn.pather.curPath.Peek(index);
			}
			return pawn.Position;
		}

		public static bool TryGetCellIndexAhead(this Pawn pawn, float ticksAhead, out int index)
		{
			PawnPath path;
			if (!(pawn.pather?.moving ?? false) || (path = pawn.pather.curPath) == null || path.NodesLeftCount <= 1)
			{
				index = -1;
				return false;
			}
			index = Mathf.FloorToInt(Maths.Min(pawn.GetMoveSpeed() * ticksAhead / 60f, path.NodesLeftCount - 1));
			return true;
		}

		public static bool TryGetSapperSubPath(this PawnPath path, Pawn pawn, List<IntVec3> store, int sightAhead, int sightStep, out IntVec3 cellBefore, out bool enemiesAhead, out bool enemiesBefore)
        {
			cellBefore = IntVec3.Invalid;
			enemiesAhead = false;
            enemiesBefore = false;
			if (path == null || !path.Found || pawn == null || !pawn.GetSightReader(out SightReader reader))
            {                
				return false;
            }
            Map map = pawn.Map;
			Area_Home home = map.areaManager.Get<Area_Home>();
			int i = path.curNodeIndex - 1;
            int num = 0;          
            IntVec3 loc = path.nodes[path.curNodeIndex];
            while(i >= 0)
            {
                IntVec3 next = path.nodes[i];
                if (store.Count == 0 && reader.GetAbsVisibilityToEnemies(next) > 0)
                {
                    enemiesBefore = true;
				}
                IntVec3 dLoc = next - loc;
                bool blocked = false;                
                if (dLoc.x != 0 && dLoc.z != 0)
                {
                    IntVec3 s1 = loc + new IntVec3(dLoc.x, 0, 0);
                    IntVec3 s2 = loc + new IntVec3(0, 0, dLoc.z);                    
                    if (!s1.IsCellWalkable(pawn) && !s2.IsCellWalkable(pawn))
                    {
                        //map.debugDrawer.FlashCell(s1, 0.9f, "b", 200);
                        //map.debugDrawer.FlashCell(s2, 0.9f, "b", 200);
                        if (num == 0)
                        {                                                                                    
                            store.Clear();
                            cellBefore = loc;
                            //map.debugDrawer.FlashCell(cellBefore, 0.1f, "_", 200);
                        }
                        num++;
                        blocked = true;
                        store.Add(Rand.Chance(0.5f) ? s1 : s2);                        
                    }
                    if (!blocked && num > 0)
                    {
                        break;
                    }
                    if (!next.IsCellWalkable(pawn))
                    {
                        //map.debugDrawer.FlashCell(next, 0.9f, "b", 200);
                        if (num == 0)
                        {                            
                            store.Clear();
                            if (!s1.IsCellWalkable(pawn))
                            {
                                cellBefore = s2;
                            }
                            else if(!s2.IsCellWalkable(pawn))
                            {
                                cellBefore = s1;
                            }
                            else
                            {
                                cellBefore = Rand.Chance(0.5f) ? s1 : s2; 
                            }
                            //map.debugDrawer.FlashCell(cellBefore, 0.1f, "_", 200);
                        }   
                        num++;
                        blocked = true;
                        store.Add(next);                        
                    }
                }
                else if(!next.IsCellWalkable(pawn))
                {
                    //map.debugDrawer.FlashCell(next, 0.9f, "b", 200);
                    if (num == 0)
                    {                        
                        store.Clear();
                        cellBefore = loc;
                        //map.debugDrawer.FlashCell(cellBefore, 0.1f, "_", 200);
                    }
                    num++;
                    blocked = true;
                    store.Add(next);                    
                }
                if (!blocked && num > 0)
                {
                     break;
                }                           
                loc = next;
                i--;
            }
            if (store.Count > 0)
            {
                int i0 = i;
                int limit = Maths.Max(i - sightAhead, 0);
				while (i >= limit)
                {
					IntVec3 next = path.nodes[i];
                    if (reader.GetAbsVisibilityToEnemies(next) > 0 || (home != null && home.innerGrid[next]))
                    {
                        enemiesAhead = true;
						//map.debugDrawer.FlashCell(next, 0.2f, "X", 200);
						break;
                    }
                    //else
                    //{
                    //	map.debugDrawer.FlashCell(next, 0.4f, "_", 200);
                    //}
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
							map.debugDrawer.FlashCell(next, 0.9f, "X", 200);
							enemiesAhead = true;
							break;
                        }
                        //else
                        //{
                        //	map.debugDrawer.FlashCell(next, 0.4f, "_", 200);
                        //}
                        i -= sightStep;
					}

				}
                return true;
            }
            return store.Count > 0;
        }

        public static bool IsCellWalkable(this IntVec3 cell, Pawn pawn)
        {            
            if (!cell.WalkableBy(pawn.Map, pawn))
            {
                return false;
            }         
            return true;
        }

        public static bool GetAvoidanceTracker(this Pawn pawn, out AvoidanceTracker.AvoidanceReader reader)
        {
            return pawn.Map.GetComp_Fast<AvoidanceTracker>().TryGetReader(pawn, out reader);
        }        

        public static ISGrid<float> GetFloatGrid(this Map map)
        {
            ISGrid<float> grid = map.GetComp_Fast<MapComponent_CombatAI>().f_grid;
            grid.Reset();
            return grid;
        }

        public static Verb TryGetAttackVerb(this Thing thing)
        {
            if (thing is Pawn pawn)
            {
                if (pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb.Available())
                {
                    return pawn.equipment.PrimaryEq.PrimaryVerb;
                }
                return pawn.meleeVerbs?.curMeleeVerb ?? null;
            }
            return null;
        }

        public static bool HasWeaponVisible(this Pawn pawn)
        {
            return (pawn.CurJob?.def.alwaysShowWeapon ?? false) || (pawn.mindState?.duty?.def.alwaysShowWeapon ?? false);
        }

        public static bool GetSightReader(this Pawn pawn, out SightTracker.SightReader reader)
        {
            SightTracker tracker = pawn.Map.GetComp_Fast<SightTracker>();
            return tracker.TryGetReader(pawn, out reader);
        }

        public static UInt64 GetThingFlags(this Thing thing)
        {
            return ((UInt64)1) << (GetThingFlagsIndex(thing));
        }

        public static int GetThingFlagsIndex(this Thing thing)
        {
            return thing.thingIDNumber % 64;
        }

        public static float DistanceToSegmentSquared(this Vector3 point, Vector3 lineStart, Vector3 lineEnd, out Vector3 closest)
        {
            float dx = lineEnd.x - lineStart.x;
            float dz = lineEnd.z - lineStart.z;
            if ((dx == 0) && (dz == 0))
            {
                closest = lineStart;
                dx = point.x - lineStart.x;
                dz = point.z - lineStart.z;
                return dx * dx + dz * dz;
            }
            float t = ((point.x - lineStart.x) * dx + (point.z - lineStart.z) * dz) / (dx * dx + dz * dz);
            if (t < 0)
            {
                closest = new Vector3(lineStart.x, 0, lineStart.z);
                dx = point.x - lineStart.x;
                dz = point.z - lineStart.z;
            }
            else if (t > 1)
            {
                closest = new Vector3(lineEnd.x, 0, lineEnd.z);
                dx = point.x - lineEnd.x;
                dz = point.z - lineEnd.z;
            }
            else
            {
                closest = new Vector3(lineStart.x + t * dx, 0, lineStart.z + t * dz);
                dx = point.x - closest.x;
                dz = point.z - closest.z;
            }
            return dx * dx + dz * dz;
        }        

        public static CellFlooder GetCellFlooder(this Map map)
        {
            return map.GetComp_Fast<MapComponent_CombatAI>().flooder;
        }

		public static void ClearCache()
		{
			speedCache.Clear();
		}
	}
}

