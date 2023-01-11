using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatAI
{
	public class BattleRoyaleGenerator
	{		
		public int score_legacy  = 0;
		public int score_vanilla = 0;
		public int score_stalemate = 0;

		private List<PawnGroupMaker> groupMakers = new List<PawnGroupMaker>();		

		public BattleRoyaleGenerator()
		{
			List<FactionDef> factions = new List<FactionDef>();
			factions.Add(FactionDefOf.OutlanderCivil);
			factions.Add(FactionDefOf.Pirate);
			if (ModsConfig.RoyaltyActive)
			{
				factions.Add(FactionDefOf.Empire);
			}
			foreach(FactionDef def in factions)
			{				
				PawnGroupMaker groupMaker = null;
				foreach(PawnGroupMaker gm in def.pawnGroupMakers)
				{
					if(gm.kindDef == PawnGroupKindDefOf.Combat)
					{
						groupMaker = gm;
						break;
					}
				}
				if (groupMaker != null)
				{
					Log.Message($"Found groupMaker for faction '{def.label}'");
					groupMakers.Add(groupMaker);
				}				
			}
		}

		public void GeneratorUpdate()
		{
				
		}

		public BattleRoyaleParms Next()
		{
			BattleRoyaleParms parms = new BattleRoyaleParms();
			parms.maxRoundTicks = 60 * 180;
			parms.runs = BattleRoyale.roundsPerPair;
			parms.spawn = BattleRoyale.spawn;	
			PawnGroupMaker groupMaker = groupMakers.RandomElement();
			int points = Rand.Range(250, 2500);			
			parms.lhs = GeneratePawnKindDefGroup(groupMaker, points).ToList();
			parms.lhsAi = AIType.legacy;
			parms.rhs = new List<PawnKindDef>(parms.lhs);
			parms.rhsAi = AIType.vanilla;
			float s_lhs = 0;
			float s_rhs = 0;			
			int rounds = 0;
			parms.callback = (res, lshPawns, rshPawns, lr, rr) =>
			{
				if(res == BattleResult.lhs_winner)
				{
					s_lhs += lr - rr; 
				}
				else if (res == BattleResult.rhs_winner)
				{
					s_rhs += rr - lr;
				}
				else
				{
					s_lhs += lr - rr;
					s_rhs += rr - lr;
				}
				rounds++;
				if (parms.runs == rounds)
				{
					if(s_lhs > s_rhs)
					{
						score_legacy++;
					}
					else
					{
						score_vanilla++;
					}
					Log.Error($"Current results:\nvanilla:{score_vanilla}\nlegacy_mod:{score_legacy}\nstalemate:{score_stalemate}");
				}				
			};
			return parms;
		}

		private IEnumerable<PawnKindDef> GeneratePawnKindDefGroup(PawnGroupMaker maker, int points)
		{
			float cost = 0;
			while (cost < points)
			{
				PawnGenOption option = maker.options.RandomElementByWeight(w => w.selectionWeight);
				cost += option.Cost;
				yield return option.kind;
			}
		}
	}
}

