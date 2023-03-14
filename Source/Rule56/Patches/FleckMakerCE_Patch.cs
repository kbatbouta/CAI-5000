using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
	public static class FleckMakerCE_Patch
	{
		public static IntVec3 Current = IntVec3.Invalid;
		[HarmonyPatch]
		private static class FleckMakerCE_Static_Patch
		{
			private static MethodBase mStatic = AccessTools.Method("FleckMakerCE:Static", new []{typeof(IntVec3), typeof(Map), typeof(FleckDef), typeof(float)});
			
			public static bool Prepare()
			{
				return mStatic != null;
			}

			public static MethodBase TargetMethod()
			{
				return mStatic;
			}

			public static void Prefix(IntVec3 cell, Map map, FleckDef fleckDef, float scale)
			{
				if (Finder.Settings.FogOfWar_Enabled && fleckDef == FleckDefOf.ShotFlash)
				{
					MapComponent_FogGrid grid = map.GetComp_Fast<MapComponent_FogGrid>();
					if (grid != null)
					{
						Current = cell;
						grid.RevealSpot(cell, Maths.Max(scale, 3f), Rand.Range(120, 240));
					}
				}
			}

			public static void Postfix()
			{
				Current = IntVec3.Invalid;
			}
		}
	}
}
