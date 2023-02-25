using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public static partial class ShadowCastingUtility
    {
        private static          object  locker   = new object();
        private static readonly IntVec3 _offsetH = new IntVec3(1, 0, 0);
        private static readonly IntVec3 _offsetV = new IntVec3(0, 0, 1);

        private static IntVec3 GetBaseOffset(int quartor)
        {
            return quartor == 0 || quartor == 2 ? _offsetV : _offsetH;
        }

        private static int GetNextQuartor(int quartor)
        {
            return (quartor + 1) % 4;
        }
        private static int GetPreviousQuartor(int quartor)
        {
            return quartor - 1 < 0 ? 3 : quartor - 1;
        }

        /// <summary>
        ///     Evaluate all cells around the source for visibility (float value).
        ///     Note: this is a slower but more accurate version of CastVisibility.
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="radius">Radius</param>
        public static void CastWeighted(Map map, IntVec3 source, Action<IntVec3, int, int, float> setAction, int radius, List<Vector3> buffer)
        {
            Cast(map, TryCastWeighted, setAction, source, radius, VISIBILITY_CARRY_MAX, buffer);
        }
        /// <summary>
        ///     Evaluate all cells around the source for visibility (float value).
        ///     Note: this is a slower but more accurate version of CastVisibility.
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="maxRadius">Radius</param>
        /// <param name="carryLimit">Cover limit</param>
        public static void CastWeighted(Map map, IntVec3 source, Action<IntVec3, int, int, float> setAction, int radius, int carryLimit, List<Vector3> buffer)
        {
            Cast(map, TryCastWeighted, setAction, source, radius, carryLimit, buffer);
        }

        /// <summary>
        ///     Evaluate all cells around the source for visibility (on/off).
        ///     Note: this is a faster but less accurate version of CastVisibility.
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="radius">Radius</param>
        public static void CastVisibility(Map map, IntVec3 source, Action<IntVec3, int> action, int radius, List<Vector3> buffer)
        {
            Cast(map, TryCastVisibility, (cell, _, dist, ignore) => action(cell, dist), source, radius, VISIBILITY_CARRY_MAX, buffer);
        }

        private static void Cast(Map map, Action<float, float, int, int, int, IntVec3, Map, Action<IntVec3, int, int, float>, List<Vector3>> castingAction, Action<IntVec3, int, int, float> action, IntVec3 source, int radius, int carryLimit, List<Vector3> buffer)
        {
            int maxDepth = radius;
            for (int i = 0; i < 4; i++)
            {
                castingAction(-1f, 1f, i, maxDepth, carryLimit, source, map, action, buffer);
            }
        }

        /// <summary>
        ///     Evaluate visible cells from the source in the direction of target. Will result in storing the cover visiblity
        ///     (on/off) for each each cell in the ShadowGrid
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="direction">Direction</param>
        /// <param name="baseWidth">What is the maximum amount of cells (width) to be scanned</param>
        public static void CastVisibility(Map map, IntVec3 source, Vector3 direction, Action<IntVec3, int> action, float radius, float baseWidth, List<Vector3> buffer)
        {
            Cast(map, TryCastVisibilitySimple, (cell, _, dist, ignore) => action(cell, dist), source, (source.ToVector3() + direction.normalized * radius).ToIntVec3(), baseWidth, VISIBILITY_CARRY_MAX, buffer);
        }

        /// <summary>
        ///     Evaluate visible cells from the source in the direction of target. Will result in storing the cover visiblity
        ///     (float value) scoring for each each cell in the ShadowGrid
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="source">Source cell</param>
        /// <param name="direction">Direction</param>
        /// <param name="action">Set action (x, z, current_ray_value)</param>
        /// <param name="baseWidth">What is the maximum amount of cells (width) to be scanned</param>
        public static void CastWeighted(Map map, IntVec3 source, Vector3 direction, Action<IntVec3, int, int, float> action, float range, float baseWidth, int carryLimit, List<Vector3> buffer)
        {
            Cast(map, TryCastWeightedSimple, action, source, (source.ToVector3() + direction.normalized * range).ToIntVec3(), baseWidth, carryLimit, buffer);
        }

        private static void Cast(Map map, Action<float, float, int, int, int, IntVec3, Map, Action<IntVec3, int, int, float>, List<Vector3>> castingAction, Action<IntVec3, int, int, float> setAction, IntVec3 source, IntVec3 target, float baseWidth, int carryLimit, List<Vector3> buffer)
        {
            // get which quartor the target is in.
            int     quartor    = GetQurator(target - source);
            Vector3 relTarget  = _transformationInverseFuncsV3[quartor]((target - source).ToVector3());
            Vector3 relDir     = relTarget.normalized;
            Vector3 relStart   = relTarget + new Vector3(relDir.y, 0, -relDir.x) * baseWidth / 2;
            Vector3 relEnd     = relTarget - new Vector3(relDir.y, 0, -relDir.x) * baseWidth / 2;
            int     maxDepth   = (int)source.DistanceTo(target);
            float   startSlope = GetSlope(relStart);
            float   endSlope   = GetSlope(relEnd);
            if (startSlope < -1)
            {
                float slope = Maths.Max(startSlope + 2, 0);
                castingAction(slope, 1, GetNextQuartor(quartor), maxDepth, carryLimit, source, map, setAction, buffer);
                startSlope = -1;
            }
            if (endSlope > 1)
            {
                float slope = Maths.Min(endSlope - 2, 0);
                castingAction(-1, slope, GetPreviousQuartor(quartor), maxDepth, carryLimit, source, map, setAction, buffer);
                endSlope = 1;
            }
            castingAction(startSlope, endSlope, quartor, maxDepth, carryLimit, source, map, setAction, buffer);
        }
    }
}
