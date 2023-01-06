using System;
namespace CombatAI
{
	public class LinearModel : IModel
	{
		public Tensor weights;		
		public float bias;

		public LinearModel(string name, IFeatureSet features) : base(name, features)
		{
		}

		public override void CopyTo(IModel dest)
		{
			throw new NotImplementedException();
		}

		public override void Load(string path)
		{
			throw new NotImplementedException();
		}

		public override void Write(string path, bool canOverwrite)
		{
			throw new NotImplementedException();
		}

		protected override object DeepCloneInternal()
		{
			throw new NotImplementedException();
		}
	}
}

