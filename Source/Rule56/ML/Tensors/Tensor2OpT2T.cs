using System;
using Verse;

namespace CombatAI
{
	public class Tensor2OpT2T : Tensor2Op
	{		
		private readonly Pair<int, int> fShape;
		private readonly Pair<int, int> sShape;
		private readonly Pair<int, int> oShape;

		public			Tensor2 output;
		public readonly Tensor2OpType type;
		public readonly Tensor2Op first;
		public readonly Tensor2Op second;

		public Tensor2OpT2T(Tensor2Op first, Tensor2Op second, Tensor2OpType type)
		{
			this.first = first;
			this.second = second;
			this.type = type;
			this.fShape = first.OutputShape();
			this.sShape = second.OutputShape();
			if (type == Tensor2OpType.dotP1)
			{
				if (fShape.second != sShape.first - 1)
					throw new Exception($"Input doesn't support padded dot product. Got ({fShape.first},{fShape.second}) and ({sShape.first},{sShape.second})");
				this.oShape = new Pair<int, int>(fShape.first, sShape.second);
			}
			else if (type == Tensor2OpType.dot)
			{
				if (fShape.second != sShape.first)
					throw new Exception($"Input doesn't support dot product. Got ({fShape.first},{fShape.second}) and ({sShape.first},{sShape.second})");
				this.oShape = new Pair<int, int>(fShape.first, sShape.second);
			}
			else
			{
				if (fShape != sShape)
					throw new Exception($"Both inputs need to have matching shapes. Got ({fShape.first},{fShape.second}) and ({sShape.first},{sShape.second})");
				this.oShape = fShape;
			}
			this.output = new Tensor2(this.oShape.first, this.oShape.second);
		}

		public override Tensor2 Evaluate()
		{			
			switch (type)
			{
				case Tensor2OpType.add:
					TensorUtility.Add(first.Evaluate(), second.Evaluate(), output);
					break;
				case Tensor2OpType.mul:
					TensorUtility.Mul(first.Evaluate(), second.Evaluate(), output);
					break;
				case Tensor2OpType.sub:
					TensorUtility.Sub(first.Evaluate(), second.Evaluate(), output);
					break;
				case Tensor2OpType.div:
					TensorUtility.Div(first.Evaluate(), second.Evaluate(), output);
					break;
				case Tensor2OpType.dot:	
					TensorUtility.Dot(first.Evaluate(), second.Evaluate(), output);
					break;
				case Tensor2OpType.dotP1:
					TensorUtility.DotP1(first.Evaluate(), second.Evaluate(), output);
					break;
			}
			return output;
		}

		public override Pair<int, int> OutputShape() => oShape;
	}
}

