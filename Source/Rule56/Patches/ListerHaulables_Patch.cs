using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
	public static class ListerHaulables_Patch
	{
		[HarmonyPatch(typeof(ListerHaulables), nameof(ListerHaulables.Notify_Forbidden))]
		public static class ListerHaulables_Notify_Forbidden_Patch
		{
			public static void Postfix(Thing t)
			{
				if (BattleRoyale.enabled && (BattleRoyale.manager?.Active ?? false) && t != null && t.Spawned && !t.Destroyed)
				{
					t.Map.GetComp_Fast<MapBattleRoyale>().forbidenItems.Add(t);
				}
			}
		}
	}
}

