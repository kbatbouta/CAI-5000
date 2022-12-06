﻿using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using static CombatAI.AvoidanceTracker;
using static CombatAI.SightTracker;

namespace CombatAI
{
	public static class CombatAI_Utility
	{
		public static bool Is<T>(this T def, T other) where T : Def
		{
			return def != null && other != null && def == other;
		}

		public static bool IsDormant(this Thing thing)
		{
			if (!TKVCache<Thing, CompCanBeDormant, bool>.TryGet(thing, out var value, 240))
			{
				var dormant = thing.GetComp_Fast<CompCanBeDormant>();
				TKVCache<Thing, CompCanBeDormant, bool>.Put(thing, dormant != null && !dormant.Awake);
			}

			return value;
		}

		public static Verb TryGetAttackVerb(this Thing thing)
		{
			if (thing is Pawn pawn) return pawn.CurrentEffectiveVerb;
			if (thing is Building_Turret turret) return turret.AttackVerb;
			return null;
		}

		public static bool HasWeaponVisible(this Pawn pawn)
		{
			return (pawn.CurJob?.def.alwaysShowWeapon ?? false) ||
			       (pawn.mindState?.duty?.def.alwaysShowWeapon ?? false);
		}

		public static bool TryGetAvoidanceReader(this Pawn pawn, out AvoidanceReader reader)
		{
			return pawn.Map.GetComp_Fast<AvoidanceTracker>().TryGetReader(pawn, out reader);
		}


		public static bool TryGetSightReader(this Pawn pawn, out SightReader reader)
		{
			if (pawn.Map.GetComp_Fast<SightTracker>().TryGetReader(pawn, out reader) && reader != null)
			{
				reader.armor = ArmorUtility.GetArmorReport(pawn);
				return true;
			}

			return false;
		}

		public static ISGrid<float> GetFloatGrid(this Map map)
		{
			var grid = map.GetComp_Fast<MapComponent_CombatAI>().f_grid;
			grid.Reset();
			return grid;
		}

		public static CellFlooder GetCellFlooder(this Map map)
		{
			return map.GetComp_Fast<MapComponent_CombatAI>().flooder;
		}


		public static ulong GetThingFlags(this Thing thing)
		{
			return (ulong)1 << GetThingFlagsIndex(thing);
		}

		public static int GetThingFlagsIndex(this Thing thing)
		{
			return thing.thingIDNumber % 64;
		}
	}
}