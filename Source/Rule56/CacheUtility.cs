using CombatAI.Patches;
using Verse;
namespace CombatAI
{
	public class CacheUtility
	{
		public static void ClearAllCache(bool mapRemoved = false)
		{
			TCacheHelper.ClearCache();
			StatCache.ClearCache();
			CompCache.ClearCaches();
			SightUtility.ClearCache();
			JobGiver_AITrashBuildingsDistant_Patch.ClearCache();
			GenSight_Patch.ClearCache();
			MetaCombatAttributeUtility.ClearCache();
			LordToil_AssaultColony_Patch.ClearCache();
			AttackTargetFinder_Patch.ClearCache();
			TrashUtility_Patch.ClearCache();
			if (mapRemoved)
			{
				DamageUtility.ClearCache();
				ArmorUtility.ClearCache();
			}
		}
		
		public static void ClearShortCache()
		{
			TrashUtility_Patch.ClearCache();
			TCacheHelper.ClearCache();
			StatCache.ClearCache();
			CompCache.ClearCaches();
			SightUtility.ClearCache();
			JobGiver_AITrashBuildingsDistant_Patch.ClearCache();
			GenSight_Patch.ClearCache();
			MetaCombatAttributeUtility.ClearCache();
			LordToil_AssaultColony_Patch.ClearCache();
			AttackTargetFinder_Patch.ClearCache();
		}

		public static void ClearThingCache(Thing thing)
		{
			DamageUtility.Invalidate(thing);
			ArmorUtility.Invalidate(thing);
		}
	}
}
