using System;
namespace CombatAI
{
	public static class SeqFactory
	{
		public static Sequential MakeReaction()
		{
			Sequential sequential = new Sequential(1, 6);
			sequential.AddDense(6, true, ActivationType.relu, InitializationType.random01, InitializationType.random01);
			sequential.AddDense(3, false, ActivationType.none, InitializationType.random01);
			if (SeqDefaults.reaction != null)
			{
				Sequential.DeepCopyWeights(SeqDefaults.reaction, sequential);				
			}
			return sequential;
		}
	}
}

