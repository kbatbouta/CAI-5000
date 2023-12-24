using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public static class GenNearby
    {
        public static void NearbyPawns(this IntVec3 root, Map map, List<Pawn> pawns, TraverseParms parms, int maxReigons, int maxDist, int maxNum, Predicate<Pawn> validator = null)
        {
            Predicate<Thing> func = t =>
            {
                if (t is Pawn pawn && validator(pawn) && !pawns.Contains(pawn))
                {
                    pawns.Add(pawn);
                }
                return pawns.Count >= maxNum;
            };
            Verse.GenClosest.RegionwiseBFSWorker(root, map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.InteractionCell, parms, func, null, 1, maxReigons, maxDist, out int _);
        }
    }
}
