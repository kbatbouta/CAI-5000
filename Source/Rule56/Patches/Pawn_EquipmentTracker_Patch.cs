using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
	public class Pawn_EquipmentTracker_Patch
	{
		[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded))]
		private static class Pawn_EquipmentTracker_Notify_EquipmentAdded_Patch
		{
			public static void Postfix(Pawn_EquipmentTracker __instance)
			{
				if (__instance.pawn != null)
				{
					CacheUtility.ClearThingCache(__instance.pawn);
				}
			}	
		}
		
		[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentRemoved))]
		private static class Pawn_EquipmentTracker_Notify_EquipmentRemoved_Patch
		{
			public static void Postfix(Pawn_EquipmentTracker __instance)
			{
				if (__instance.pawn != null)
				{
					CacheUtility.ClearThingCache(__instance.pawn);
				}
			}
		}
	}
}
