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
			if (!TKVCache<Thing, CompCanBeDormant, bool>.TryGet(thing, out bool value, 240))
			{
				CompCanBeDormant dormant = thing.GetComp_Fast<CompCanBeDormant>();
				TKVCache<Thing, CompCanBeDormant, bool>.Put(thing, dormant != null && !dormant.Awake);
			}
			return value;
		}

		public static IntVec3 TryGetNextDutyDest(this Pawn pawn, float maxDistFromPawn = -1)
		{
			if (pawn.mindState?.duty == null || !pawn.mindState.duty.focus.IsValid)
			{
				return IntVec3.Invalid;
			}
			Tuple<int, int, IntVec3> key = new Tuple<int, int, IntVec3>
			{
				val1 = pawn.thingIDNumber,
				val2 = pawn.mindState.duty.def.index,
				val3 = pawn.mindState.duty.focus.Cell
			};
			if (!TKVCache<Tuple<int, int, IntVec3>, PawnDuty, IntVec3>.TryGet(key, out IntVec3 dutyDest, 600))
			{
				dutyDest = IntVec3.Invalid;
				if (pawn.mindState.duty.focus.Cell.DistanceToSquared(pawn.Position) > Maths.Sqr(Maths.Max(pawn.mindState.duty.radius, 10)))
				{
					PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, pawn.mindState.duty.focus.Cell, pawn);
					if (path != null && path.nodes.Count > 0)
					{
						maxDistFromPawn = Mathf.Clamp(maxDistFromPawn, 5f, 64f);
						int     i          = path.nodes.Count - 1;
						float   maxDistSqr = Maths.Sqr(maxDistFromPawn);
						IntVec3 pawnPos    = pawn.Position;
						while (i >= 0 && path.nodes[i].DistanceToSquared(pawnPos) < maxDistSqr)
						{
							i--;
						}
						dutyDest = path.nodes[Maths.Max(i, 0)];
						path.ReleaseToPool();
					}
				}
				TKVCache<Tuple<int, int, IntVec3>, PawnDuty, IntVec3>.Put(key, dutyDest);
			}
			return dutyDest;
		}

		public static Verb TryGetAttackVerb(this Thing thing)
		{
			if (thing is Pawn pawn)
			{
				return pawn.CurrentEffectiveVerb;
			}
			if (thing is Building_Turret turret)
			{
				return turret.AttackVerb;
			}
			return null;
		}

		public static bool HasWeaponVisible(this Pawn pawn)
		{
			return (pawn.CurJob?.def.alwaysShowWeapon ?? false) || (pawn.mindState?.duty?.def.alwaysShowWeapon ?? false);
		}

		public static bool TryGetAvoidanceReader(this Pawn pawn, out AvoidanceReader reader)
		{
			return pawn.Map.GetComp_Fast<AvoidanceTracker>().TryGetReader(pawn, out reader);
		}


		public static bool TryGetSightReader(this Pawn pawn, out SightReader reader)
		{
			if (pawn.Map.GetComp_Fast<SightTracker>().TryGetReader(pawn, out reader) && reader != null)
			{
				reader.armor = pawn.GetArmorReport();
				return true;
			}
			return false;
		}

		public static ISGrid<float> GetFloatGrid(this Map map)
		{
			ISGrid<float> grid = map.GetComp_Fast<MapComponent_CombatAI>().f_grid;
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
