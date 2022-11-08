using System;
using System.Security.Cryptography;
using Mono.Unix.Native;
using RimWorld;
using UnityEngine;
using Verse;
using static CombatAI.AvoidanceTracker;
using static CombatAI.SightTracker;

namespace CombatAI
{
    public static class CombatAI_Utility
    {
        public static bool GetAvoidanceTracker(this Pawn pawn, out AvoidanceTracker.AvoidanceReader reader)
        {            
            return pawn.Map.GetComp_Fast<AvoidanceTracker>().TryGetReader(pawn, out reader);
        }

        public static TGrid<float> GetTGrid(this Map map)
        {
            TGrid<float> grid = map.GetComp_Fast<MapComponent_CombatAI>().tempGrid;
            grid.Reset();
            return grid;
        }

        public static Verb TryGetAttackVerb(this Thing thing)
        {
            if (thing is Pawn pawn)
            {
                if (pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb.Available())
                {
                    return pawn.equipment.PrimaryEq.PrimaryVerb;
                }
                return pawn.meleeVerbs?.curMeleeVerb ?? null;
            }
            return null;
        }

        public static bool HasWeaponVisible(this Pawn pawn)
        {
            return (pawn.CurJob?.def.alwaysShowWeapon ?? false) || (pawn.mindState?.duty?.def.alwaysShowWeapon ?? false);
        }

        public static bool GetSightReader(this Pawn pawn, out SightTracker.SightReader reader)
        {
            SightTracker tracker = pawn.Map.GetComp_Fast<SightTracker>();
            return tracker.TryGetReader(pawn, out reader);
        }

        public static UInt64 GetThingFlags(this Thing thing)
        {
            return ((UInt64)1) << (GetThingFlagsIndex(thing));
        }

        public static int GetThingFlagsIndex(this Thing thing)
        {
            return thing.thingIDNumber % 64;
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

