using System.Collections;
using System.Collections.Generic;
using Verse;
namespace CombatAI
{
	public abstract class IPawnGroup : IExposable, ILoadReferenceable, IEnumerable<Pawn>
	{
		public int groupIDNumber = -1;

		public virtual bool IsValid
		{
			get => groupIDNumber != -1 && PawnNum > 0;
		}
		public abstract int PawnNum
		{
			get;
		}

		public abstract IEnumerator<Pawn> GetEnumerator();
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref groupIDNumber, "squadIDNumber");
		}

		public string GetUniqueLoadID()
		{
			return $"{GetType().Name}_{groupIDNumber}";
		}

		public static T Create<T>() where T : IPawnGroup, new()
		{
			return new T
			{
				groupIDNumber = UniqueIDsManager.GetNextID<T>()
			};
		}
	}
}
