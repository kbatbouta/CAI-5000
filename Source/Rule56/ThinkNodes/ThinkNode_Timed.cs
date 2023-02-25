using Verse;
using Verse.AI;
namespace CombatAI
{
    public class ThinkNode_Timed : ThinkNode_Conditional
    {
        private int interval;

        public override bool Satisfied(Pawn pawn)
        {
            if (!pawn.mindState.thinkData.TryGetValue(UniqueSaveKey, out int val) || GenTicks.TicksGame - val > interval)
            {
                pawn.mindState.thinkData[UniqueSaveKey] = GenTicks.TicksGame;
                return true;
            }
            return false;
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_Timed obj = (ThinkNode_Timed)base.DeepCopy(resolve);
            obj.interval = interval;
            return obj;
        }
    }
}
