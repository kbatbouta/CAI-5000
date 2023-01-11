using System;
using System.Runtime.CompilerServices;
using Verse;

namespace CombatAI
{
	public class Tensor1
	{
		private Tensor2 tensor2;
		private int start;
		private int step;

		public Tensor1(int length)
		{
			this.tensor2 = new Tensor2(1, length);
			this.start = 0;
			this.step = 1;
		}

		public Tensor1(Tensor2 tensor2, int start, int step)
		{
			this.tensor2 = tensor2;
			this.start = start;
			this.step = step;
		}

		public Tensor1(Tensor1 tensor)
		{
			this.tensor2 = tensor.tensor2;
			this.start = tensor.start;
			this.step = tensor.step;
		}	

		public int Length
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return tensor2.shape.second;
			}
		}

		public float this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return tensor2.arr[start + index * step];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				tensor2.arr[start + index * step] = value;
			}
		}

		public override string ToString()
		{
			string str = "[" + this[0];
			for (int i = 1; i < Length; i++)
			{
				str += "," + this[i];
			}
			str += "]";
			return str;
		}
	}
}

