using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
	public static class Thing_Patch
	{
		[HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
		public static class Thing_DeSpawn_Patch
		{
			public static void Prefix(Thing __instance)
			{
				CacheUtility.ClearThingCache(__instance);
			}
		}	
	}
}
