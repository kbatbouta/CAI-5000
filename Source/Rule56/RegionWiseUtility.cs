using System.Runtime.CompilerServices;
using Verse;
namespace CombatAI
{
	public static class RegionWiseUtility
	{
		private static Region _origin;
		private static Region _target;
		private static int    _dist;
		private static bool DistanceTo_RegionWise_Delegate(Region region, int cost, int depth)
		{
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
		public static int DistanceTo_RegionWise(this Thing first, Thing second)
		{
			return DistanceTo_RegionWise(first.Position, second.Position, first.Map);
		}

		public static int DistanceTo_RegionWise(this IntVec3 first, IntVec3 second, Map map)
		{
			MapComponent_CombatAI comp   = map.AI();
			Region                origin = first.GetRegion(map);
			if (origin == null)
			{
				return int.MaxValue;
			}
			Region target = second.GetRegion(map);
			if (target == null)
			{
				return int.MaxValue;
			}
			// always place the smaller id first.
			int a = origin.id;
			int b = target.id;
			if (b < a)
			{
				(a, b) = (b, a);
			}
			Pair<int, int> key = new Pair<int, int>(a, b);
			if (!comp.regionWiseDist.TryGetValue(key, out _dist))
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
	}
}
