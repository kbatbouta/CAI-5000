using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI
{
	[LoadIf("brrainz.zombieland")]
	public class Mod_ZombieLand
	{
		public static bool active;
		
		[LoadNamed("ZombieLand.Zombie")]
		public static Type Zombie;
		[LoadNamed("ZombieLand.Zombie:Render", LoadableType.Method)]
		public static MethodInfo Zombie_Render;

		[RunIf(loaded: true)]
		private static void OnActive()
		{
			Finder.Harmony.Patch(Zombie_Render, prefix: new HarmonyMethod(AccessTools.Method(typeof(Zombie_Patch), nameof(Zombie_Patch.Render))));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsZombie(Pawn pawn)
		{
			return active && (Zombie?.IsInstanceOfType(pawn) ?? false);
		}
		
		private class Zombie_Patch
		{
			public static bool Render(Pawn __instance)
			{
				if (Finder.Settings.FogOfWar_Enabled && __instance.Spawned)
				{
					MapComponent_FogGrid grid = __instance.Map.GetComp_Fast<MapComponent_FogGrid>();
					if (grid != null)
					{
						return !grid.IsFogged(__instance.Position);
					}
				}
				return true;
			}
		}
	}
}
