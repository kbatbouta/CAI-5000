using CombatAI.Comps;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public class ThinkNode_ConditionalReacted : ThinkNode_Conditional
    {
        private bool fallback = true;
#pragma warning disable CS0649
        private int ticks;
#pragma warning restore CS0649

        public override bool Satisfied(Pawn pawn)
        {
            ThingComp_CombatAI comp = pawn.GetComp_Fast<ThingComp_CombatAI>();
            if (comp != null && comp.data != null)
            {
                return comp.data.InterruptedRecently(ticks) || comp.data.RetreatedRecently(ticks);
            }
            return fallback;
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalReacted obj = (ThinkNode_ConditionalReacted)base.DeepCopy(resolve);
            obj.ticks    = ticks;
            obj.fallback = fallback;
            return obj;
        }
    }
}
