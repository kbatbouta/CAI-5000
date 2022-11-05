using System;
using UnityEngine;
using Verse;

namespace CombatAI
{
    public static class CombatAI_Utility
    {
        public static bool GetSightReader(this Pawn pawn, out SightTracker.SightReader reader)
        {
            SightTracker tracker = pawn.Map.GetComp_Fast<SightTracker>();
            return tracker.TryGetReader(pawn, out reader);
        }

        public static UInt64 GetCombatFlags(this Thing thing)
        {
            return ((UInt64)1) << (thing.thingIDNumber % 64);
        }

        public static float DistanceToSegmentSquared(this Vector3 point, Vector3 lineStart, Vector3 lineEnd, out Vector3 closest)
        {
            float dx = lineEnd.x - lineStart.x;
            float dz = lineEnd.z - lineStart.z;
            if ((dx == 0) && (dz == 0))
            {
                closest = lineStart;
                dx = point.x - lineStart.x;
                dz = point.z - lineStart.z;
                return dx * dx + dz * dz;
            }
            float t = ((point.x - lineStart.x) * dx + (point.z - lineStart.z) * dz) / (dx * dx + dz * dz);
            if (t < 0)
            {
                closest = new Vector3(lineStart.x, 0, lineStart.z);
                dx = point.x - lineStart.x;
                dz = point.z - lineStart.z;
            }
            else if (t > 1)
            {
                closest = new Vector3(lineEnd.x, 0, lineEnd.z);
                dx = point.x - lineEnd.x;
                dz = point.z - lineEnd.z;
            }
            else
            {
                closest = new Vector3(lineStart.x + t * dx, 0, lineStart.z + t * dz);
                dx = point.x - closest.x;
                dz = point.z - closest.z;
            }
            return dx * dx + dz * dz;
        }

        public static CellFlooder GetCellFlooder(this Map map)
        {
            return map.GetComp_Fast<MapComponent_CombatAI>().flooder;
        }
    }
}

