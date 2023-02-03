using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public partial class Pawn_CustomDutyTracker
    {
        public class CustomPawnDuty : IExposable
        {
            public bool     finished;
            public int      startAfter;
            public int      startsAt   = -1;
            public bool     canExitMap = true;
            public bool     canFlee    = true;
            public int      expireAfter;
            public int      expiresAt = -1;
            public int      endOnDistToFocusLess;
            public int      endOnDistToFocusLarger;
            public bool     endOnFocusDeath;
            public bool     endOnFocusDestroyed;
            public bool     endOnFocusDowned;
            public bool     endOnTookDamage;
            public PawnDuty duty;
            public DutyDef  endOnFocusDutyNot;

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
                Scribe_Values.Look(ref endOnTookDamage, "endOnTookDamage", false);
                Scribe_Values.Look(ref canExitMap, "canExitMap", true);
                Scribe_Defs.Look(ref endOnFocusDutyNot, "failOnFocusDutyNot");
            }
        }
    }
}
