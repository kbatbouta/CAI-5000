using System;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI
{
	public class JobGiver_CAIFollowEscortee : JobGiver_AIFollowEscortee
	{
		public override Job TryGiveJob(Pawn pawn)
		{
			LocalTargetInfo focus = pawn.mindState.duty?.focus ?? LocalTargetInfo.Invalid;
			if (focus is { IsValid: true, HasThing: true, Thing: Pawn followee } 
			    && !pawn.CurJobDef.Is(JobDefOf.Wait_Combat) 
			    && !NearFollowee(pawn, followee, pawn.mindState.duty.radius, out IntVec3 root)
			    && pawn.TryGetSightReader(out SightTracker.SightReader reader))
			{
				Map                   map           = pawn.Map;
				MapComponent_CombatAI comp          = map.AI();
				IntVec3               bestCell      = IntVec3.Invalid;
				IntVec3               pawnPos       = pawn.Position;
				float                 bestCellScore = float.MaxValue;
				float                 radius        = pawn.mindState.duty.radius * 0.8f;
				Action<CellFlooder.Node> action = (node) =>
				{
					if (node.distAbs > radius)
					{
						return;
					}
					float score = reader.GetEnemyAvailability(node.cell) + reader.GetEnemyAvailability(node.cell);
					if (!pawn.CanReserve(node.cell) || node.cell.GetEdifice(map) != null)
					{
						score += 16f;
					}
					if (bestCellScore > score)
					{
						bestCellScore = score;
						bestCell      = node.cell;
					}
				};
				comp.flooder.Flood(root, action, maxDist: Mathf.CeilToInt(Maths.Max(radius, Mathf.Abs(pawnPos.x - root.x) + Mathf.Abs(pawnPos.z - root.z))));
				if (bestCell.IsValid && pawn.CanReach(bestCell, PathEndMode.ClosestTouch, Danger.Deadly, true, true, TraverseMode.PassDoors))
				{
					Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, bestCell);
					job_goto.expiryInterval        = 30;
					job_goto.locomotionUrgency     = Finder.Settings.Enable_Sprinting ? LocomotionUrgency.Sprint : LocomotionUrgency.Jog;
					job_goto.checkOverrideOnExpire = true;
					return job_goto;
				}
				return base.TryGiveJob(pawn);
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NearFollowee(Pawn follower, Pawn followee, float radius, out IntVec3 dest)
		{
			return NearFollowee(follower, followee, follower.Position, radius, out dest);
		}
		
		public static bool NearFollowee(Pawn follower, Pawn followee, IntVec3 locus, float radius, out IntVec3 followeeRoot)
		{
			float   speedMul       = Mathf.Clamp(followee.GetStatValue_Fast(StatDefOf.MoveSpeed, 3600) / (follower.GetStatValue_Fast(StatDefOf.MoveSpeed, 3600) + 0.1f), 0.5f, 2);
			float   adjustedRadius = radius * 1.2f; 
			IntVec3 shiftedPos     = followeeRoot = PawnPathUtility.GetMovingShiftedPosition(followee, 60 * speedMul * 6);
			if (shiftedPos.HeuristicDistanceTo(locus, followee.Map, Mathf.CeilToInt(radius / 12f + 2)) >= adjustedRadius)
			{
				return false;
			}
			return true;
		}
	}
}
