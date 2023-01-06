using System;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;

namespace CombatAI
{
	public class IFeatureSet
	{
		private bool _readonly;
		private bool _simple = true;
		private int _simpleNum;
		private List<IFeature> features = new List<IFeature>();
		private IFeature[] _featuresArr;

		public IFeatureSet()
		{
		}

		public bool ReadOnly
		{
			get => _readonly;
		}

		public int Count
		{
			get => _featuresArr.Length;
		}

		public int SimpleCount
		{
			get => _simpleNum;
		}

		public IFeature[] AllFeatures
		{
			get => _featuresArr;
		}

		public IFeature this[IFKey key]
		{
			get => _featuresArr[key.index];
		}		

		public IFKey Add(IFeature feature)
		{			
			if (_readonly)
			{
				throw new Exception($"Readonly FeatureSet cannot add new features. Got new feature '{feature.name}'.");
			}
			if (features.Any(f => f.name == feature.name))
			{
				throw new Exception($"FeatureSet already has feature '{feature.name}'");
			}
			if (!_simple)
			{
				if (feature is IFeature_Simple)
				{
					throw new Exception($"IFeature_Simple {feature.name} need to be added before any other type");
				}
			}
			else
			{
				_simple = feature is IFeature_Simple;
			}
			features.Add(feature);
			return new IFKey()
			{
				index = features.Count - 1
			};
		}		

		public void SetReadOnly()
		{
			_readonly = true;
			_simpleNum = features.Count(f => f is IFeature_Simple);
			_featuresArr = new IFeature[features.Count];
			for (int i = 0; i < features.Count; i++)
			{
				_featuresArr[i] = features[i];
			}
			features.Clear();
			features = null;
		}

		public struct IFKey
		{
			public int index;
		}		
	}
}

