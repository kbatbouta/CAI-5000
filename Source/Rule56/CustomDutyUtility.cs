using System;
using Verse.AI;
using Verse;
using RimWorld;
using CombatAI.Comps;

namespace CombatAI
{
	public static class CustomDutyUtility
	{
		public static Pawn_CustomDutyTracker.CustomPawnDuty Escort(Pawn escorter, Pawn escortee, int radius = -1,
			int failOnDist = 0, int expireAfter = 0, int startAfter = 0, bool failOnFocusDowned = true,
			DutyDef failOnFocusDutyNot = null)
		{
			if (!escorter.CanReach(escortee, PathEndMode.InteractionCell, Danger.Unspecified)) return null;
			var compee = escortee.GetComp_Fast<ThingComp_CombatAI>();
			if (compee == null) return null;
			var tracker = compee.duties;
			if (tracker == null) return null;
			var custom = new Pawn_CustomDutyTracker.CustomPawnDuty();
			custom.duty = new PawnDuty(DutyDefOf.Escort, escortee, radius);
			custom.duty.locomotion = LocomotionUrgency.Sprint;
			custom.failOnDistanceToFocus = failOnDist;
			custom.expireAfter = expireAfter;
			custom.startAfter = startAfter;
			custom.failOnFocusDowned = failOnFocusDowned;
			custom.failOnFocusDutyNot = failOnFocusDutyNot;
			custom.failOnFocusDestroyed = true;
			custom.failOnFocusDeath = true;
			return custom;
		}
	}
}