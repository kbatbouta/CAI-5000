﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
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
			if (queue.Count < 10 || Rand.Chance(0.2f))
			{
				Log.Message($"Breed still needs {6 - queue.Count}.");
				for (int i = 0; i < recycled.weights.Count; i++)
				{
					InitializationUtility.Randomize(recycled.weights[i], -0.5f, 1f);
				}				
			}
			else
			{	
				queue.SortBy(s => -(s.score * 2 - s.breeded));
				_temp.Clear();
				int num = Rand.Range(2, Maths.Min(_temp.Count, 6));
				for(int i = 0;i < num; i++)
				{
					if (Rand.Chance(0.5f))
					{
						_temp.Add(queue[i]);
					}
					else
					{
						_temp.Add(queue.Where(s => !_temp.Contains(s)).RandomElementByWeight(s => s.score * 2 - s.breeded));
					}					
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
				int k = queue.Count - 1;
				while (k-- > 32)
				{
					queue.RemoveAt(k);
				}	
				queue.RemoveAll(s => s.breeded >= s.score);
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

