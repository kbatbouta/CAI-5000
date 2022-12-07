using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatAI.Patches
{
	public static class FleckMaker_Patch
	{
		[HarmonyPatch(typeof(FleckMaker), nameof(FleckMaker.ThrowMetaIcon))]
		private static class FleckMaker_ThrowMetaIcon_Patch
		{
			public static bool Prefix(IntVec3 cell, Map map)
			{
				return !Finder.Settings.FogOfWar_Enabled || !(map.GetComp_Fast<MapComponent_FogGrid>()?.IsFogged(cell) ?? false);
			}
		}

		[HarmonyPatch(typeof(FleckMaker), nameof(FleckMaker.PlaceFootprint))]
		private static class FleckMaker_PlaceFootprint_Patch
		{
			public static bool Prefix(IntVec3 loc, Map map)
			{
				return !Finder.Settings.FogOfWar_Enabled || !(map.GetComp_Fast<MapComponent_FogGrid>()?.IsFogged(loc) ?? false);
			}
		}

		[HarmonyPatch(typeof(FleckMaker), nameof(FleckMaker.ThrowBreathPuff))]
		private static class FleckMaker_ThrowBreathPuff_Patch
		{
			public static bool Prefix(IntVec3 loc, Map map)
			{
				return !Finder.Settings.FogOfWar_Enabled || !(map.GetComp_Fast<MapComponent_FogGrid>()?.IsFogged(loc) ?? false);
			}
		}
	}
}
