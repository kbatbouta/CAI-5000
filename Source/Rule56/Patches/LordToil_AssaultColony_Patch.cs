using System;
using System.Collections.Generic;
using System.Linq;
using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CombatAI.Patches
{
	public static class LordToil_AssaultColony_Patch
	{
		private static List<Pawn>[] forces = new List<Pawn>[5];
		private static List<Zone_Stockpile> stockpiles = new List<Zone_Stockpile>();

		static LordToil_AssaultColony_Patch()
		{
			forces[0] = new List<Pawn>();
			forces[1] = new List<Pawn>();
			forces[2] = new List<Pawn>();
			forces[3] = new List<Pawn>();
			forces[4] = new List<Pawn>();
		}

		public static void ClearCache()
		{
			stockpiles.Clear();
			forces[0].Clear();
			forces[1].Clear();
			forces[2].Clear();
			forces[3].Clear();
			forces[4].Clear();
		}

		[HarmonyPatch(typeof(LordToil_AssaultColony), nameof(LordToil_AssaultColony.UpdateAllDuties))]
		private static class LordToil_AssaultColony_UpdateAllDuties_Patch
		{			
			public static void Postfix(LordToil_AssaultColony __instance)
			{
				if (__instance.lord.ownedPawns.Count > 10)
				{
					ClearCache();
					stockpiles.AddRange(__instance.Map.zoneManager.AllZones.Where(z => z is Zone_Stockpile).Select(z => z as Zone_Stockpile));
					if(stockpiles.Count == 0)
					{
						return;
					}
					int m = Rand.Int % 5 + 1;
					int taskForceNum = Maths.Min(__instance.lord.ownedPawns.Count / 5, 5);
					for (int i = 0; i < __instance.lord.ownedPawns.Count; i++)
					{
						int k = Rand.Range(0, taskForceNum + m);
						if (k <= m)
						{
							continue;
						}
						forces[k - m].Add(__instance.lord.ownedPawns[i]);						
					}
					for(int i = 0; i < 5; i++)
					{						
						List<Pawn> force = forces[i];
						IntVec3 cell = stockpiles.RandomElementByWeight(s => (int)s.settings.Priority / 5f + GetStockpileTotalMarketValue(s) / 100f).cells.RandomElement();
						for (int j = 0; j < force.Count; j++)
						{
							ThingComp_CombatAI comp = force[j].GetComp_Fast<ThingComp_CombatAI>();
							if(comp != null && !comp.duties.Any(DutyDefOf.Defend))
							{
								var customDuty = CustomDutyUtility.AssaultPoint(force[j], cell, Rand.Range(7, 15), 3600);
								comp.duties.StartDuty(customDuty, true);
							}												
						}
					}
					ClearCache();
				}
			}
		}

		private static float GetStockpileTotalMarketValue(Zone_Stockpile stockpile)
		{
			if (!TKVCache<int, Zone_Stockpile, float>.TryGet(stockpile.ID, out float val, 6000))
			{
				val = stockpile.AllContainedThings.Sum(t => t.GetStatValue_Fast(StatDefOf.MarketValue, 1200));
				TKVCache<int, Zone_Stockpile, float>.Put(stockpile.ID, val);
			}
			return val;
		}	
	}
}

