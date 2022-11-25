using System;
using RimWorld;
using Verse;

namespace CombatAI
{
	public abstract class Building_CCTVBase : Building_Turret
	{
		public bool Active => true;

		public Building_CCTVBase()
		{
		}	
	}
}

