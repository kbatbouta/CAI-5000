using System.Runtime.CompilerServices;
using CombatAI.Comps;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public static class CustomDutyUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pawn_CustomDutyTracker GetPawnCustomDutyTracker(this Pawn pawn)
        {
            return pawn.AI()?.duties ?? null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyCustomDuties(this Pawn pawn, DutyDef def)
        {
            return pawn.GetPawnCustomDutyTracker()?.Any(def) ?? false;
        }

        public static bool TryStartCustomDuty(this Pawn pawn, Pawn_CustomDutyTracker.CustomPawnDuty duty, bool returnCurDutyToQueue = true)
        {
            ThingComp_CombatAI comp = pawn.AI();
            if (comp == null)
            {
                return false;
            }
            comp.duties.StartDuty(duty, returnCurDutyToQueue);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnqueueFirstCustomDuty(this Pawn pawn, Pawn_CustomDutyTracker.CustomPawnDuty duty)
        {
            ThingComp_CombatAI comp = pawn.AI();
            if (comp?.duties == null)
            {
                comp.duties.EnqueueFirst(duty);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnqueueCustomDuty(this Pawn pawn, Pawn_CustomDutyTracker.CustomPawnDuty duty)
        {
            ThingComp_CombatAI comp = pawn.AI();
            if (comp?.duties == null)
            {
                comp.duties.Enqueue(duty);
            }
        }

        public static Pawn_CustomDutyTracker.CustomPawnDuty Escort(Pawn escortee, int radius = -1, int failOnDist = 0, int expireAfter = 0, int startAfter = 0, bool failOnFocusDowned = true, DutyDef failOnFocusDutyNot = null)
        {
            Pawn_CustomDutyTracker.CustomPawnDuty custom = new Pawn_CustomDutyTracker.CustomPawnDuty
            {
                duty = new PawnDuty(CombatAI_DutyDefOf.CombatAI_Escort, escortee, radius)
                {
                    locomotion = LocomotionUrgency.Sprint
                },
                endOnDistToFocusLarger = failOnDist,
                expireAfter            = expireAfter,
                startAfter             = startAfter,
                endOnFocusDowned       = failOnFocusDowned,
                endOnFocusDutyNot      = failOnFocusDutyNot,
                endOnFocusDestroyed    = true,
                endOnFocusDeath        = true
            };
            return custom;
        }

        public static Pawn_CustomDutyTracker.CustomPawnDuty AssaultPoint(IntVec3 dest, int switchAssaultRadius = 15, int expireAfter = 0, int startAfter = 0)
        {
            Pawn_CustomDutyTracker.CustomPawnDuty custom = new Pawn_CustomDutyTracker.CustomPawnDuty
            {
                duty = new PawnDuty(CombatAI_DutyDefOf.CombatAI_AssaultPoint, dest, switchAssaultRadius)
                {
                    locomotion = LocomotionUrgency.Sprint
                },
                endOnDistToFocusLess = switchAssaultRadius,
                expireAfter          = expireAfter,
                startAfter           = startAfter
            };
            return custom;
        }

        public static Pawn_CustomDutyTracker.CustomPawnDuty DefendPoint(IntVec3 dest, int radius, bool endOnTookDamage, int expireAfter, int startAfter = 0)
        {
            Pawn_CustomDutyTracker.CustomPawnDuty custom = new Pawn_CustomDutyTracker.CustomPawnDuty
            {
                duty = new PawnDuty(DutyDefOf.Defend, dest, radius)
                {
                    locomotion = LocomotionUrgency.Sprint
                },
                expireAfter     = expireAfter,
                startAfter      = startAfter,
                endOnTookDamage = endOnTookDamage
            };
            return custom;
        }

        public static Pawn_CustomDutyTracker.CustomPawnDuty HuntDownEnemies(LocalTargetInfo enemy, IntVec3 fallbackPosition, int maxDist, int expireAfter = 0, int startAfter = 0)
        {
            PawnDuty duty = new PawnDuty(DutyDefOf.HuntEnemiesIndividual)
            {
                locomotion  = LocomotionUrgency.Sprint,
                focus       = enemy,
                focusSecond = fallbackPosition
            };
            Pawn_CustomDutyTracker.CustomPawnDuty custom = new Pawn_CustomDutyTracker.CustomPawnDuty
            {
                duty                   = duty,
                expireAfter            = expireAfter,
                startAfter             = startAfter,
                endOnFocusDowned       = true,
                endOnDistToFocusLarger = maxDist
            };
            return custom;
        }

        public static Pawn_CustomDutyTracker.CustomPawnDuty HuntDownEnemies(IntVec3 fallbackPosition, int expireAfter = 0, int startAfter = 0)
        {
            PawnDuty duty = new PawnDuty(DutyDefOf.HuntEnemiesIndividual)
            {
                locomotion  = LocomotionUrgency.Sprint,
                focusSecond = fallbackPosition
            };
            Pawn_CustomDutyTracker.CustomPawnDuty custom = new Pawn_CustomDutyTracker.CustomPawnDuty
            {
                duty        = duty,
                expireAfter = expireAfter,
                startAfter  = startAfter
            };
            return custom;
        }
    }
}
