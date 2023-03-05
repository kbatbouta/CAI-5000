using System.Collections.Generic;
using Verse;
namespace CombatAI
{
	public abstract class IPawnGroupPlan : IPawnGroup
	{
		public abstract bool InProgress
		{
			get;
		}

		public override bool IsValid
		{
			get => base.IsValid && InProgress;
		}
	}
}
