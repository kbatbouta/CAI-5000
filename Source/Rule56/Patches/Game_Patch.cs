using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
    public static class Game_Patch
    {
#pragma warning disable CS0612
        [HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
#pragma warning restore CS0612
        private static class Game_DeinitAndRemoveMap_Patch
        {
            public static void Prefix(Map map)
            {
                CompCache.Notify_MapRemoved(map);
                CacheUtility.ClearAllCache(mapRemoved: true);
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.ClearCaches))]
        private static class Game_ClearCaches_Patch
        {
            public static void Prefix()
            {
	            CacheUtility.ClearAllCache();
            }
        }

#if DEBUG
		[HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
#pragma warning restore CS0612
		private static class Game_FinalizeInit_Patch
		{
			private static bool debugWindowShowen;
			
			public static void Prefix()
			{
				if (!debugWindowShowen)
				{
					Window_JobLogs.ShowTutorial();
					debugWindowShowen = true;
				}
			}
		}
#endif
    }
}
