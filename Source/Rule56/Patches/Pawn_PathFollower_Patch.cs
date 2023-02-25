using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
    public static class Pawn_PathFollower_Patch
    {

        private static bool WalkableBy(IntVec3 cell, Map map, Pawn pawn)
        {
            if (Finder.Settings.Debug)
            {
                return cell.WalkableBy(map, pawn);
            }
            return true;
        }

        [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.SetupMoveIntoNextCell))]
        private static class Pawn_PathFollower__Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                return instructions.MethodReplacer(AccessTools.Method(typeof(GenGrid), nameof(GenGrid.WalkableBy)), AccessTools.Method(typeof(Pawn_PathFollower_Patch), nameof(WalkableBy)));
            }
        }
    }
}
