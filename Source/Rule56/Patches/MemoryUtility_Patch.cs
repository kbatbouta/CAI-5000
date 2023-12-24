using HarmonyLib;
using Verse.Profile;
namespace CombatAI.Patches
{
    public static class MemoryUtility_Patch
    {
        [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
        private static class MemoryUtility_ClearAllMapsAndWorld_Patch
        {
            public static void Postfix()
            {
                AsyncActions.KillAll();
                CacheUtility.ClearAllCache(true);
            }
        }
    }
}
