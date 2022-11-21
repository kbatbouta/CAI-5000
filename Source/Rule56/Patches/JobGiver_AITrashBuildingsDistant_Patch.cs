using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;
using System.Reflection;


namespace CombatAI.Patches
{
	public static class JobGiver_AITrashBuildingsDistant_Patch
	{
		private static Dictionary<int, int> lastGaveById = new Dictionary<int, int>();

		[HarmonyPatch(typeof(JobGiver_AITrashBuildingsDistant), nameof(JobGiver_AITrashBuildingsDistant.TryGiveJob))]
		private static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
		{
			public static bool Prefix(Pawn pawn)
			{				
                if (lastGaveById.TryGetValue(pawn.thingIDNumber, out int ticks) && GenTicks.TicksGame - ticks < 30)
				{	
					return false;
				}
				return true;
			}

			public static void Postfix(Pawn pawn, Job __result)
			{
				if(__result != null)
				{
					lastGaveById[pawn.thingIDNumber] = GenTicks.TicksGame;
                }
			}
		}

		public static void ClearCache()
		{
            lastGaveById.Clear();
        }
    }
}

