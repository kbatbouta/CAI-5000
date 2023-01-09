using System;
using System.Collections.Generic;
using Verse;

namespace CombatAI
{
	public static class BattleRoyale
	{
		public static bool enabled;
		public static int roundsPerPair;
		public static BattleSpawn spawn;
		public static BattleRoyaleManager manager;
		public static BattleRoyaleGenerator generator;
		public static readonly HashSet<Pawn> lhsPawns = new HashSet<Pawn>(1024);
		public static readonly HashSet<Pawn> rhsPawns = new HashSet<Pawn>(1024);
	}
}

