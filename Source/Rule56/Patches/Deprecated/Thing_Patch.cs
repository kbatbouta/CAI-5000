using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;
using System.Linq;
using System.Reflection.Emit;

#if DEBUG_REACTION
using CombatAI.Utilities;
#endif

namespace CombatAI.Patches
{
#if DEBUG_REACTION
	public static class Thing_Patch
	{
		[HarmonyPatch(typeof(Thing), nameof(Thing.Position), MethodType.Setter)]
		public static class Thing_Position_Patch
		{
			private static MethodInfo mRegister = AccessTools.Method(typeof(ThingGrid), nameof(ThingGrid.Register));

			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
			{
				List<CodeInstruction> codes    = instructions.ToList();
				bool                  finished = false;

				for (int i = 0; i < codes.Count; i++)
				{
					yield return codes[i];
					if (!finished)
					{
						if (codes[i].opcode == OpCodes.Callvirt && codes[i].OperandIs(mRegister))
						{
							finished = true;
							yield return new CodeInstruction(OpCodes.Ldarg_0);
							yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Thing_Position_Patch), nameof(OnPositionChanged)));
						}
					}
				}
			}

			private static void OnPositionChanged(Thing thing)
			{
				thing.Map.GetComp_Fast<ThingsTracker>()?.Notify_PositionChanged(thing);
			}
		}

		[HarmonyPatch]
		public static class Thing_DeSpawn_Patch
		{
			public static IEnumerable<MethodBase> TargetMethods()
			{
				yield return AccessTools.Method(typeof(Thing), nameof(Thing.DeSpawn));
				yield return AccessTools.Method(typeof(Thing), nameof(Thing.ForceSetStateToUnspawned));
			}

			[HarmonyPriority(Priority.First)]
			public static void Prefix(Thing __instance)
			{
				__instance.Map?.GetComp_Fast<ThingsTracker>()?.Notify_DeSpawned(__instance);
			}
		}

		[HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
		public static class Thing_SpawnSetup_Patch
		{
			[HarmonyPriority(Priority.First)]
			public static void Postfix(Thing __instance)
			{
				__instance.Map?.GetComp_Fast<ThingsTracker>()?.Notify_Spawned(__instance);
			}
		}
	}
#endif
}
