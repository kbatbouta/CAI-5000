using System.Collections.Generic;
using CombatAI.Comps;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public class JobGiver_DuckOrRetreat : ThinkNode_JobGiver
    {
        private int radius = 8;

        public override Job TryGiveJob(Pawn pawn)
        {
            Verb verb = pawn.TryGetAttackVerb();
            if (verb == null || !verb.IsMeleeAttack)
            {
                ThingComp_CombatAI comp = pawn.AI();
                List<Thing>        list = comp.data.BeingTargetedBy;
                if (comp != null && !list.NullOrEmpty())
                {
                    CoverPositionRequest request = new CoverPositionRequest();
                    request.caster             = pawn;
                    request.maxRangeFromCaster = radius;
                    request.majorThreats       = list;
                    request.checkBlockChance   = true;
                    if (!CoverPositionFinder.TryFindDuckPosition(request, out IntVec3 cell))
                    {
                        return null;
                    }
                    Job goto_job = JobMaker.MakeJob(CombatAI_JobDefOf.CombatAI_Goto_Retreat, cell);
                    return goto_job;
                }
            }
            return null;
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_DuckOrRetreat node = (JobGiver_DuckOrRetreat)base.DeepCopy(resolve);
            node.radius = radius;
            return node;
        }
    }
}
