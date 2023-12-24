using Verse;
using Verse.AI;
namespace CombatAI
{
	public class JobGiver_CAIWanderNearEscortee : JobGiver_WanderNearDutyLocation
	{
		public override Job TryGiveJob(Pawn pawn)
		{
			return base.TryGiveJob(pawn);
		}
	}
}
