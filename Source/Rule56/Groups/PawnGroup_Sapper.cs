using System.Collections.Generic;
using Verse;
namespace CombatAI
{
	public class PawnGroup_Sapper : IPawnGroupPlan
	{
		public List<Pawn> members = new List<Pawn>();

		public override int PawnNum
		{
			get => members.Count;
		}
		public override IEnumerator<Pawn> GetEnumerator()
		{
			return members.GetEnumerator();
		}
		public override bool InProgress
		{
			get;
		}
	}
}
