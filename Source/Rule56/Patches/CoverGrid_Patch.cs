using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatAI.Patches
{
    public static class CoverGrid_Patch
    {
        public static WallGrid grid;
        public static MethodBase mCellToIndex = AccessTools.Method(typeof(CellIndices), nameof(CellIndices.CellToIndex), new[] { typeof(IntVec3) });
        public static MethodBase mCellToIndex2 = AccessTools.Method(typeof(CellIndices), nameof(CellIndices.CellToIndex), new[] { typeof(int), typeof(int) });

        [HarmonyPatch(typeof(CoverGrid), nameof(CoverGrid.Register))]
        public static class CoverGrid_Register_Patch
        {
            public static void Prefix(CoverGrid __instance, Thing t)
            {
                grid = t.def.fillPercent > 0 ?
                     __instance.map.GetComp_Fast<WallGrid>() : null;
            }

            public static void Postfix()
            {
                grid = null;
            }
        }

        [HarmonyPatch(typeof(CoverGrid), nameof(CoverGrid.DeRegister))]
        public static class CoverGrid_DeRegister_Patch
        {
            public static void Prefix(CoverGrid __instance, Thing t)
            {
                grid = t.def.fillPercent > 0 ?
                    __instance.map.GetComp_Fast<WallGrid>() : null;
            }

            public static void Postfix()
            {
                grid = null;
            }
        }

        [HarmonyPatch(typeof(CoverGrid), nameof(CoverGrid.RecalculateCell))]
        public static class CoverGrid_RecalculateCell_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (!finished)
                    {
                        if (codes[i].opcode == OpCodes.Ret)
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(codes[i]).MoveBlocksFrom(codes[i]);
                            yield return new CodeInstruction(OpCodes.Ldloc_0);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CoverGrid_Patch), nameof(CoverGrid_Patch.Set)));
                        }
                    }
                    yield return codes[i];
                }
            }
        }

        public static void Set(IntVec3 cell, Thing t)
        {
            if (grid != null)
            {
                grid.RecalculateCell(cell, t);
            }
        }
    }
}

