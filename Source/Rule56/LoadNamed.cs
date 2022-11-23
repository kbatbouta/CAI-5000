using System;
namespace CombatAI
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
	public class LoadNamed : Attribute
	{
		public string name;
		public Type[] prams;

		public LoadNamed(string name, Type[] prams = null)
		{
			this.name = name;
			this.prams = prams;
		}
	}
}

