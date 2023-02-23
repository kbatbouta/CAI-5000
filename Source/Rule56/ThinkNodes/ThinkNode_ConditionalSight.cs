using Verse;
using Verse.AI;
namespace CombatAI
{
    public class ThinkNode_ConditionalSight : ThinkNode_Conditional
    {
#pragma warning disable CS0649
        private int  visibilityToEnemies;
#pragma warning restore CS0649
        private bool fallback = true;

        public override bool Satisfied(Pawn pawn)
        {
            if (pawn.TryGetSightReader(out SightTracker.SightReader reader))
            {
                return reader.GetVisibilityToEnemies(pawn.Position) <= visibilityToEnemies;
            }
            return fallback;
        }
        
        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalSight obj = (ThinkNode_ConditionalSight)base.DeepCopy(resolve);
            obj.fallback            = fallback;
            obj.visibilityToEnemies = visibilityToEnemies;
            return obj;
        }
    }
}