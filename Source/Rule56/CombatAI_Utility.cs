using System.Runtime.CompilerServices;
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

        public static bool Is<T>(this Job job, T other) where T : Def
        {
            return job != null && other != null && job.def == other;
        }

        public static bool Is<T>(this PawnDuty duty, T other) where T : Def
        {
            return duty != null && other != null && duty.def == other;
        }

        public static bool Is<T>(this Thing thing, T other) where T : Def
        {
            return thing != null && other != null && thing.def == other;
        }

        public static bool Is<T>(this Thing thing, Thing other) where T : Def
        {
            return thing != null && other != null && thing == other;
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

        public static bool IsApproachingMeleeTarget(this Pawn pawn, float distLimit = 5, bool allowCached = true)
        {
            if (!allowCached || !TKVCache<int, IsApproachingMeleeTargetCache, bool>.TryGet(pawn.thingIDNumber, out bool result, 5))
            {
                result = IsApproachingMeleeTarget(pawn, out _, distLimit);
                if (allowCached)
                {
                    TKVCache<int, IsApproachingMeleeTargetCache, bool>.Put(pawn.thingIDNumber, result);
                }
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsApproachingMeleeTarget(this Pawn pawn, out Thing target, float distLimit = 5)
        {
            target = null;
            Job attackJob;
            return (attackJob = pawn.CurJob).Is(JobDefOf.AttackMelee) && attackJob.targetA.IsValid && attackJob.targetA.Cell.DistanceToSquared(pawn.Position) <= distLimit * distLimit && (target = attackJob.targetA.Thing) != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Verb TryGetAttackVerb(this Thing thing)
        {
            if (thing is Pawn pawn)
            {
                Pawn_EquipmentTracker equipment = pawn.equipment;
                if (equipment is { Primary: { } } && equipment.PrimaryEq.PrimaryVerb.Available() && (!equipment.PrimaryEq.PrimaryVerb.verbProps.onlyManualCast || pawn.CurJob != null && pawn.CurJob.def != JobDefOf.Wait_Combat))
                {
                    return equipment.PrimaryEq.PrimaryVerb;
                }
                Pawn_MeleeVerbs meleeVerbs = pawn.meleeVerbs;
                if (meleeVerbs != null)
                {
                    if (meleeVerbs.curMeleeVerb != null)
                    {
                        return meleeVerbs.curMeleeVerb;
                    }
                    if (!TKVCache<int, Pawn_MeleeVerbs, Verb>.TryGet(thing.thingIDNumber, out Verb verb, 480) || verb == null || verb.DirectOwner != thing)
                    {
                        TKVCache<int, Pawn_MeleeVerbs, Verb>.Put(thing.thingIDNumber, verb = meleeVerbs.TryGetMeleeVerb(null));
                        return verb;
                    }
                }
                return null;
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

        private class IsApproachingMeleeTargetCache
        {
        }
    }
}
