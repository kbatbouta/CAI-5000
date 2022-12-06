using System;
using System.Collections.Generic;

namespace CombatAI
{
	public static class TCacheHelper
	{
		public static readonly List<Action> clearFuncs = new List<Action>();

		public static void ClearCache()
		{
			for (var i = 0; i < clearFuncs.Count; i++)
			{
				var action = clearFuncs[i];
				action();
			}
		}
	}
}