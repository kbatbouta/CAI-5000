using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;
using System.Reflection;

namespace CombatAI.Patches
{
	public static class Building_Door_Patch
	{
		[HarmonyPatch(typeof(Building_Door), nameof(Building_Door.ClearReachabilityCache))]
		private static class Building_Door_ClearReachabilityCache_Patch
		{
			public static void Postfix(Building_Door __instance)
			{
				if (!__instance.Destroyed && __instance.Spawned)
					__instance.Map?.GetComp_Fast<WallGrid>()?.RecalculateCell(__instance.Position, __instance);
			}
		}

		[HarmonyPatch(typeof(Building_Door), nameof(Building_Door.Tick))]
		private static class Building_Door_Tick_Patch
		{
			public static void Postfix(Building_Door __instance)
			{
				if (__instance.IsHashIntervalTick(30) && !__instance.Destroyed && __instance.Spawned)
					__instance.Map?.GetComp_Fast<WallGrid>()?.RecalculateCell(__instance.Position, __instance);
			}
		}
	}
}