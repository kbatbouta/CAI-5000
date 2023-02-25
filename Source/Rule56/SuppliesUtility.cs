using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
namespace CombatAI
{
    public class SuppliesUtility
    {
        private static readonly HashSet<ThingDef> temp = new HashSet<ThingDef>();

        public static void DropSupplies(Map map, IntVec3 position, ThingDef thingDef, int count)
        {
            List<Thing> list  = new List<Thing>();
            Thing       thing = ThingMaker.MakeThing(thingDef);
            thing.stackCount = count;
            list.Add(thing);
            DropPodUtility.DropThingsNear(position, map, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ThingDef> PotentialFoodFor(Pawn pawn)
        {
            return PotentialFoodFor(pawn.RaceProps);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ThingDef> PotentialFoodFor(RaceProperties race)
        {
            if (!race.EatsFood)
            {
                yield break;
            }
            // TODO add more food types.
            yield return ThingDefOf.MealSurvivalPack;
        }

        public static void FulfillFoodSupplies(List<Pawn> pawns, Map map)
        {
            if (pawns.Count == 0)
            {
                return;
            }
            TechLevel tech = pawns.RandomElement().Faction?.def?.techLevel ?? TechLevel.Undefined;
            if (tech == TechLevel.Animal || tech == TechLevel.Medieval || tech == TechLevel.Neolithic)
            {
                return;
            }
            temp.Clear();
            temp.AddRange(pawns.SelectMany(PotentialFoodFor));
            float   mul    = tech == TechLevel.Industrial ? Rand.Range(1.0f, 1.5f) : Rand.Range(1.5f, 2.5f);
            IntVec3 center = IntVec3.Zero;
            for (int i = 0; i < pawns.Count; i++)
            {
                center += pawns[i].Position;
            }
            center.x /= pawns.Count;
            center.z /= pawns.Count;
            IntVec3 dropPoint  = center;
            float   minDistSqr = 1e6f;
            for (int i = 0; i < pawns.Count; i++)
            {
                float distSqr = pawns[i].Position.DistanceToSquared(center);
                if (distSqr < minDistSqr)
                {
                    Region region = pawns[i].GetRegion();
                    if (region != null)
                    {
                        int     k    = 0;
                        IntVec3 cell = IntVec3.Invalid;
                        while (k++ < 16 && !(cell = region.RandomCell).Walkable(map))
                        {
                        }
                        if (cell.IsValid && cell.Walkable(map))
                        {
                            minDistSqr = distSqr;
                            dropPoint  = region.RandomCell;
                        }
                    }
                }
            }
            if (dropPoint.IsValid && dropPoint.Walkable(map))
            {
                foreach (ThingDef def in temp)
                {
                    DropSupplies(map, dropPoint, def, (int)(pawns.Count() * mul));
                }
            }
        }
    }
}
