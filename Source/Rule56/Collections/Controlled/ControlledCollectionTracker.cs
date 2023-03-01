using System;
using System.Collections;
using System.Collections.Generic;
namespace CombatAI
{
	public class ControlledCollectionTracker
	{
		public static void Register<T>(ICollection<T> collection)
		{
		}

		public abstract class ITCollection
		{
			public abstract bool IsValid { get; }
			public abstract bool TryGetCount(out int count);
		}
		
		public class ITList<T>
		{
		}
	}
}
