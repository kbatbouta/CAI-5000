using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
	public static class TrashUtility_Patch
	{
		private static SightTracker.SightReader sightReader;
		private static Pawn                     sightPawn;
		
		[HarmonyPatch(typeof(TrashUtility), nameof(TrashUtility.ShouldTrashBuilding), new []{typeof(Pawn), typeof(Building), typeof(bool)})]
		private static class TrashUtility_ShouldTrashBuilding_Patch
		{
			public static void Postfix(Pawn pawn, Building b, bool attackAllInert, ref bool __result)
			{
				if (__result)
				{
					if ((sightPawn == pawn && sightReader != null) || (sightPawn = pawn).TryGetSightReader(out sightReader))
					{
						foreach (IntVec3 cell in b.OccupiedRect())
						{
							if (sightReader.GetVisibilityToEnemies(cell) > 0)
							{
								__result = false;
								break;
							}
						}
					}
				}
			}
		}
		
		[HarmonyPatch(typeof(TrashUtility), nameof(TrashUtility.CanTrash))]
		private static class TrashUtility_CanTrash_Patch
		{
			public static void Postfix(Pawn pawn, Thing t, ref bool __result)
			{
				if (__result)
				{
					if ((sightPawn == pawn && sightReader != null) || (sightPawn = pawn).TryGetSightReader(out sightReader))
					{
						foreach (IntVec3 cell in t.OccupiedRect())
						{
							if (sightReader.GetVisibilityToEnemies(cell) > 0)
							{
								__result = false;
								break;
							}
						}
					}
				}
			}
		}

		public static void ClearCache()
		{
			sightPawn   = null;
			sightReader = null;
		}
	}
}
