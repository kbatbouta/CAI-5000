using System.Collections.Generic;
using Verse;
namespace CombatAI.Squads
{
	public class Squad : IExposable, ILoadReferenceable
	{
		public int        squadIDNumber;
		public List<Pawn> members = new List<Pawn>();

		private Squad()
		{
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref squadIDNumber, "squadIDNumber");
			Scribe_Collections.Look(ref members, "members", LookMode.Reference);
		}
		
		public string GetUniqueLoadID()
		{
			return $"squad_{squadIDNumber}";
		}
	}
}
