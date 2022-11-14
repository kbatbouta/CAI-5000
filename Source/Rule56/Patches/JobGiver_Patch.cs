using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;
using System.Reflection;

namespace CombatAI.Patches
{    
    public static class JobGiver_Patch
    {
        //public static readonly Dictionary<int, string> jobGiverByJob = new Dictionary<int, string>();
        //
        //[HarmonyPatch]
        //static class JobGiver_TryGiveJob_Patch
        //{       
        //    public static IEnumerable<MethodBase> TargetMethods()
        //    {
        //        foreach(Type t in typeof(ThinkNode_JobGiver).AllSubclassesNonAbstract())
        //        {
        //            MethodInfo m = AccessTools.Method(t, "TryGiveJob");
        //            if (m != null && m.HasMethodBody() && m.DeclaringType == m.ReflectedType)
        //            {                        
        //                yield return m;
        //            }
        //        }                
        //    }
        //
        //    public static void Postfix(ThinkNode_JobGiver __instance, Job __result, Pawn pawn)
        //    {
        //        if (Finder.Settings.Debug && __result != null)
        //        {                    
        //            //jobGiverByJob[__result.loadID] = __instance.GetType().Name;
        //            if (Find.Selector.SelectedPawns.Contains(pawn))
        //            {
        //                Log.Message($"{pawn}\tjob\t{__result.def}\tfrom\t{__instance.GetType().Name}");
        //            }
        //        }
        //    }
        //}        
    }
}

