using System.Collections.Generic;
using Verse;
namespace CombatAI
{
	public class Pawn_CustomThreatTracker
	{

		public Pawn                 pawn;
		public CustomThreatVector[] threatVectors = new CustomThreatVector[4];

		public Pawn_CustomThreatTracker(Pawn pawn)
		{
			this.pawn = pawn;
		}

		public class CustomThreatVector
		{
			public readonly Thing parent;

			public readonly List<Thing> things = new List<Thing>(64);
			private         int         oldCount;

			public CustomThreatVector(Thing parent)
			{
				this.parent = parent;
			}

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
				get;
				private set;
			}

			public float OldDistSqr
			{
				get;
				private set;
			}

			public void Push(Thing thing)
			{
				things.Add(thing);
				CurDistSqr = Maths.Min(thing.Position.DistanceToSquared(thing.Position), CurDistSqr);
			}

			public void Clear()
			{
				OldDistSqr = CurDistSqr;
				CurDistSqr = 1e6f;
				oldCount   = things.Count;
				things.Clear();
			}
		}
	}
}
