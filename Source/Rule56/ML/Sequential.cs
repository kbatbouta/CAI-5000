using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using Verse;

namespace CombatAI
{
	public class Sequential
	{
		private Tensor2 _feeder;

		public Tensor2Op headOp;
		public Tensor2Op tailOp;
		public Tensor2Var inVar;

		public readonly Pair<int, int> inputShape;
		public readonly List<Tensor2> weights = new List<Tensor2>();

		public Sequential(int m, int n)
		{
			this._feeder = new Tensor2(m, n);
			this.inputShape = new Pair<int, int>(m, n);
			this.headOp = this.inVar = new Tensor2Var(m, n);			
			this.tailOp = headOp;
		}

		public Sequential(Pair<int, int> inputShape)
		{
			this._feeder = new Tensor2(inputShape);
			this.inputShape = inputShape;
			this.headOp = this.inVar = new Tensor2Var(inputShape);
			this.tailOp = headOp;
		}

		public Sequential(Tensor2Var inVar, Tensor2Op op)
		{
			this._feeder = new Tensor2(inVar.OutputShape());
			this.inVar = inVar;
			this.inputShape = inVar.OutputShape();
			this.headOp = op;
			this.tailOp = headOp;
		}

		public Sequential(Sequential sequential)
		{
			this._feeder = new Tensor2(sequential._feeder.shape);
			this.inputShape = sequential.inputShape;
			this.headOp = sequential.headOp;
			this.tailOp = sequential.tailOp;
			this.inVar = sequential.inVar;			
		}

		public Pair<int, int> OutputShape
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return tailOp.OutputShape();
			}
		}

		public Sequential AddDense(int varNum, bool useBias, ActivationType activation, InitializationType weightInit = InitializationType.random01, InitializationType biasInit = InitializationType.zeros)
		{
			Pair<int, int> s1 = this.OutputShape;
			Pair<int, int> shape = new Pair<int, int>(s1.second + (useBias ? 1 : 0), varNum);
			Tensor2Var tensorVar = new Tensor2Var(shape);			
			Tensor2 tensor = tensorVar.Value = new Tensor2(shape);
			weights.Add(tensor);
			switch (weightInit)
			{				
				case InitializationType.zeros:
					InitializationUtility.ReSet(tensor, 0f);
					break;
				case InitializationType.random01:
					InitializationUtility.Randomize(tensor, 0f, 1f);
					break;
				default:
					InitializationUtility.ReSet(tensor, 1f);
					break;
			}
			if (useBias)
			{
				switch (biasInit)
				{
					case InitializationType.ones:
						InitializationUtility.ReSet(tensor[s1.second], 1f);
						break;
					case InitializationType.random01:
						InitializationUtility.Randomize(tensor[s1.second], 0f, 1f);
						break;
					default:
						InitializationUtility.ReSet(tensor[s1.second], 0f);
						break;
				}				
			}
			if (useBias)
				this.tailOp = this.tailOp.DotP1(tensorVar);
			else
				this.tailOp = this.tailOp.Dot(tensorVar);
			if (activation != ActivationType.none)	
				this.tailOp = this.tailOp.Lambda(activation.Function(), null);			
			return this;
		}

		public Tensor2 Evaluate(float val1)
		{
			_feeder[0, 0] = val1;			
			inVar.Value = _feeder;
			return tailOp.Evaluate();
		}

		public Tensor2 Evaluate(float val1, float val2)
		{
			_feeder[0, 0] = val1;
			_feeder[0, 1] = val2;
			inVar.Value = _feeder;
			return tailOp.Evaluate();
		}

		public Tensor2 Evaluate(float val1, float val2, float val3)
		{
			_feeder[0, 0] = val1;
			_feeder[0, 1] = val2;
			_feeder[0, 2] = val3;
			inVar.Value = _feeder;
			return tailOp.Evaluate();
		}

		public Tensor2 Evaluate(float val1, float val2, float val3, float val4)
		{
			_feeder[0, 0] = val1;
			_feeder[0, 1] = val2;
			_feeder[0, 2] = val3;
			_feeder[0, 3] = val4;
			inVar.Value = _feeder;
			return tailOp.Evaluate();
		}

		public Tensor2 Evaluate(float val1, float val2, float val3, float val4, float val5)
		{
			_feeder[0, 0] = val1;
			_feeder[0, 1] = val2;
			_feeder[0, 2] = val3;
			_feeder[0, 3] = val4;
			_feeder[0, 4] = val5;
			inVar.Value = _feeder;
			return tailOp.Evaluate();
		}

		public Tensor2 Evaluate(float val1, float val2, float val3, float val4, float val5, float val6)
		{
			_feeder[0, 0] = val1;
			_feeder[0, 1] = val2;
			_feeder[0, 2] = val3;
			_feeder[0, 3] = val4;
			_feeder[0, 4] = val5;
			_feeder[0, 5] = val6;
			inVar.Value = _feeder;
			return tailOp.Evaluate();
		}

		public Tensor2 Evaluate(float val1, float val2, float val3, float val4, float val5, float val6, float val7)
		{
			_feeder[0, 0] = val1;
			_feeder[0, 1] = val2;
			_feeder[0, 2] = val3;
			_feeder[0, 3] = val4;
			_feeder[0, 4] = val5;
			_feeder[0, 5] = val6;
			_feeder[0, 6] = val7;
			inVar.Value = _feeder;
			return tailOp.Evaluate();
		}

		public Tensor2 Evaluate(float val1, float val2, float val3, float val4, float val5, float val6, float val7, params float[] vals)
		{
			_feeder[0, 0] = val1;
			_feeder[0, 1] = val2;
			_feeder[0, 2] = val3;
			_feeder[0, 3] = val4;
			_feeder[0, 4] = val5;
			_feeder[0, 5] = val6;
			_feeder[0, 6] = val7;
			for (int i = 0; i < vals.Length; i++)
			{
				_feeder[0, i + 7] = vals[i];
			}
			inVar.Value = _feeder;
			return tailOp.Evaluate();
		}

		public static void DeepCopyWeights(Sequential source, Sequential dest)
		{
			for(int i = 0; i < source.weights.Count; i++)
			{
				source.weights[i].DeepCopyTo(dest.weights[i]);
			}
		}

		public static void WriteWeights(Sequential sequential, XmlDocument document, XmlNode root)
		{
			foreach(var weight in sequential.weights)
			{
				XmlElement node = document.CreateElement("li");
				node.InnerText = weight.Serialize();
				root.AppendChild(node);
			}
		}

		public static void LoadWeights(Sequential sequential, XmlNode root)
		{
			int i = 0;
			foreach (var node in root)
			{
				sequential.weights[i++].Parse((node as XmlElement).InnerText);
			}
		}
	}
}

