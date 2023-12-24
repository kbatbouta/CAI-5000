using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
    public static class JobGiver_AITrashBuildingsDistant_Patch
    {
        private static readonly Dictionary<int, int> lastGaveById = new Dictionary<int, int>();

        public static void ClearCache()
        {
            lastGaveById.Clear();
        }

        [HarmonyPatch(typeof(JobGiver_AITrashBuildingsDistant), nameof(JobGiver_AITrashBuildingsDistant.TryGiveJob))]
        private static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
        {
            public static bool Prefix(Pawn pawn)
            {
                if (lastGaveById.TryGetValue(pawn.thingIDNumber, out int ticks) && GenTicks.TicksGame - ticks < 30)
                {
                    return false;
                }
                if (pawn.TryGetSightReader(out SightTracker.SightReader reader) && reader.GetVisibilityToEnemies(pawn.Position) > 0)
                {
	                return false;
                }
                return true;
            }

            public static void Postfix(Pawn pawn, Job __result)
            {
                if (__result != null)
                {
                    lastGaveById[pawn.thingIDNumber] = GenTicks.TicksGame;
                }
                else
                {
                    lastGaveById[pawn.thingIDNumber] = GenTicks.TicksGame + 20;
                }
            }
        }
    }
}
