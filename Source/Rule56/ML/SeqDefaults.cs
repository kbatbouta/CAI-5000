using System;
namespace CombatAI
{
	public static class SeqDefaults
	{
		public static Sequential reaction;

		static SeqDefaults()
		{
			reaction = SeqFactory.MakeReaction();
		}
	}
}

