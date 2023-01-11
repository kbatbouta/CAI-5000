using System;
namespace CombatAI
{
	public static class SeqFactory
	{
		public static Sequential MakeReaction()
		{
			Sequential sequential = new Sequential(1, 6);
			sequential.AddDense(3, true, ActivationType.none, InitializationType.random01, InitializationType.random01);			
			if (SeqDefaults.reaction != null)
			{
				Sequential.DeepCopyWeights(SeqDefaults.reaction, sequential);				
			}
			return sequential;
		}
	}
}

