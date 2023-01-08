using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatAI
{
	public class LinearModel
	{
		private Tensor _temp_avg;
		private Tensor _temp_momentums;		

		public int iteration;
		public Tensor weights;
		public Func<float, float> activation;
		public float momentumFactor;
		public float momentumRetention;
		public Tensor momentums;		

		public LinearModel(int featureNum, Func<float, float> activation, float momentumFactor, float momentumRetention)
		{			
			this.activation = activation;			
			weights = new Tensor(featureNum);
			momentums = new Tensor(featureNum);
			this.momentumFactor = momentumFactor;
			this.momentumRetention = momentumRetention;			
		}

		public int FeatureNum
		{
			get => weights.Length;
		}

		public float Evaluate(Tensor inputTensor)
		{
			float result = 0;
			for(int i = 0;i < inputTensor.Length; i++)
			{
				result += inputTensor.arr[i] * weights.arr[i];
			}
			if (activation != null)
			{
				return activation(result);
			}
			return result;
		}

		public void Mutate(float intensity, float chance)
		{
			int num = FeatureNum;
			float[] w = weights.arr;
			for (int i = 0;i < num; i++)
			{
				if (Rand.Chance(chance))
				{
					w[i] += intensity * (Rand.Chance(0.5f) ? 1f : -1f) * Rand.Range(0.5f, 1.0f);
				}
			}
		}

		public void Train(IEnumerable<LinearModel> mutatedChildren)
		{
			LinearModel[] mc = mutatedChildren.ToArray();
			if(_temp_avg == null || _temp_momentums == null)
			{
				_temp_avg = new Tensor(FeatureNum);
				_temp_momentums = new Tensor(FeatureNum);				
			}			
			Tensor.Mul(_temp_avg, 0f, _temp_avg);
			Tensor.Mul(_temp_momentums, 0f, _temp_momentums);
			// calculate the avg of all weights from the mutated children.
			for (int i = 0; i < mc.Length; i++)
			{
				LinearModel child = mc[i];
				Tensor.Add(_temp_avg, child.weights, _temp_avg);				
			}
			Tensor.Div(_temp_avg, mc.Length, _temp_avg);
			// calculate the new momentum (and apply the momentum factor) before applying the old one.
			Tensor.Sub(_temp_avg, weights, _temp_momentums);		
			Tensor.Mul(_temp_momentums, momentumFactor, _temp_momentums);
			// adjust weights.
			Tensor.Add(_temp_avg, this.momentumFactor, weights);
			// adjust and save the new momentum.
			// next momentum = old * momentumRetention + new * (1 - momentumRetention)
			Tensor.Mul(_temp_momentums, 1 - momentumRetention, _temp_momentums);
			Tensor.Mul(momentums, momentumRetention, momentums);
			Tensor.Add(momentums, _temp_momentums, momentums);

			iteration++;
		}

		public LinearModel DeepCopy()
		{
			LinearModel model = new LinearModel(FeatureNum, activation, momentumFactor, momentumRetention);
			Array.Copy(weights.arr, model.weights.arr, FeatureNum);
			model.momentums = momentums;
			return model;
		}

		public void CopyTo(LinearModel model)
		{
			if (model.FeatureNum != FeatureNum)
			{
				throw new Exception("Both models need to have matching featureNum");
			}
			Array.Copy(weights.arr, model.weights.arr, FeatureNum);			
			model.activation = activation;
			model.momentumFactor = momentumFactor;
			model.momentums = momentums;			
		}
	}
}

