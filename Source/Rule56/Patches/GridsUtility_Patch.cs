using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
	[HarmonyPatch(typeof(GridsUtility), nameof(GridsUtility.Fogged), new []{typeof(IntVec3), typeof(Map)})]
	public class GridsUtility_Patch
	{
		private static bool Prefix(Map map, ref bool __result)
		{
			if (map == null)
			{
				return __result = false;
			}
			return true;
		}
	}
}
