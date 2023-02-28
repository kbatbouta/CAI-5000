using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
    public static class ThingComp_Patch
    {
        [HarmonyPatch]
        private static class ThingComp_PostDraw_Patch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                HashSet<MethodBase> methods = new HashSet<MethodBase>();
                foreach (Type t in typeof(ThingComp).AllSubclasses())
                {
                    MethodInfo method = t.GetMethod(nameof(ThingComp.PostDraw));
                    if(method != null)
                    { 
                        if (method != null && !methods.Contains(method) && !method.IsStatic && !method.IsAbstract && method.HasMethodBody() && method.Name == "PostDraw" && method.DeclaringType == t)
                        {
                            Log.Message($"ISMA: Patched ThingComp type {t.FullName}:{method.Name}"); //                            
                            methods.Add(method);
                        }
                    }
                }
                return methods;
            }

            public static bool Prefix(ThingComp __instance)
            {
                if (Finder.Settings.FogOfWar_Enabled && __instance.parent is Pawn { Spawned: true, Dead: false } pawn)
                {
                    return !(pawn.Map.GetComp_Fast<MapComponent_FogGrid>()?.IsFogged(__instance.parent.Position) ?? false);
                }
                return true;
            }
        }
    }
}
