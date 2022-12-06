using System;
using UnityEngine;
using Verse;

namespace CombatAI
{
	public static class MortarUtility
	{
		private static Vector2 zero = new Vector2(0, 0);

		public static void TryCastSight(Map map, IntVec3 root, ITSignalGrid grid, int minRange, int maxRange,
			bool canPenetrateThick, MetaCombatAttribute attributes)
		{
			var roofs = map.GetComponent<RoofGrid>();
			var minRSqr = minRange * minRange;
			var maxRSqr = maxRange * maxRange;
			attributes |= MetaCombatAttribute.Mortar;
			for (var i = -maxRange; i <= maxRange; i++)
			for (var j = -maxRange; j <= maxRange; j++)
			{
				var cell = root + new IntVec3(i, 0, j);
				if (!cell.InBounds(map)) continue;
				var dist = cell.DistanceToSquared(root);
				if (dist < minRSqr || dist > maxRSqr) continue;
				var roof = roofs.GetRoofType(cell);
				if (canPenetrateThick || (roof & RoofType.RockThick) != RoofType.None) grid.Set(cell, attributes);
			}
		}
	}
}