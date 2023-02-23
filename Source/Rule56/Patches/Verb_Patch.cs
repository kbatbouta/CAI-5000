using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using System;
using CombatAI.Comps;
using RimWorld;
using UnityEngine;
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
                    foreach (var method in t.GetMethods(AccessTools.all))
                    {
                        if (method != null && !methods.Contains(method) &&  !method.IsStatic && method.ReturnType == typeof(bool) && (method.Name.Contains("TryStartCastOn") || method.Name.Contains("TryCastShot"))  && !method.IsAbstract && method.HasMethodBody() && method.DeclaringType == t)
                        {
                            Log.Message($"ISMA: Patched verb type {t.FullName}:{method.Name}");
                            methods.Add(method);
                        }
                    }
                }
                foreach (var method in typeof(Verb).GetMethods(AccessTools.all))
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
                if (__state = (__instance != callerVerb))
                {
                    callerVerb    = __instance;
                }
            }

            public static void Postfix(Verb __instance, bool __result, bool __state)
            {
                if (__state && __result && __instance.caster != null)
                {
                    if (__instance.CurrentTarget is { IsValid: true, Thing: Pawn targetPawn } && (__instance.caster?.HostileTo(targetPawn) ?? false))
                    {
                        ThingComp_CombatAI comp = targetPawn.GetComp_Fast<ThingComp_CombatAI>();
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
    }
}
