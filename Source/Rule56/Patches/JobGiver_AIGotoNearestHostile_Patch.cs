using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatAI.Patches
{
	//public static class JobGiver_AIGotoNearestHostile_Patch
	//{
	//	[HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), nameof(JobGiver_AIGotoNearestHostile.TryGiveJob))]
	//	private static class JobGiver_AIGotoNearestHostile_TryGiveJob_Patch
	//	{
	//		private static Pawn attacker;
	//		private static IntVec3 attackerLoc;
	//		private static DamageReport attackerDamage;
	//		private static ArmorReport attackerArmor;
	//		private static SightTracker.SightReader reader;
	//		private static MethodInfo mIntVec3Utility = AccessTools.Method(typeof(IntVec3Utility), nameof(IntVec3Utility.DistanceToSquared));
	//
	//		public static void Prefix(Pawn pawn)
	//		{
	//			if (pawn != null)
	//			{
	//				attacker = pawn;
	//				attackerArmor = ArmorUtility.GetArmorReport(pawn);
	//				attackerLoc = pawn.Position;
	//				if (!attacker.GetSightReader(out reader) || !(attackerDamage = DamageUtility.GetDamageReport(attacker)).IsValid)
	//				{
	//					attacker = null;
	//					reader = null;
	//					attackerLoc = IntVec3.Invalid;
	//					attackerArmor = default(ArmorReport);
	//					attackerDamage = default(DamageReport);
	//				}
	//			}
	//		}			
	//
	//		public static void Postfix()
	//		{
	//			reader = null;
	//			attacker = null;
	//			attackerLoc = IntVec3.Invalid;
	//			attackerArmor = default(ArmorReport);
	//			attackerDamage = default(DamageReport);
	//		}
	//
	//		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	//		{
	//			List<CodeInstruction> codes = instructions.ToList();
	//			bool finished = false;
	//			for(int i = 0;i < codes.Count; i++)
	//			{
	//				yield return codes[i];
	//				if (!finished)
	//				{
	//					if (codes[i].opcode == OpCodes.Call && codes[i].OperandIs(mIntVec3Utility))
	//					{
	//						finished = true;
	//						yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
	//						yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(JobGiver_AIGotoNearestHostile_TryGiveJob_Patch), nameof(JobGiver_AIGotoNearestHostile_TryGiveJob_Patch.DistanceToSquaredOffset)));
	//						yield return new CodeInstruction(OpCodes.Add);
	//					}
	//				}					
	//			}
	//		}
	//
	//		public static int DistanceToSquaredOffset(int distSqr, Thing enemy)
	//		{
	//			if(attacker == null)
	//			{
	//				return distSqr;
	//			}
	//			IntVec3 targetLoc = enemy.Position;
	//			Vector2 targetDir = new Vector2(targetLoc.x - attackerLoc.x, targetLoc.z - attackerLoc.z);
	//
	//			Vector2 enemiesDir = reader.GetEnemyDirection(enemy.Position);
	//			float distEnemiesSqr = enemiesDir.SqrMagnitude();
	//			if()
	//			if (distSqr > distEnemiesSqr)
	//			{
	//				distSqr = Mathf.CeilToInt(Mathf.CeilToInt(distSqr - 2 * Maths.Max(enemiesDir.x, enemiesDir.y) * Maths.Max(targetDir.x, targetDir.y) + distEnemiesSqr));
	//			}
	//			else if(distSqr < distEnemiesSqr)
	//			{
	//				distSqr = -1 * Mathf.CeilToInt(Mathf.CeilToInt(distSqr - 2 * Maths.Max(enemiesDir.x, enemiesDir.y) * Maths.Max(targetDir.x, targetDir.y) + distEnemiesSqr));
	//			}
	//			else
	//			{
	//				distSqr = 0;
	//			}
	//			DamageReport targetDamage = DamageUtility.GetDamageReport(enemy);
	//			if (distSqr != 0 && targetDamage.IsValid)
	//			{
	//				if (attackerDamage.primaryIsRanged)
	//				{
	//				}
	//				else
	//				{						
	//					if (enemy is Pawn enemyPawn)
	//					{
	//						ArmorReport targetArmor = ArmorUtility.GetArmorReport(enemyPawn);
	//						float threatToTarget = attackerDamage.ThreatTo(targetArmor);
	//						float threatToAttacker = targetDamage.ThreatTo(attackerArmor);
	//						return distSqr * Mathf.Max(threatToAttacker - threatToTarget, 0.25f, 0.1f);
	//					}						
	//				}
	//			}
	//			return distSqr;
	//		}
	//	}
	//}
}