using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
	public static class Game_Patch
	{
		[HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
		private static class Game_DeinitAndRemoveMap_Patch
		{
			public static void Prefix(Map map)
			{
				CompCache.Notify_MapRemoved(map);
			}
		}

		[HarmonyPatch(typeof(Game), nameof(Game.ClearCaches))]
		private static class Game_ClearCaches_Patch
		{
			public static void Prefix()
			{
				TCacheHelper.ClearCache();
				StatCache.ClearCache();
				CompCache.ClearCaches();
				SightUtility.ClearCache();
				JobGiver_AITrashBuildingsDistant_Patch.ClearCache();
				GenSight_Patch.ClearCache();
				ArmorUtility.ClearCache();
				DamageUtility.ClearCache();
				MetaCombatAttributeUtility.ClearCache();
				BattleRoyale.manager.Active = false;
			}
		}
	}
}
