using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class Building_Turret_Patch
    {
        private static List<Type>       types;
        private static List<MethodInfo> methods;

        [HarmonyPatch]
        private static class Building_Turret_SpawnSetup_Patch
        {
            public static bool Prepare()
            {
                if (methods != null)
                {
                    return methods.Count != 0;
                }
                methods = new List<MethodInfo>();
                foreach (Type t in types ?? (types = typeof(Building_TurretGun).AllSubclassesNonAbstract().ToList()))
                {
                    if (typeof(Building_TurretGun).IsAssignableFrom(t))
                    {
                        continue;
                    }
                    MethodInfo method = AccessTools.Method(t, nameof(Building_Turret.SpawnSetup));
                    if (method != null && method.DeclaringType == t && method.HasMethodBody())
                    {
                        methods.Add(method);
                    }
                }
                return methods.Count != 0;
            }

            public static IEnumerable<MethodBase> TargetMethods()
            {
                return methods;
            }

            public static void Postfix(Building_TurretGun __instance)
            {
                __instance.Map.GetComp_Fast<TurretTracker>().Register(__instance);
            }
        }

        [HarmonyPatch]
        private static class Building_Turret_DeSpawn_Patch
        {
            public static bool Prepare()
            {
                if (methods != null)
                {
                    return methods.Count != 0;
                }
                foreach (Type t in types ?? (types = typeof(Building_TurretGun).AllSubclassesNonAbstract().ToList()))
                {
                    if (typeof(Building_TurretGun).IsAssignableFrom(t))
                    {
                        continue;
                    }
                    MethodInfo method = AccessTools.Method(t, nameof(Building_Turret.SpawnSetup));
                    if (method != null && method.DeclaringType == t && method.HasMethodBody())
                    {
                        methods.Add(method);
                    }
                }
                return methods.Count != 0;
            }

            public static IEnumerable<MethodBase> TargetMethods()
            {
                return methods;
            }

            public static void Prefix(Building_TurretGun __instance)
            {
                __instance.Map.GetComp_Fast<TurretTracker>().DeRegister(__instance);
            }
        }
    }
}
