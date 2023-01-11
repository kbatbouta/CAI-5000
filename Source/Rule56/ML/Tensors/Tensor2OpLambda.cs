using System;
using Verse;

namespace CombatAI
{
	public class TensorOpLambda : Tensor2Op
	{		
		public			Tensor2 output;	
		public readonly Action<Tensor2, Tensor2> action;
		public readonly Tensor2Op tensor;

		public TensorOpLambda(Tensor2Op tensor, Action<Tensor2, Tensor2> action, Pair<int, int>? outputShape)
		{
			if (action == null)
				throw new Exception();
			this.action = action;
			this.tensor = tensor;
			Pair<int,int> oShape = outputShape != null ? outputShape.Value : tensor.OutputShape();
			this.output = new Tensor2(oShape.first, oShape.second);
		}

		public override Tensor2 Evaluate()
		{
			action(tensor.Evaluate(), output);
			return output;
		}

		public override Pair<int, int> OutputShape()
		{
			return this.output.shape;
		}
	}
}

