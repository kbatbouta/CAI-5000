using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
    public static class GenSight_Patch
    {
        private static Map                 map;
        private static InterceptorTracker  interceptors;
        private static ITByteGrid          grid;
        private static Func<IntVec3, bool> validator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ValidateWithValidator(IntVec3 cell)
        {
            return (grid.GetFlags(cell) & (ulong)InterceptorFlags.interceptNonHostileProjectiles) == 0 && validator(cell);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ValidateWithNoValidator(IntVec3 cell)
        {
            return (grid.GetFlags(cell) & (ulong)InterceptorFlags.interceptNonHostileProjectiles) == 0;
        }

        public static void ClearCache()
        {
            map          = null;
            grid         = null;
            interceptors = null;
            validator    = null;
        }

        [HarmonyPatch(typeof(GenSight), nameof(GenSight.LineOfSight), typeof(IntVec3), typeof(IntVec3), typeof(Map), typeof(bool), typeof(Func<IntVec3, bool>), typeof(int), typeof(int))]
        private static class GenSight_LineOfSight1_Patch
        {
            public static void Prefix(IntVec3 start, IntVec3 end, Map map, bool skipFirstCell, ref Func<IntVec3, bool> validator, int halfXOffset, int halfZOffset)
            {
                if (GenSight_Patch.map != map)
                {
                    GenSight_Patch.map = map;
                    grid               = (interceptors = map.GetComp_Fast<MapComponent_CombatAI>().interceptors).grid;
                }
                if (interceptors.Count != 0 && grid.Get(start) == 0 && grid.Get(end) == 0)
                {
                    if (validator == null)
                    {
                        GenSight_Patch.validator = null;
                        validator                = ValidateWithNoValidator;
                    }
                    else
                    {
                        GenSight_Patch.validator = validator;
                        validator                = ValidateWithValidator;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GenSight), nameof(GenSight.LineOfSight), typeof(IntVec3), typeof(IntVec3), typeof(Map), typeof(CellRect), typeof(CellRect), typeof(Func<IntVec3, bool>))]
        private static class GenSight_LineOfSight2_Patch
        {
            public static void Prefix(IntVec3 start, IntVec3 end, Map map, CellRect startRect, CellRect endRect, ref Func<IntVec3, bool> validator)
            {
                if (GenSight_Patch.map != map)
                {
                    GenSight_Patch.map = map;
                    grid               = (interceptors = map.GetComp_Fast<MapComponent_CombatAI>().interceptors).grid;
                }
                if (interceptors.Count != 0 && grid.Get(start) == 0 && grid.Get(end) == 0)
                {
                    if (validator == null)
                    {
                        GenSight_Patch.validator = null;
                        validator                = ValidateWithNoValidator;
                    }
                    else
                    {
                        GenSight_Patch.validator = validator;
                        validator                = ValidateWithValidator;
                    }
                }
            }
        }
    }
}
