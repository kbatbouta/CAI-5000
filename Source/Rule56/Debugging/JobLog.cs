using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public class JobLog
    {
        private readonly static StringBuilder builder = new StringBuilder();
        
        public int          timestamp;
        public string       job;
        public int          id;
        public IntVec3      origin;
        public IntVec3      destination;
        public string       duty;
        public List<string> thinknode;
        public List<string> stacktrace;

        private JobLog()
        {
        }

        public bool IsValid
        {
            get => true;
        }

        public static JobLog For(Pawn pawn, Job job, ThinkNode jobGiver)
        {
            JobLog     log   = new JobLog();
            log.job         = job.def.defName;
            log.origin      = pawn.Position;
            log.id          = job.loadID;
            log.destination = job.targetA.IsValid ? job.targetA.Cell : IntVec3.Invalid;
            log.duty        = pawn.mindState.duty?.def.defName ?? "none";
            // fill thinknode trace
            log.thinknode = new List<string>();
            if (jobGiver != null)
            {
                ThinkNodeDatabase.GetTrace(jobGiver, log.thinknode);
            }
            // reset builder
            StackTrace trace = new StackTrace();
            // fill stacktrace
            log.stacktrace = new List<string>();
            foreach (StackFrame frame in trace.GetFrames())
            {
                MethodBase method = frame.GetMethod();
                Type       type   = method.DeclaringType;
                if (typeof(Root).IsAssignableFrom(type))
                {
                    break;
                }
                log.stacktrace.Add("{0}.{1}:{2}".Formatted(type.Namespace, type.Name, method.Name));
            }
            log.timestamp = GenTicks.TicksGame;
            return log;
        }
        
        public static JobLog For(Pawn pawn, Job job, string jobGiverTag)
        {
            JobLog log = new JobLog();
            log.job         = job.def.defName;
            log.id          = job.loadID;
            log.origin      = pawn.Position;
            log.destination = job.targetA.IsValid ? job.targetA.Cell : IntVec3.Invalid;
            log.duty        = pawn.mindState.duty?.def.defName ?? "none";
            // fill thinknode trace
            log.thinknode = new List<string>() { jobGiverTag };
            // reset builder
            StackTrace trace = new StackTrace();
            // fill stacktrace
            log.stacktrace = new List<string>();
            foreach (StackFrame frame in trace.GetFrames())
            {
                MethodBase method = frame.GetMethod();
                Type       type   = method.DeclaringType;
                if (typeof(Root).IsAssignableFrom(type))
                {
                    break;
                }
                log.stacktrace.Add("{0}.{1}:{2}".Formatted(type.Namespace, type.Name, method.Name));
            }
            log.timestamp = GenTicks.TicksGame;
            return log;
        }
    }
}
