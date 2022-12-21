using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI.Patches
{
	public static class RegionGrid_Patch
	{
		[HarmonyPatch(typeof(RegionGrid), nameof(RegionGrid.SetRegionAt))]
		private static class RegionGrid_SetRegionAt_Patch
		{
			public static void Prefix(RegionGrid __instance, IntVec3 c, Region reg)
			{
				__instance.map.GetComp_Fast<SightTracker>().Notify_RegionChanged(c, reg);
			}
		}	
	}
}
