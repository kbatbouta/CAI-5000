using System;
using Verse;

namespace CombatAI
{
	public class Tensor2OpTranspose : Tensor2Op
	{
		private readonly Pair<int, int> oShape;		
		private readonly Tensor2 output;

		public readonly bool deep;
		public readonly Tensor2Op tensor;

		public Tensor2OpTranspose(Tensor2Op tensor, bool deep = false)
		{
			this.tensor = tensor;
			this.deep = deep;
			Pair<int, int> shape = tensor.OutputShape();
			this.oShape = new Pair<int, int>(shape.second, shape.first);
			if (deep)
				output = new Tensor2(oShape.first, oShape.second);
		}

		public override Tensor2 Evaluate()
		{
			if (!deep)
				return tensor.Evaluate().T;
			tensor.Evaluate().T.DeepCopyTo(output);
			return output;
		}

		public override Pair<int, int> OutputShape()
		{
			return this.oShape;
		}
	}
}

