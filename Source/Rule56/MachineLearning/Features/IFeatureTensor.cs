using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CombatAI
{
	public class IFeatureTensor
	{
		private readonly List<Pair<int, IFeature_Product>> products;

		public IFeatureSet features;
		public Tensor tensor;

		public IFeatureTensor(IFeatureSet features)
		{
			this.features = features;													
			tensor = new Tensor(features.Count);			
			IFeature[] arr = features.AllFeatures;
			for (int i = 0; i < arr.Length; i++)
			{
				IFeature f = arr[i];
				// copy consts.
				if (f is IFeature_Const constF)
				{					
					tensor.arr[i] = constF.value;
				}
				else if(f is IFeature_Product productF)
				{
					products ??= new List<Pair<int, IFeature_Product>>();
					products.Add(new Pair<int, IFeature_Product>(i, productF));
				}
			}
		}

		public float this[IFeatureSet.IFKey key]
		{
			set
			{
				tensor.arr[key.index] = value;
			}
			get => tensor.arr[key.index];
		}

		public void Prepare()
		{
			if (products != null)
			{
				for (int i = 0;i < products.Count; i++)
				{
					Pair<int, IFeature_Product> pair = products[i];
					tensor.arr[pair.first] = tensor.arr[pair.second.first.index] * tensor.arr[pair.second.second.index];
				}
			}
		}
	}
}

