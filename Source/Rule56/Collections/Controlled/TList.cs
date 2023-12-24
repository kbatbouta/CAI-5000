using System.Collections.Generic;
namespace CombatAI
{
	public sealed class TList<T> : List<T>
	{
		public TList() : base()
		{
			Register();
		}
		
		public TList(int size) : base(size)
		{
			Register();
		}
		
		private void Register()
		{
			ControlledCollectionTracker.Register(this);
		}
	}
}
