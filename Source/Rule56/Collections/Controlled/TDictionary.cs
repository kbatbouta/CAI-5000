using System.Collections.Generic;
namespace CombatAI
{
	public sealed class TDictionary<T, K> : Dictionary<T, K>
	{
		public TDictionary() : base()
		{
			Register();
		}
		
		public TDictionary(int size) : base(size)
		{
			Register();
		}
		
		private void Register()
		{
			ControlledCollectionTracker.Register(this);
		}
	}
}
