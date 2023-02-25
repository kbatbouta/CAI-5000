using Verse;
using Verse.AI;
namespace CombatAI
{
    public partial class Pawn_CustomDutyTracker
    {
        public class CustomPawnDuty : IExposable
        {
            public bool     canExitMap = true;
            public bool     canFlee    = true;
            public PawnDuty duty;
            public int      endOnDistToFocusLarger;
            public int      endOnDistToFocusLess;
            public bool     endOnFocusDeath;
            public bool     endOnFocusDestroyed;
            public bool     endOnFocusDowned;
            public DutyDef  endOnFocusDutyNot;
            public bool     endOnTookDamage;
            public int      expireAfter;
            public int      expiresAt = -1;
            public bool     finished;
            public int      startAfter;
            public int      startsAt = -1;

            public void ExposeData()
            {
                Scribe_Deep.Look(ref duty, "duty");
                Scribe_Values.Look(ref finished, "finished");
                Scribe_Values.Look(ref expireAfter, "expireAfter");
                Scribe_Values.Look(ref startAfter, "startAfter");
                Scribe_Values.Look(ref startsAt, "startsAt", -1);
                Scribe_Values.Look(ref expiresAt, "expiresAt", -1);
                Scribe_Values.Look(ref endOnDistToFocusLarger, "failOnDistanceToFocus");
                Scribe_Values.Look(ref endOnDistToFocusLess, "endOnDistanceToFocus");
                Scribe_Values.Look(ref endOnFocusDeath, "failOnFocusDeath");
                Scribe_Values.Look(ref endOnFocusDowned, "failOnFocusDowned");
                Scribe_Values.Look(ref endOnFocusDestroyed, "failOnFocusDestroyed");
                Scribe_Values.Look(ref canFlee, "canFlee", true);
                Scribe_Values.Look(ref endOnTookDamage, "endOnTookDamage");
                Scribe_Values.Look(ref canExitMap, "canExitMap", true);
                Scribe_Defs.Look(ref endOnFocusDutyNot, "failOnFocusDutyNot");
            }
        }
    }
}
