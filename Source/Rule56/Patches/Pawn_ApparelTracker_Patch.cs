using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
	public static class Pawn_ApparelTracker_Patch
	{
		[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelAdded))]
		private static class Pawn_ApparelTracker_Notify_ApparelAdded_Patch
		{
			public static void Postfix(Pawn_ApparelTracker __instance)
			{
				if (__instance.pawn != null)
				{
					CacheUtility.ClearThingCache(__instance.pawn);
				}
			}	
		}
		
		[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelRemoved))]
		private static class Pawn_ApparelTracker_Notify_ApparelRemoved_Patch
		{
			public static void Postfix(Pawn_ApparelTracker __instance)
			{
				if (__instance.pawn != null)
				{
					CacheUtility.ClearThingCache(__instance.pawn);
				}
			}
		}
		
		[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
		private static class Pawn_ApparelTracker_Notify_ApparelChanged_Patch
		{
			public static void Postfix(Pawn_ApparelTracker __instance)
			{
				if (__instance.pawn != null)
				{
					CacheUtility.ClearThingCache(__instance.pawn);
				}
			}
		}
	}
}
