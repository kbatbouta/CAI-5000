using System;
using System.Collections.Generic;

namespace CombatAI
{
	public static class TCacheHelper
	{
		public static readonly List<Action> clearFuncs = new List<Action>();

		public static void ClearCache()
		{
			for (int i = 0; i < clearFuncs.Count; i++)
			{
				Action action = clearFuncs[i];
				action();
			}
		}
	}
}
