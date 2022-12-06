using System;
using Verse;

namespace CombatAI.Comps
{
	public class CompProperties_Sighter : CompProperties
	{
		public int radius;

		public int? radiusNight;

		public bool powered;

		public bool mannable;

		public CompProperties_Sighter()
		{
			compClass = typeof(ThingComp_Sighter);
		}
	}
}