using System;
using Verse;

namespace CombatAI
{
	public static class BattleRoyaleUtility
	{
		public static bool IsRhs(this Pawn pawn)
		{
			return BattleRoyale.enabled && pawn != null && BattleRoyale.rhsPawns.Contains(pawn);
		}

		public static bool IsLhs(this Pawn pawn)
		{
			return BattleRoyale.enabled && pawn != null && BattleRoyale.lhsPawns.Contains(pawn);
		}		
	}
}

