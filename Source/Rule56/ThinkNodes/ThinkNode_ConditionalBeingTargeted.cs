using CombatAI.Comps;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public class ThinkNode_ConditionalBeingTargeted : ThinkNode_Conditional
    {
        private bool fallback = true;

        public override bool Satisfied(Pawn pawn)
        {
            ThingComp_CombatAI comp = pawn.AI();
            if (comp != null)
            {
                return comp.data.BeingTargetedBy.Count > 0;
            }
            return fallback;
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalBeingTargeted obj = (ThinkNode_ConditionalBeingTargeted)base.DeepCopy(resolve);
            obj.fallback = fallback;
            return obj;
        }
    }
}
