using System;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace CombatAI
{
	public class Tensor
	{		
		public readonly float[] arr;

		public Tensor(int length)
		{
			arr = new float[length];	
		}

		public Tensor(Tensor tensor)
		{
			arr = new float[tensor.Length];
			Array.Copy(tensor.arr, arr, tensor.Length);
		}

		public int Length
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return arr.Length;
			}
		}

		public float this[ITKey key]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return arr[(int)key - 1];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				arr[(int)key - 1] = value;
			}
		}
		
		public static float Dot(Tensor first, Tensor second)
		{
			float result = 0;
			for (int i = 0; i < first.Length; i++)
			{
				result += first.arr[i] * second.arr[i];
			}
			return result;
		}
		
		public static void Mul(Tensor tensor, float val, Tensor dest)
		{
			for (int i = 0;i < tensor.arr.Length; i++)
			{
				dest.arr[i] = tensor.arr[i] * val;
			}
		}
		
		public static void Add(Tensor tensor, float val, Tensor dest)
		{
			for (int i = 0; i < tensor.arr.Length; i++)
			{
				dest.arr[i] = tensor.arr[i] + val;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Sub(Tensor tensor, float val, Tensor dest)
		{
			Add(tensor, -val, dest);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Div(Tensor tensor, float val, Tensor dest)
		{
			Mul(tensor, 1f / val, dest);
		}
		
		public static void Mul(Tensor first, Tensor second, Tensor dest)
		{
			for (int i = 0;i < first.Length; i++)
			{
				dest.arr[i] = first.arr[i] * second.arr[i];
			}
		}
		
		public static void Div(Tensor first, Tensor second, Tensor dest)
		{
			for (int i = 0; i < first.Length; i++)
			{
				dest.arr[i] = first.arr[i] / second.arr[i];
			}
		}
		
		public static void Add(Tensor first, Tensor second, Tensor dest)
		{
			for (int i = 0; i < first.Length; i++)
			{
				dest.arr[i] = first.arr[i] + second.arr[i];
			}
		}
		
		public static void Sub(Tensor first, Tensor second, Tensor dest)
		{
			for (int i = 0; i < first.Length; i++)
			{
				dest.arr[i] = first.arr[i] - second.arr[i];
			}
		}

		public static void Copy(Tensor source, Tensor dest)
		{
			for (int i = 0; i < source.Length; i++)
			{
				dest.arr[i] = source.arr[i];
			}
		}
	}
}

