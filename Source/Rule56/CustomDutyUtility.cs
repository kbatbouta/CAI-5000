using CombatAI.Comps;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI
{
	public static class CustomDutyUtility
	{
		public static Pawn_CustomDutyTracker.CustomPawnDuty Escort(Pawn escorter, Pawn escortee, int radius = -1, int failOnDist = 0, int expireAfter = 0, int startAfter = 0, bool failOnFocusDowned = true, DutyDef failOnFocusDutyNot = null)
		{
			if (!escorter.CanReach(escortee, PathEndMode.InteractionCell, Danger.Unspecified))
			{
				return null;
			}
			ThingComp_CombatAI compee = escortee.GetComp_Fast<ThingComp_CombatAI>();
			if (compee == null)
			{
				return null;
			}
			Pawn_CustomDutyTracker tracker = compee.duties;
			if (tracker == null)
			{
				return null;
			}
			Pawn_CustomDutyTracker.CustomPawnDuty custom = new Pawn_CustomDutyTracker.CustomPawnDuty();
			custom.duty                  = new PawnDuty(DutyDefOf.Escort, escortee, radius);
			custom.duty.locomotion       = LocomotionUrgency.Sprint;
			custom.failOnDistanceToFocus = failOnDist;
			custom.expireAfter           = expireAfter;
			custom.startAfter            = startAfter;
			custom.failOnFocusDowned     = failOnFocusDowned;
			custom.failOnFocusDutyNot    = failOnFocusDutyNot;
			custom.failOnFocusDestroyed  = true;
			custom.failOnFocusDeath      = true;
			return custom;
		}

		public static Pawn_CustomDutyTracker.CustomPawnDuty AssaultPoint(Pawn pawn, IntVec3 dest, int switchAssaultRadius = 15, int expireAfter = 0, int startAfter = 0)
		{
			//if (!pawn.CanReach(dest, PathEndMode.InteractionCell, Danger.Unspecified, true, true, TraverseMode.PassAllDestroyableThingsNotWater))
			//{
			//	return null;
			//}
			ThingComp_CombatAI comp = pawn.GetComp_Fast<ThingComp_CombatAI>();
			if (comp == null)
			{
				return null;
			}
			Pawn_CustomDutyTracker tracker = comp.duties;
			if (tracker == null)
			{
				return null;
			}
			Pawn_CustomDutyTracker.CustomPawnDuty custom = new Pawn_CustomDutyTracker.CustomPawnDuty();
			custom.duty                 = new PawnDuty(DutyDefOf.Defend, dest, switchAssaultRadius);
			custom.duty.locomotion      = LocomotionUrgency.Sprint;
			custom.dest                 = dest;
			custom.endOnDistanceToFocus = switchAssaultRadius;
			custom.expireAfter          = expireAfter;
			custom.startAfter           = startAfter;
			custom.failOnFocusDestroyed = false;
			custom.failOnFocusDeath     = false;
			return custom;
		}
	}
}
