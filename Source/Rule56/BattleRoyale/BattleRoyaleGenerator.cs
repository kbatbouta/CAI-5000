using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatAI
{
	public class BattleRoyaleGenerator
	{
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
			parms.maxRoundTicks = 60 * 120;
			parms.runs = BattleRoyale.roundsPerPair;
			parms.spawn = BattleRoyale.spawn;	
			PawnGroupMaker groupMaker = groupMakers.RandomElement();
			int points = Rand.Range(100, 1500);			
			parms.lhs = GeneratePawnKindDefGroup(groupMaker, points).ToList();
			parms.rhs = GeneratePawnKindDefGroup(groupMaker, points).ToList();
			parms.callback = (res, lshPawns, rshPawns) =>
			{
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

