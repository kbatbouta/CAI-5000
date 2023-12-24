using HarmonyLib;
using UnityEngine;
using Verse;
namespace CombatAI.Patches
{
	[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
	public static class PawnRenderer_Patch
	{
		private static bool Prefix(PawnRenderer __instance, Vector3 drawLoc)
		{
			if (Mod_ZombieLand.active && Finder.Settings.FogOfWar_Enabled && __instance.pawn != null && __instance.pawn.Spawned)
			{
				var grid = __instance.pawn.Map.GetComp_Fast<MapComponent_FogGrid>();
				if (grid != null)
				{
					return !grid.IsFogged(drawLoc.ToIntVec3());
				}
			}
			return true;
		}	
	}
}
