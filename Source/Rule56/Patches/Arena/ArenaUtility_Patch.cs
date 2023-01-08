using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace CombatAI.Patches.Arean
{
	public static class ArenaUtility_Patch
	{		
		private const int MapWidth = 125;

		private static IEnumerable<CodeInstruction> Replace_Ldc_I4_S(IEnumerable<CodeInstruction> instructions, int old, int value)
		{
			foreach(var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.OperandIs(old))			
					yield return new CodeInstruction(OpCodes.Ldc_I4_S, value).MoveBlocksFrom(instruction).MoveLabelsFrom(instruction);				
				else				
					yield return instruction;
			}			
		}

		private static IEnumerable<CodeInstruction> Replace_Ldc_I4_S(IEnumerable<CodeInstruction> instructions, int old, CodeInstruction newInstruction)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.OperandIs(old))
					yield return newInstruction.MoveBlocksFrom(instruction).MoveLabelsFrom(instruction);
				else
					yield return instruction;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static int GetMaxArenaMapNum()
		{
			return Finder.Settings.Debug_ArenaMaxMapNum;
		}

		[HarmonyPatch]
		private static class ArenaUtilityInner_Patch
		{
			public static IEnumerable<MethodBase> TargetMethods()
			{				
				yield return AccessTools.Method(typeof(ArenaUtility), nameof(ArenaUtility.ArenaFightQueue));
				yield return typeof(ArenaUtility).GetNestedTypes(AccessTools.all).First(t => t.Name.Contains("8_0")).GetMethods(AccessTools.all).First(m => m.Name.Contains("b__0"));
			}

			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				return Replace_Ldc_I4_S(instructions, 15, new CodeInstruction(OpCodes.Call, AccessTools.Method("ArenaUtility_Patch:GetMaxArenaMapNum")));
			}
		}

		[HarmonyPatch(typeof(ArenaUtility))]
		private static class ArenaUtilityInner2_Patch
		{
			[HarmonyPatch(nameof(ArenaUtility.BeginArenaFight))]
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> T_BeginArenaFight(IEnumerable<CodeInstruction> instructions)
			{
				return Replace_Ldc_I4_S(instructions, 50, MapWidth);
			}
		}		
	}
}

