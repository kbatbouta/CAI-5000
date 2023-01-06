using System;
namespace CombatAI
{
	public abstract class IFeature
	{
		public string name;		

		public IFeature(string name)
		{
			this.name = name;
		}		
	}
}

