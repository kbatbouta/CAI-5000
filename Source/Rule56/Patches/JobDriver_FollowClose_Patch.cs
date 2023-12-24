using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI.Patches
{
	public static class JobDriver_FollowClose_Patch
	{
//		[HarmonyPatch(typeof(JobDriver_FollowClose), nameof(JobDriver_FollowClose.NearFollowee))]
//		private static class JobDriver_FollowClose_NearFollowee_Patch
//		{
//			public static bool Prefix(Pawn follower, Pawn followee, float radius, ref bool __result)
//			{
//				if (follower.mindState != null && (follower.mindState.duty.Is(CombatAI_DutyDefOf.CombatAI_Escort) || follower.mindState.duty.Is(DutyDefOf.Escort)))
//				{
//					__result = NearFollowee(follower, followee, follower.Position, radius);
//					return false;
//				}
//				return true;
//			}
//		}	
//		
//		public static bool NearFollowee(Pawn follower, Pawn followee, IntVec3 locus, float radius)
//		{
////			if (radius <= 15)
////			{
////				ThingComp_CombatAI comp = follower.AI();
////				if (comp?.data != null)
////				{
////					if (!comp.data.AllAllies.Contains(followee))
////					{
////						return false;
////					}
////				}
////			}
//			float   speedMul       = Mathf.Clamp(followee.GetStatValue_Fast(StatDefOf.MoveSpeed, 3600) / (follower.GetStatValue_Fast(StatDefOf.MoveSpeed, 3600) + 0.1f), 0.5f, 2);
//			float   adjustedRadius = radius * 1.2f;
//			IntVec3 shiftedPos     = PawnPathUtility.GetMovingShiftedPosition(followee, 60 * speedMul * 3);
//			if (shiftedPos.HeuristicDistanceTo(locus, followee.Map, Mathf.CeilToInt(radius / 12f + 2)) >= adjustedRadius)
//			{
//				return false;
//			}
//			return true;
//		}
	}
}
