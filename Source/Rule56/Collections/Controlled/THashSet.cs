using System.Collections.Generic;
namespace CombatAI
{
	public sealed class THashSet<T> : HashSet<T>
	{
		public THashSet() : base()
		{
			Register();
		}
		
		public THashSet(int size) : base(size)
		{
			Register();
		}

		private void Register()
		{
			ControlledCollectionTracker.Register(this);
		}
	}
}
