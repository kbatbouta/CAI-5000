using System;
using System.Collections.Generic;
using Verse;

namespace CombatAI
{
	public class SeqBreeder
	{
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
			if (queue.Count < 6)
			{
				Log.Message($"Breed still needs {6 - queue.Count}.");
				for (int i = 0; i < recycled.weights.Count; i++)
				{
					InitializationUtility.Randomize(recycled.weights[i], -0.5f, 1f);
				}				
			}
			else
			{
				queue.RemoveAll(s => s.score <= 0);
				_temp.Clear();
				int num = Rand.Range(2, Maths.Min(_temp.Count, 8));
				for(int i = 0;i < num; i++)
				{
					_temp.Add(queue.RandomElementByWeight(s => s.score - s.breeded));
				}
				_temp.SortBy(s => - (s.score - s.breeded));
				Sequential.DeepCopyWeights(_temp[0].sequential, recycled);
				for(int i = 1; i < _temp.Count; i++)
				{
					for (int j = 0; j < recycled.weights.Count; j++)
					{
						TensorUtility.Add(recycled.weights[j], _temp[i].sequential.weights[j], recycled.weights[j]);
					}
					_temp[i].breeded++;
				}
				for (int j = 0; j < recycled.weights.Count; j++)
				{
					TensorUtility.Div(recycled.weights[j], _temp.Count, recycled.weights[j]);
				}
				for (int j = 0; j < recycled.weights.Count; j++)
				{
					TensorUtility.Noise(recycled.weights[j], -0.15f, 0.15f, recycled.weights[j]);
				}
				queue.RemoveAll(s => s.breeded > s.score + 1);
			}
			return recycled;
		}		

		public class SeqSpecimen
		{
			public Sequential sequential;
			public int score;
			public int breeded;
		}
	}
}

