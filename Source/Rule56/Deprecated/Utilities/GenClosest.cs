using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI;
using CombatAI.Utilities;

namespace CombatAI
{
#if DEBUG_REACTION
	public static class GenClosest
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ThingsTracker GetThingsTracker(this Map map)
		{
			return map.GetComp_Fast<ThingsTracker>();
		}

		public static IEnumerable<Thing> ThingsInRange(this IntVec3 cell, Map map,
			TrackedThingsRequestCategory category, float range)
		{
			var model = GetThingsTracker(map).GetModelFor(category);
			if (model != null) return model.ThingsInRangeOf(cell, range);
			return null;
		}

		public static IEnumerable<Thing> ThingsNearSegment(this IntVec3 origin, IntVec3 destination, Map map,
			TrackedThingsRequestCategory category, float range, bool behind = false)
		{
			var model = GetThingsTracker(map).GetModelFor(category);
			if (model != null) return model.ThingsNearSegment(origin, destination, range, behind);
			return null;
		}
	}
#endif
}