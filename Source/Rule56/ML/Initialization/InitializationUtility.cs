using System;
using Verse;

namespace CombatAI
{
	public static class InitializationUtility
	{
		public static void Randomize(Tensor1 tensor, float minVal = 0f, float maxVal = 1f)
		{
			var length = tensor.Length;
			for (int i = 0; i < length; i++)
			{
				tensor[i] = Rand.Range(minVal, maxVal);
			}
		}

		public static void ReSet(Tensor1 tensor, float val)
		{
			var length = tensor.Length;
			for (int i = 0; i < length; i++)
			{
				tensor[i] = val;
			}
		}

		public static void ReSet(Tensor1 tensor, Func<int, float> func)
		{
			var length = tensor.Length;
			for (int i = 0; i < length; i++)
			{
				tensor[i] = func(i);
			}
		}

		public static void Randomize(Tensor2 tensor, float minVal = 0f, float maxVal = 1f)
		{
			var l1 = tensor.shape.first;
			var l2 = tensor.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					tensor[i, j] = Rand.Range(minVal, maxVal);
				}
			}
		}

		public static void ReSet(Tensor2 tensor, float val)
		{
			var l1 = tensor.shape.first;
			var l2 = tensor.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					tensor[i, j] = val;
				}
			}
		}

		public static void ReSet(Tensor2 tensor, Func<int, int, float> func)
		{
			var l1 = tensor.shape.first;
			var l2 = tensor.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					tensor[i, j] = func(i, j);
				}
			}
		}
	}
}

