using System.Collections.Generic;
using System.Linq;
using CombatAI.Comps;
using Verse;
using Verse.AI;
namespace CombatAI
{
	public static class JobLogUtility
	{
		public static JobLog LogFor(this Pawn pawn, Job job, string tag = null)
		{
			ThingComp_CombatAI comp = pawn.AI();
			JobLog             log  = comp.jobLogs?.FirstOrDefault(j => j.id == job.loadID) ?? null;
			if (log == null)
			{
				log = JobLog.For(pawn, job, tag);
				comp.jobLogs ??= new List<JobLog>();
				comp.jobLogs.Add(log);
			}
			return log;
		}
	}
}
