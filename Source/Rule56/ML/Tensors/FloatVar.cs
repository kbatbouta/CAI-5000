using System;
using Verse;

namespace CombatAI
{
	public class FloatVar : Tensor2Op
	{
		public float _value;

		public FloatVar(float value)
		{
			this._value = value;
		}

		public float Value
		{
			get => _value;
			set => _value = value;
		}

		public override Tensor2 Evaluate()
		{
			throw new NotSupportedException();
		}

		public override Pair<int, int> OutputShape()
		{
			throw new NotSupportedException();
		}
	}
}

