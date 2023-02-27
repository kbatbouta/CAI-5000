using CombatAI.Comps;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public class ThinkNode_ConditionalSafe : ThinkNode_Conditional
    {
        private bool fallback = true;

        public override bool Satisfied(Pawn pawn)
        {
            ThingComp_CombatAI comp = pawn.AI();
            if (comp != null && pawn.TryGetSightReader(out SightTracker.SightReader reader))
            {
                return comp.data.NumEnemies == 0 && reader.GetAbsVisibilityToEnemies(pawn.Position) == 0;
            }
            return fallback;
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalSafe obj = (ThinkNode_ConditionalSafe)base.DeepCopy(resolve);
            obj.fallback = fallback;
            return obj;
        }
    }
}
