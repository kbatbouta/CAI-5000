using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;
namespace CombatAI
{
    public static class HeuristicDistanceUtility
    {
        private static Map  map;
        private static bool saveRegions;

        private static          float           _distCell;
        private static          IntVec3         _targetCell;
        private static readonly HashSet<Region> _regions = new HashSet<Region>(64);

        private static Region _origin;
        private static Region _target;
        private static int    _dist;

        private static void DistanceTo_CellWise_Delegate(IntVec3 cell, IntVec3 parent, float dist)
        {
            if (cell == _targetCell)
            {
                _distCell = dist;
            }
        }

        private static bool Validator_CellWise_Delegate(IntVec3 cell)
        {
            Region region = cell.GetRegion(map);
            return region != null && _regions.Contains(region);
        }

        private static bool DistanceTo_RegionWise_Delegate(Region region, int cost, int depth)
        {
            if (saveRegions)
            {
                _regions.Add(region);
            }
            // update cost for everything along the way.
            int a = _origin.id;
            int b = region.id;
            if (b < a)
            {
                (a, b) = (b, a);
            }
            // if the target is found then no need to continue.
            if (region == _target)
            {
                _dist = depth;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HeuristicDistanceTo_RegionWise(this Thing first, Thing second)
        {
            return HeuristicDistanceTo_RegionWise(first.Position, second.Position, first.Map);
        }
        public static int HeuristicDistanceTo_RegionWise(this IntVec3 first, IntVec3 second, Map map, bool useCache = true)
        {
            MapComponent_CombatAI comp   = map.AI();
            Region                origin = first.GetRegion(map);
            if (origin == null)
            {
                return short.MaxValue;
            }
            Region target = second.GetRegion(map);
            if (target == null)
            {
                return short.MaxValue;
            }
            // always place the smaller id first.
            int a = origin.id;
            int b = target.id;
            if (b < a)
            {
                (a, b) = (b, a);
            }
            Pair<int, int> key = new Pair<int, int>(a, b);
            if (!useCache || !comp.regionWiseDist.TryGetValue(key, out _dist))
            {
                _dist   = int.MaxValue;
                _origin = origin;
                _target = target;
                RegionFlooder.Flood(first, second, map, DistanceTo_RegionWise_Delegate, depthCostMul: 2);
                comp.regionWiseDist[new Pair<int, int>(a, b)] = _dist;
                _origin                                       = null;
                _target                                       = null;
            }
            return _dist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HeuristicDistanceTo(this Thing first, Thing second)
        {
            return HeuristicDistanceTo(first.Position, second.Position, first.Map);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HeuristicDistanceTo(this IntVec3 first, IntVec3 second, Map map)
        {
            return HeuristicDistanceTo(first, second, map, 9999);
        }
        public static float HeuristicDistanceTo(this IntVec3 first, IntVec3 second, Map map, int skipIfRegionWiseLarger)
        {
            MapComponent_CombatAI comp = map.AI();
            if (!first.IsValid)
            {
                return short.MaxValue;
            }
            if (!second.IsValid)
            {
                return short.MaxValue;
            }
            _regions.Clear();
            saveRegions                  = true;
            HeuristicDistanceUtility.map = map;
            Exception er = null;
            try
            {
                int hd = HeuristicDistanceTo_RegionWise(first, second, map, false);
                if (hd < skipIfRegionWiseLarger)
                {
                    _distCell   = int.MaxValue;
                    _targetCell = second;
                    comp.flooder_heursitic.Flood(first, second, DistanceTo_CellWise_Delegate, null, Validator_CellWise_Delegate, 9999, 9999);
                }
            }
            catch (Exception exp)
            {
                er = exp;
            }
            finally
            {
                _regions.Clear();
                HeuristicDistanceUtility.map = null;
                saveRegions                  = false;
            }
            if (er != null)
            {
                throw er;
            }
            return _distCell;
        }
    }
}
