using System;
using System.Collections.Generic;
using Verse;

namespace CombatAI
{
	public struct BattleRoyaleParms
	{		
		public int battleId;
		public int maxRoundTicks;
		public int runs;
		public List<PawnKindDef> lhs;
		public AIType lhsAi;
		public List<PawnKindDef> rhs;
		public AIType rhsAi;
		public BattleSpawn spawn;
		public Action<BattleResult, List<Pawn>, List<Pawn>> callback;	

		public bool IsValid
		{
			get
			{
				return lhs != null && rhs != null && runs > 0;
			}
		}
	}
}

