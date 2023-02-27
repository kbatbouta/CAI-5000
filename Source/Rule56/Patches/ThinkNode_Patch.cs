using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using CombatAI.Comps;
using HarmonyLib;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
    public class ThinkNode_Patch
    {
        private static          Pawn         curPawn;
        private static readonly HashSet<Job> processedJobs = new HashSet<Job>();

        private static bool Enabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Finder.Settings.Debug && Finder.Settings.Debug_LogJobs;
        }

        [HarmonyPatch]
        private static class ThinkNode_TryIssueJobPackage_Patch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                HashSet<MethodBase> methods = new HashSet<MethodBase>();
                foreach (Type t in typeof(ThinkNode).AllSubclasses())
                {
                    foreach (MethodInfo method in t.GetMethods(AccessTools.all))
                    {
                        if (method != null && !methods.Contains(method) && method.HasMethodBody() && !method.IsAbstract && method.ReturnType == typeof(ThinkResult) && method.Name == "TryIssueJobPackage" && method.DeclaringType == t)
                        {
                            if (Finder.Settings.Debug)
                            {
                                Log.Message($"ISMA: Patched thinknode type {t.FullName}:{method.Name}");
                            }
                            methods.Add(method);
                        }
                    }
                }
                return methods;
            }

            public static void Postfix(ThinkNode __instance, ThinkResult __result, Pawn pawn)
            {
                if (Enabled && __result is { IsValid: true, Job: { } } && !processedJobs.Contains(__result.Job))
                {
                    if (curPawn != pawn)
                    {
                        curPawn = pawn;
                        processedJobs.Clear();
                    }
                    processedJobs.Add(__result.Job);
                    JobLog log = JobLog.For(pawn, __result.Job, __instance);
                    if (log.IsValid)
                    {
                        ThingComp_CombatAI comp = pawn.AI();
                        if (comp != null)
                        {
                            comp.jobLogs ??= new List<JobLog>();
                            comp.jobLogs.Insert(0, log);
                            while (comp.jobLogs.Count > 64)
                            {
                                // limit size to 32
                                comp.jobLogs.RemoveAt(comp.jobLogs.Count - 1);
                            }
                        }
                    }
                }
            }
        }
    }
}
