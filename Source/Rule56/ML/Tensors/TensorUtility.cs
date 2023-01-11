using System;
using Verse;

namespace CombatAI
{
	public static class TensorUtility
	{		
		public static float Dot(this Tensor1 first, Tensor1 second)
		{
			var length = first.Length;
			var result = 0f;
			for (int i = 0; i < length; i++)
			{
				result += first[i] * second[i];
			}
			return result;
		}

		public static void Noise(this Tensor1 first, float min, float max, Tensor1 dest)
		{
			var length = first.Length;
			for (int i = 0; i < length; i++)
			{
				dest[i] = first[i] + Rand.Range(min, max);
			}
		}

		public static void Noise(this Tensor2 t1, float min, float max, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = t1[i, j] + Rand.Range(min, max);
				}
			}
		}

		public static void Add(this Tensor1 first, Tensor1 second, Tensor1 dest)
		{
			var length = first.Length;	
			for (int i = 0; i < length; i++)
			{
				dest[i] = first[i] + second[i];
			}
		}		

		public static void Mul(this Tensor1 first, Tensor1 second, Tensor1 dest)
		{
			var length = first.Length;
			for (int i = 0; i < length; i++)
			{
				dest[i] = first[i] * second[i];
			}
		}

		public static void Sub(this Tensor1 first, Tensor1 second, Tensor1 dest)
		{
			var length = first.Length;
			for (int i = 0; i < length; i++)
			{
				dest[i] = first[i] - second[i];
			}
		}

		public static void Div(this Tensor1 first, Tensor1 second, Tensor1 dest)
		{
			var length = first.Length;
			for (int i = 0; i < length; i++)
			{
				dest[i] = first[i] / second[i];
			}
		}

		public static void Add(this Tensor1 tensor, float val, Tensor1 dest)
		{
			var length = tensor.Length;
			for (int i = 0; i < length; i++)
			{
				dest[i] = tensor[i] + val;
			}
		}

		public static void Mul(this Tensor1 tensor, float val, Tensor1 dest)
		{
			var length = tensor.Length;
			for (int i = 0; i < length; i++)
			{
				dest[i] = tensor[i] * val;
			}
		}

		public static void Sub(this Tensor1 tensor, float val, Tensor1 dest)
		{
			var length = tensor.Length;
			for (int i = 0; i < length; i++)
			{
				dest[i] = tensor[i] - val;
			}
		}

		public static void Div(this Tensor1 tensor, float val, Tensor1 dest)
		{
			var length = tensor.Length;
			for (int i = 0; i < length; i++)
			{
				dest[i] = tensor[i] / val;
			}
		}

		public static void Add(this Tensor2 t1, float val, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = t1[i, j] + val;
				}
			}
		}

		public static void Mul(this Tensor2 t1, float val, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = t1[i, j] * val;
				}
			}
		}

		public static void Sub(this Tensor2 t1, float val, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = t1[i, j] - val;
				}
			}
		}

		public static void Div(this Tensor2 t1, float val, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = t1[i, j] / val;
				}
			}
		}

		public static void Add(this Tensor2 t1, Tensor2 t2, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = t1[i, j] + t2[i, j];
				}
			}
		}

		public static void Mul(this Tensor2 t1, Tensor2 t2, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = t1[i, j] * t2[i, j];
				}
			}
		}

		public static void Sub(this Tensor2 t1, Tensor2 t2, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = t1[i, j] - t2[i, j];
				}
			}
		}

		public static void Div(this Tensor2 t1, Tensor2 t2, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = t1[i, j] / t2[i, j];
				}
			}
		}

		public static void Dot(this Tensor2 t1, Tensor2 t2, Tensor2 dest)
		{			
			var l1 = t1.shape.first;
			var l2 = t2.shape.second;
			var l3 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					float val = 0f;
					for (int k = 0; k < l3;k++)
					{
						val += t1[i, k] * t2[k, j];
					}
					dest[i, j] = val;
				}
			}
		}


		public static void DotP1(this Tensor2 t1, Tensor2 t2, Tensor2 dest)
		{
			var l1 = t1.shape.first;
			var l2 = t2.shape.second;
			var l3 = t1.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					float val = 0f;
					for (int k = 0; k < l3; k++)
					{
						val += t1[i, k] * t2[k, j];
					}
					dest[i, j] = val + t2[l3, j];
				}
			}
		}
	}
}

