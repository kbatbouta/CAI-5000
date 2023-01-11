using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace CombatAI
{
	public class Tensor2
	{
		private Tensor2 _t;
		private int _i;
		private int _j;

		public Pair<int, int> shape;
		public float[] arr;
			
		public Tensor2(int m, int n)
		{
			this.shape = new Pair<int, int>(m, n);
			this.arr = new float[shape.first * shape.second];
			this._i = shape.second;
			this._j = 1;
		}

		public Tensor2(Pair<int, int> shape)
		{
			this.shape = shape;
			this.arr = new float[shape.first * shape.second];
			this._i = shape.second;
			this._j = 1;
		}

		public Tensor2(Tensor2 tensor)
		{
			this.shape = tensor.shape;
			this.arr = tensor.arr;
			this._i = tensor._i;
			this._j = tensor._j;
		}

		private Tensor2(float[] arr, Pair<int, int> shape)
		{
			this.shape = shape;
			this.arr = arr;			
		}

		public Tensor2 T
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (_t == null)
				{
					_t = new Tensor2(arr, new Pair<int, int>(shape.second, shape.first));
					_t._t = this;
					_t._i = 1;
					_t._j = shape.second;
				}
				return _t;
			}
		}

		public float this[int i, int j]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return arr[i * _i + j * _j];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				arr[i * _i + j * _j] = value;
			}
		}

		public Tensor1 this[int i]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return new Tensor1(this, i * _i, _j);
			}
		}

		public void DeepCopyTo(Tensor2 other)
		{
			Array.Copy(this.arr, other.arr, this.shape.first * this.shape.second);
			other._i = this._i;
			other._j = this._j;		
		}

		public override string ToString()
		{
			string str = "[";
			for (int i = 0; i < shape.first; i++)
			{
				if (i != 0)
				{
					str += " ";
				}
				str += "[" + this[i, 0];
				for (int j = 1; j < shape.second; j++)
				{
					str += ", " + this[i, j];
				}
				str += "]";
				if (i != shape.first - 1)
				{
					str += "\n";
				}
			}
			str += "]";
			return str;
		}

		public string Serialize()
		{
			string data = $"{arr[0]}";
			for(int i = 1;i < arr.Length; i++)
			{
				data += $",{arr[i]}";
			}
			data += $",{_i},{_j}";			
			return data;
		}

		public void Parse(string text)
		{
			string[] tokens = text.Split(',');
			int length = arr.Length;
			for (int i = 0; i < length; i++)
			{
				arr[i] = float.Parse(tokens[i].Trim());
			}
			_i = int.Parse(tokens[length - 2].Trim());
			_j = int.Parse(tokens[length - 1].Trim());
		}
	}	
}

