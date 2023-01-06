using System;
namespace CombatAI
{
	public class IFeature_Const : IFeature
	{
		public float value;

		public IFeature_Const(string name, float value) : base(name)
		{
			this.value = value;
		}
	}
}

