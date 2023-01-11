using System;
using Verse;

namespace CombatAI
{
	public class Tensor2OpT2F : Tensor2Op
	{
		public			Tensor2 output;
		public readonly Tensor2OpType type;
		public readonly Tensor2Op first;
		public readonly FloatVar second;

		public Tensor2OpT2F(Tensor2Op first, FloatVar second, Tensor2OpType type)
		{
			this.type = type;
			this.first = first;
			this.second = second;
			Pair<int, int> oShape = first.OutputShape();
			this.output = new Tensor2(oShape.first, oShape.second);			
		}

		public override Tensor2 Evaluate()
		{			
			switch (type)
			{
				case Tensor2OpType.add:
					TensorUtility.Add(first.Evaluate(), second.Value, output);
					break;
				case Tensor2OpType.mul:
					TensorUtility.Mul(first.Evaluate(), second.Value, output);
					break;
				case Tensor2OpType.sub:
					TensorUtility.Sub(first.Evaluate(), second.Value, output);
					break;
				case Tensor2OpType.div:
					TensorUtility.Div(first.Evaluate(), second.Value, output);
					break;
				case Tensor2OpType.dot:
					throw new Exception("Invalid operation Tensor2OpType.dot.");					
			}
			return output;
		}
	
		public override Pair<int, int> OutputShape()
		{			
			return this.output.shape;
		}
	}
}

