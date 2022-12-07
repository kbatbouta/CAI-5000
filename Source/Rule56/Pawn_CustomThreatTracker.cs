using System;
using System.Collections.Generic;
using Steamworks;
using Verse;

namespace CombatAI
{
	public class Pawn_CustomThreatTracker
	{
		public class CustomThreatVector
		{
			private int   oldCount;
			private float curDistSqr;
			private float oldDistSqr;

			public readonly List<Thing> things = new List<Thing>(64);
			public readonly Thing       parent;

			public int NewNum
			{
				get => Maths.Max(things.Count - oldCount, 0);
			}

			public int CurNum
			{
				get => things.Count;
			}

			public float CurDistSqr
			{
				get => curDistSqr;
			}

			public float OldDistSqr
			{
				get => oldDistSqr;
			}

			public CustomThreatVector(Thing parent)
			{
				this.parent = parent;
			}

			public void Push(Thing thing)
			{
				things.Add(thing);
				curDistSqr = Maths.Min(thing.Position.DistanceToSquared(thing.Position), curDistSqr);
			}

			public void Clear()
			{
				oldDistSqr = curDistSqr;
				curDistSqr = 1e6f;
				oldCount   = things.Count;
				things.Clear();
			}
		}

		public Pawn                 pawn;
		public CustomThreatVector[] threatVectors = new CustomThreatVector[4];

		public Pawn_CustomThreatTracker(Pawn pawn)
		{
			this.pawn = pawn;
		}
	}
}
