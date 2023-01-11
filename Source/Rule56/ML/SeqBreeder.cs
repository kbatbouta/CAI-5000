using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatAI
{
	public class SeqBreeder
	{
		private int num;
		private List<SeqSpecimen> _temp = new List<SeqSpecimen>();

		public Sequential defSeq;
		public Func<Sequential> maker;
		public List<SeqSpecimen> queue = new List<SeqSpecimen>();

		public SeqBreeder(Func<Sequential> maker, Sequential defSeq)
		{
			this.maker = maker;
			this.defSeq = defSeq;
		}

		public Sequential TryBreedNewSpecimen(Sequential recycled)
		{
			if (recycled == null || defSeq == recycled)
			{
				recycled = maker();
			}
			if (queue.Count < 10 || Rand.Chance(0.10f - 0.10f * Maths.Min(num / 256f, 0.85f)))
			{
				Log.Message($"Breed still needs {6 - queue.Count}.");
				for (int i = 0; i < recycled.weights.Count; i++)
				{
					InitializationUtility.Randomize(recycled.weights[i], -0.5f, 1f);
				}				
			}
			else
			{
				float scores = queue.Sum(s => s.score);
				for(int i = 0;i < queue.Count; i++)
				{
					var s = queue[i];
					s.order = -s.score / (scores + 0.1f) * Maths.Max(Rand.Value, 0.5f);
				}
				queue.SortBy(s => s.order);
				_temp.Clear();
				int num = Rand.Range(2, Maths.Min(_temp.Count, 6));
				for(int i = 0;i < num; i++)
				{
					_temp.Add(queue[i]);					
				}
				_temp.SortBy(s => - s.score);
				Sequential.DeepCopyWeights(_temp[0].sequential, recycled);
				for(int i = 1; i < _temp.Count; i++)
				{
					for (int j = 0; j < recycled.weights.Count; j++)
					{
						TensorUtility.Add(recycled.weights[j], _temp[i].sequential.weights[j], recycled.weights[j]);
					}					
				}
				for (int j = 0; j < recycled.weights.Count; j++)
				{
					TensorUtility.Div(recycled.weights[j], _temp.Count, recycled.weights[j]);
				}
				if (Rand.Chance(0.2f))
				{
					for (int j = 0; j < recycled.weights.Count; j++)
					{
						float f = Maths.Max(1f - num / 1000f, 0.5f);
						TensorUtility.Noise(recycled.weights[j], -0.1f * f, 0.1f * f, recycled.weights[j]);
					}
				}
				int k = queue.Count - 1;
				while (k-- > 64)
				{
					queue.RemoveAt(k);
					num++;
				}				
			}			
			return recycled;
		}		

		public class SeqSpecimen
		{
			public Sequential sequential;
			public float score;
			public float order;
		}
	}
}

