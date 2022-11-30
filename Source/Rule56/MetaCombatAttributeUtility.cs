using System;
using Verse;

namespace CombatAI
{
	public static class MetaCombatAttributeUtility
	{
		public static MetaCombatAttribute GetWeaknessAttributes(this Pawn pawn)
		{
			MetaCombatAttribute result = MetaCombatAttribute.None;
			if (pawn == null)
			{
				return result;
			}
			DamageReport report = DamageUtility.GetDamageReport(pawn);
			if (pawn.CurrentEffectiveVerb?.IsMeleeAttack ?? true)
			{
				//
				//result |= MetaCombatAttribute.Ranged;				
				result |= MetaCombatAttribute.Ranged_AOEWeaponLarge;
			}
			else
			{
				result |= MetaCombatAttribute.Melee;
			}
			if (pawn.RaceProps.IsMechanoid)
			{
				result |= MetaCombatAttribute.Emp;
			}
			return result;
		}
	}
}

