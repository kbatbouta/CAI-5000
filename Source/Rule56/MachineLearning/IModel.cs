using System;
namespace CombatAI
{
	public abstract class IModel
	{
		private int id;
		private string name;
		private string nameCached;
		public IFeatureSet features;

		public IModel(string name, IFeatureSet features)
		{			
			if (!features.ReadOnly)
			{
				throw new Exception($"Model {name} expect readonly FeatureSet.");
			}
			this.name = name;
			this.features = features;			
		}
	
		public string Name
		{
			get => nameCached == null ? (nameCached = $"{name}_{id}") : nameCached;
		}

		public string ModelName
		{
			get => name;
		}

		public object DeepClone()
		{
			IModel model = (IModel) DeepCloneInternal();
			model.id = id + 1;
			model.nameCached = null;
			return model;
		}

		protected abstract object DeepCloneInternal();
		public abstract void CopyTo(IModel dest);
		public abstract void Write(string path, bool canOverwrite);
		public abstract void Load(string path);				
	}
}

