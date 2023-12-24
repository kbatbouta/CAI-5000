using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public class Verb_Patch
    {
	    private static Verb callerVerb;

	    [HarmonyPatch]
        private static class Verb_TryStartCast_Patch
        {
	        public static IEnumerable<MethodBase> TargetMethods()
            {
                HashSet<MethodBase> methods = new HashSet<MethodBase>();
                foreach (Type t in typeof(Verb).AllSubclasses())
                {
                    foreach (MethodInfo method in t.GetMethods(AccessTools.all))
                    {
                        if (method != null && !methods.Contains(method) && !method.IsStatic && method.ReturnType == typeof(bool) && (method.Name.Contains("TryStartCastOn") || method.Name.Contains("TryCastShot")) && !method.IsAbstract && method.HasMethodBody() && method.DeclaringType == t)
                        {
                            Log.Message($"ISMA: Patched verb type {t.FullName}:{method.Name}");
                            methods.Add(method);
                        }
                    }
                }
                foreach (MethodInfo method in typeof(Verb).GetMethods(AccessTools.all))
                {
                    if (method != null && !methods.Contains(method) && !method.IsStatic && method.ReturnType == typeof(bool) && (method.Name.Contains("TryStartCastOn") || method.Name.Contains("TryCastShot")) && !method.IsAbstract && method.HasMethodBody() && method.DeclaringType == typeof(Verb))
                    {
                        Log.Message($"ISMA: Patched verb type {typeof(Verb).FullName}:{method.Name}");
                        methods.Add(method);
                    }
                }
                return methods;
            }

            public static void Prefix(Verb __instance, out bool __state)
            {
                if (__state = __instance != callerVerb)
                {
                    callerVerb = __instance;
                }
            }

            public static void Postfix(Verb __instance, bool __result, bool __state)
            {
                if (__state && __result && __instance.caster != null)
                {
                    if (__instance.CurrentTarget is { IsValid: true, Thing: Pawn targetPawn } && (__instance.caster?.HostileTo(targetPawn) ?? false))
                    {
                        ThingComp_CombatAI comp = targetPawn.AI();
                        if (comp != null)
                        {
                            //
                            // Log.Message($"{targetPawn} is being targeted by {__instance.caster}");
                            comp.Notify_BeingTargeted(__instance.caster, __instance);
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch]
        private static class Verb_TryCastNextBurstShot_Patch
        {
	        private static MethodBase mFleckMakerStatic = AccessTools.Method(typeof(FleckMaker), nameof(FleckMaker.Static), new []{typeof(IntVec3), typeof(Map), typeof(FleckDef), typeof(float)});
	        
            public static IEnumerable<MethodBase> TargetMethods()
            {
                HashSet<MethodBase> methods = new HashSet<MethodBase>();
                foreach (Type t in typeof(Verb).AllSubclasses())
                {
                    foreach (MethodInfo method in t.GetMethods(AccessTools.all))
                    {
                        if (method != null && !methods.Contains(method) && !method.IsStatic && method.Name.Contains("TryCastNextBurstShot") && !method.IsAbstract && method.HasMethodBody() && method.DeclaringType == t)
                        {
                            Log.Message($"ISMA: TryCastNextBurstShot Patched verb type {t.FullName}:{method.Name}");
                            methods.Add(method);
                        }
                    }
                }
                foreach (MethodInfo method in typeof(Verb).GetMethods(AccessTools.all))
                {
                    if (method != null && !methods.Contains(method) && !method.IsStatic && method.Name.Contains("TryCastNextBurstShot") && !method.IsAbstract && method.HasMethodBody() && method.DeclaringType == typeof(Verb))
                    {
                        Log.Message($"ISMA: TryCastNextBurstShot Patched verb type {typeof(Verb).FullName}:{method.Name}");
                        methods.Add(method);
                    }
                }
                return methods;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
	            List<CodeInstruction> codes    = instructions.ToList();
	            bool                  finished = false;
	            for (int i = 0; i < codes.Count; i++)
	            {
		            yield return codes[i];
		            if (!finished)
		            {
			            if (codes[i].opcode == OpCodes.Call && codes[i].OperandIs(mFleckMakerStatic))
			            {
				            finished = true;
				            yield return new CodeInstruction(OpCodes.Ldarg_0);
				            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Verb_TryCastNextBurstShot_Patch), nameof(Verb_TryCastNextBurstShot_Patch.OnShot)));
			            }
		            }
	            }
            }
            

            public static void OnShot(Verb verb)
            {
	            if (verb.verbProps.muzzleFlashScale > 0)
	            {
		            Map map = verb.caster.Map;
		            if (map != null)
		            {
			            MapComponent_FogGrid grid = map.GetComp_Fast<MapComponent_FogGrid>();
			            if (grid != null)
			            {
				            grid.RevealSpot(verb.caster.Position, Maths.Min(verb.verbProps.muzzleFlashScale, 8f), Rand.Range(120, 360));
			            }
		            }
	            }
            }
        }
    }
}
