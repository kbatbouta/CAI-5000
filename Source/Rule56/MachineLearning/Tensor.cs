using System;
namespace CombatAI
{
	public class Tensor
	{		
		public readonly float[] arr;		

		public Tensor(Tensor other)
		{
			arr = new float[other.Length];			
			Array.Copy(other.arr, arr, other.Length);
		}

		public Tensor(int length)
		{
			arr = new float[length];			
		}

		public int Length
		{
			get
			{
				return arr.Length;
			}
		}
	}
}

