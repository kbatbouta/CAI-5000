using System;
using System.Runtime.CompilerServices;
using Verse;

namespace CombatAI
{
	public class Tensor2Var : Tensor2Op
	{
		private Tensor2 val;
		private readonly Pair<int, int> outputShape;

		public Tensor2Var(int m, int n)
		{
			outputShape = new Pair<int, int>(m, n);
		}

		public Tensor2Var(Pair<int, int> shape)
		{
			outputShape = shape;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override Tensor2 Evaluate()
		{
			return Value;
		}
		
		public override Pair<int, int> OutputShape()
		{
			return outputShape;
		}

		public Tensor2 Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return val;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (value.shape != outputShape)
				{
					throw new Exception($"Value must match the shape of 'Tensor2Var.shape'. Tried to assign ({value.shape.first}, {value.shape.second}) to ({outputShape.first}, {outputShape.second}) Tensor2Var.");
				}
				val = value;
			}
		}
	}	
}

