using System;
using UnityEngine;

namespace CombatAI
{
	public static class ActivationUtility
	{
		public static Action<Tensor2, Tensor2> Function(this ActivationType type)
		{
			switch (type)
			{
				case ActivationType.relu:
					return Relu;
				case ActivationType.reluSoft:
					return ReluSoft;
				case ActivationType.sigmoid:
					return Sigmoid;
				default:
					return null;
			}			
		}

		public static void Relu(Tensor2 first, Tensor2 dest)
		{
			var l1 = first.shape.first;
			var l2 = first.shape.second;
			for(int i = 0; i < l1; i++)
			{
				for(int j = 0; j < l2; j++)
				{
					dest[i, j] = Maths.Max(first[i, j], 0);
				}
			}
		}

		public static void ReluSoft(Tensor2 first, Tensor2 dest)
		{
			ReluSoft(first, dest, 0.01f);
		}

		public static void ReluSoft(Tensor2 first, Tensor2 dest, float leak)
		{
			var l1 = first.shape.first;
			var l2 = first.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					float val = first[i, j];
					if(val >= 0)				
						dest[i, j] = val;					
					else
						dest[i, j] = val * leak;
				}
			}
		}

		public static void Sigmoid(Tensor2 first, Tensor2 dest)
		{
			var l1 = first.shape.first;
			var l2 = first.shape.second;
			for (int i = 0; i < l1; i++)
			{
				for (int j = 0; j < l2; j++)
				{
					dest[i, j] = 1f / (1f + Mathf.Exp(-first[i, j]));
				}
			}
		}
	}
}

