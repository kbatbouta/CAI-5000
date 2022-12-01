﻿using System;
namespace CombatAI
{
	public enum MetaCombatAttribute : int
	{
		None		= 0,
		Ranged		= 1,
		AOE			= 2,
		AOELarge	= 6,
		Melee		= 8,
		Emp			= 16,
		Fire		= 32,
		Gas			= 64,
		Explosives	= 128
	}
}
