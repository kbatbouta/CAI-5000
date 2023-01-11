using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using RimWorld;
using Verse;

namespace CombatAI
{

	public abstract class Tensor2Op
	{	
		public Tensor2Op()
		{
		}

		public abstract Tensor2 Evaluate();
		public abstract Pair<int,int> OutputShape();

		public Tensor2Op Add(Tensor2Op op)
		{
			if (op is FloatVar fv)
				return new Tensor2OpT2F(this, fv, Tensor2OpType.add);
			else
				return new Tensor2OpT2T(this, op, Tensor2OpType.add);
		}

		public Tensor2Op Mul(Tensor2Op op)
		{
			if (op is FloatVar fv)
				return new Tensor2OpT2F(this, fv, Tensor2OpType.mul);
			else
				return new Tensor2OpT2T(this, op, Tensor2OpType.mul);
		}

		public Tensor2Op Sub(Tensor2Op op)
		{
			if (op is FloatVar fv)
				return new Tensor2OpT2F(this, fv, Tensor2OpType.sub);
			else
				return new Tensor2OpT2T(this, op, Tensor2OpType.sub);
		}

		public Tensor2Op Div(Tensor2Op op)
		{
			if (op is FloatVar fv)
				return new Tensor2OpT2F(this, fv, Tensor2OpType.div);
			else
				return new Tensor2OpT2T(this, op, Tensor2OpType.div);
		}

		public Tensor2Op Dot(Tensor2Op op)
		{
			return new Tensor2OpT2T(this, op, Tensor2OpType.dot);
		}

		public Tensor2Op DotP1(Tensor2Op op)
		{
			return new Tensor2OpT2T(this, op, Tensor2OpType.dotP1);
		}

		public Tensor2Op Add(float val)
		{
			if (this is FloatVar)
				throw new Exception("Operation not supported between 2 floats.");
			return new Tensor2OpT2F(this, new FloatVar(val), Tensor2OpType.add);
		}

		public Tensor2Op Mul(float val)
		{
			if (this is FloatVar)
				throw new Exception("Operation not supported between 2 floats.");
			return new Tensor2OpT2F(this, new FloatVar(val), Tensor2OpType.mul);
		}

		public Tensor2Op Sub(float val)
		{
			if (this is FloatVar)
				throw new Exception("Operation not supported between 2 floats.");
			return new Tensor2OpT2F(this, new FloatVar(val), Tensor2OpType.sub);
		}

		public Tensor2Op Div(float val)
		{
			if (this is FloatVar)
				throw new Exception("Operation not supported between 2 floats.");
			return new Tensor2OpT2F(this, new FloatVar(val), Tensor2OpType.div);
		}

		public Tensor2Op Lambda(Action<Tensor2, Tensor2> action, Pair<int, int>? outputShape)
		{
			if (this is FloatVar)
				throw new Exception("Lambda doesn't support float inputs.");
			return new TensorOpLambda(this, action, outputShape);
		}

		public Tensor2Op Flatten()
		{
			if (this is FloatVar)
				throw new Exception("Flatten doesn't support float inputs.");
			Pair<int, int> shape = OutputShape();
			Action<Tensor2, Tensor2> action = (tensor, dest) =>
			{
				for (int i = 0; i < shape.first; i++)
				{
					for (int j = 0; j < shape.second;j++)		
						dest[0, i * shape.second + j] = tensor[i, j];					
				} 
			};
			return new TensorOpLambda(this, action, new Pair<int, int>(1, shape.first * shape.second));
		}

		public Tensor2Op Reshape(Pair<int, int> newShape)
		{
			if (this is FloatVar)
				throw new Exception("Flatten doesn't support float inputs.");
			Pair<int, int> shape = OutputShape();
			Action<Tensor2, Tensor2> action = (tensor, dest) =>
			{				
				for (int i = 0; i < shape.first; i++)
				{
					for (int j = 0; j < shape.second; j++)
					{
						int u = i * shape.second + j;
						dest[u / newShape.second, u % newShape.second] = tensor[i, j];
					}
				}
			};
			return new TensorOpLambda(this, action, newShape);
		}

		public Tensor2Op Reshape(int m, int n )
		{
			return this.Reshape(new Pair<int, int>(m, n));
		}

		public Tensor2Op T(bool deep = false)
		{
			if (this is FloatVar)
				throw new Exception("Flatten doesn't support float inputs.");
			return new Tensor2OpTranspose(this, deep);
		}
	}
}

