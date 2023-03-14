using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public class JobLog
    {
        private static readonly StringBuilder builder = new StringBuilder();
        public                  IntVec3       destination;
        public                  string        duty;
        public                  int           id;
        public                  string        job;
        public                  string        note = string.Empty;
        public                  IntVec3       origin;
        public                  List<string>  stacktrace;
        public                  List<string>  thinknode;
        public                  List<IntVec3> path;
        public                  List<IntVec3> pathSapper;

        public int timestamp;

        private JobLog()
        {
            if (!Finder.Settings.Debug || !Finder.Settings.Debug_LogJobs)
            {
                Log.ErrorOnce("ISMA: Job logging active when it shouldn't! Disabling job logging.", 10949);
                Finder.Settings.Debug_LogJobs = false;
            }
        }

        public bool IsValid
        {
            get => true;
        }

        public static JobLog For(Pawn pawn, Job job, ThinkNode jobGiver)
        {
            JobLog log = new JobLog();
            log.job         = job.def.defName;
            log.origin      = pawn.Position;
            log.id          = job.loadID;
            log.destination = job.targetA.IsValid ? job.targetA.Cell : IntVec3.Invalid;
            log.duty        = pawn.mindState.duty?.def.defName ?? "none";
            // fill thinknode trace
            log.thinknode = new List<string>();
            if (jobGiver != null)
            {
                ThinkNodeDatabase.GetTrace(jobGiver, pawn, log.thinknode);
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
            log.thinknode = new List<string>
            {
                jobGiverTag
            };
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

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("job:\t{0} ({1})\n", job, id);
            builder.AppendFormat("duty:\t{0}\n", duty);
            builder.AppendFormat("timestamp:\t{0}\n", timestamp);
            builder.AppendFormat("(origin:{0}, dest:{1})\n", origin, destination);
            builder.AppendFormat($"note: {note}\n");
            if (destination.IsValid)
            {
                builder.AppendFormat("distanceToDest:\n{0}",destination.DistanceTo(origin));
            }
            builder.AppendLine();
            builder.Append("thinknode trace:\n");
            for (int i = 0; i < thinknode.Count; i++)
            {
                builder.AppendFormat("  {0}. {1}\n", i + 1, thinknode[i]);
            }
            builder.AppendLine();
            builder.Append("stacktrace:\n");
            for (int i = 0; i < stacktrace.Count; i++)
            {
                builder.AppendFormat("  {0}. {1}\n", i + 1, stacktrace[i]);
            }
            return builder.ToString();
        }
    }
}
