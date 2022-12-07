using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CombatAI
{
	public struct DamageReport
	{
		private static Dictionary<ToolCapacityDef, List<ManeuverDef>> _maneuvers = new Dictionary<ToolCapacityDef, List<ManeuverDef>>(128);

		private int  _createdAt;
		private bool _finalized;

		/// <summary>
		/// Report thing.
		/// </summary>
		public Thing thing;
		/// <summary>
		/// Ranged blunt damage potential per second.
		/// </summary>
		public float rangedBlunt;
		/// <summary>
		/// Ranged sharp damage potential per second.
		/// </summary>
		public float rangedSharp;
		/// <summary>
		/// Ranged blunt armor penetration potential.
		/// </summary>
		public float rangedBluntAp;
		/// <summary>
		/// Ranged sharp armor penetration potential.
		/// </summary>
		public float rangedSharpAp;
		/// <summary>
		/// Whether report thing can melee.
		/// </summary>
		public bool canMelee;
		/// <summary>
		/// Melee blunt damage potential per second.
		/// </summary>
		public float meleeBlunt;
		/// <summary>
		/// Melee sharp damage potential per second.
		/// </summary>
		public float meleeSharp;
		/// <summary>
		/// Melee blunt armor penetration potential.
		/// </summary>
		public float meleeBluntAp;
		/// <summary>
		/// Melee sharp armor penetration potential.
		/// </summary>
		public float meleeSharpAp;
		/// <summary>
		/// Report meta flags.
		/// </summary>
		public MetaCombatAttribute attributes;
		/// <summary>
		/// Whether the primary damage model is ranged.
		/// </summary>
		public bool primaryIsRanged;
		/// <summary>
		/// The combined and the adjusted sharp value. 
		/// </summary>
		public float adjustedSharp;
		/// <summary>
		/// The combined and the adjusted blunr value. 
		/// </summary>
		public float adjustedBlunt;
		/// <summary>
		/// Primary verb properties.
		/// </summary>
		public VerbProperties primaryVerbProps;
		/// <summary>
		/// Whether this is valid report or not.
		/// </summary>		
		public bool IsValid
		{
			get => _finalized && GenTicks.TicksGame - _createdAt < 1800;
		}

		public float GetAdjustedDamage(DamageArmorCategoryDef def)
		{
			if (def == null)
			{
				return adjustedSharp * 0.6f + adjustedBlunt * 0.4f;
			}
			else if (def == DamageArmorCategoryDefOf.Sharp)
			{
				return adjustedSharp;
			}
			else
			{
				return adjustedBlunt;
			}
		}

		public void Finalize(float rangedMul, float meleeMul)
		{
			float mainSharp;
			float mainBlunt;
			float weakSharp;
			float weakBlunt;
			if (primaryIsRanged)
			{
				mainSharp = Adjust(rangedSharp, rangedSharpAp) * rangedMul;
				mainBlunt = Adjust(rangedBlunt, rangedBluntAp) * rangedMul;
				weakSharp = Adjust(meleeSharp, meleeSharpAp) * meleeMul;
				weakBlunt = Adjust(meleeBlunt, meleeBluntAp) * meleeMul;
			}
			else
			{
				mainSharp = Adjust(meleeSharp, meleeSharpAp) * meleeMul;
				mainBlunt = Adjust(meleeBlunt, meleeBluntAp) * meleeMul;
				weakSharp = Adjust(rangedSharp, rangedSharpAp) * rangedMul;
				weakBlunt = Adjust(rangedBlunt, rangedBluntAp) * rangedMul;
			}
			adjustedSharp = mainSharp * 0.95f + weakSharp * 0.05f;
			adjustedBlunt = mainBlunt * 0.95f + weakBlunt * 0.05f;
			_createdAt    = GenTicks.TicksGame;
			_finalized    = true;
		}

		public void AddVerb(Verb verb)
		{
			if (verb != null && verb.Available())
			{
				if (verb.IsEMP())
				{
					attributes |= MetaCombatAttribute.Emp;
				}
				if (!verb.IsMeleeAttack)
				{
					ProjectileProperties projectile = verb.GetProjectile()?.projectile ?? null;
					if (projectile != null)
					{
						if (projectile.explosionRadius > 0)
						{
							attributes |= MetaCombatAttribute.AOE;
							if (projectile.explosionRadius > 3.5f)
							{
								attributes |= MetaCombatAttribute.AOELarge;
							}
						}
						float warmupTime     = Maths.Max(verb.verbProps.warmupTime, 0.5f);
						float burstShotCount = verb.verbProps.burstShotCount;
						float output         = 1f / warmupTime * burstShotCount;
						if (projectile.damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
						{
							rangedSharp   = (rangedSharp + output * projectile.damageAmountBase) / 2f;
							rangedSharpAp = rangedSharpAp != 0 ? (rangedSharpAp + Maths.Max(GetArmorPenetration(projectile), 0f)) / 2f : Maths.Max(GetArmorPenetration(projectile), 0f);
						}
						else
						{
							rangedBlunt   = (rangedBlunt + output * projectile.damageAmountBase) / 2f;
							rangedBluntAp = rangedBluntAp != 0 ? (rangedBluntAp + Maths.Max(GetArmorPenetration(projectile), 0f)) / 2f : Maths.Max(GetArmorPenetration(projectile), 0f);
						}
					}
				}
				else
				{
					AddTool(verb.tool);
				}
			}
		}

		public void AddTool(Tool tool)
		{
			if (tool != null && tool.capacities != null)
			{
				for (int i = 0; i < tool.capacities.Count; i++)
				{
					ToolCapacityDef def = tool.capacities[i];
					if (!_maneuvers.TryGetValue(def, out List<ManeuverDef> maneuvers))
					{
						_maneuvers[def] = maneuvers = new List<ManeuverDef>(def.Maneuvers);
					}
					for (int j = 0; j < maneuvers.Count; j++)
					{
						ManeuverDef maneuver = maneuvers[j];
						if (maneuver.verb != null)
						{
							if (maneuver.verb.meleeDamageDef == DamageDefOf.Blunt)
							{
								meleeBlunt   = (meleeBlunt + tool.power) / 2f;
								meleeBluntAp = (meleeBluntAp + Maths.Max(maneuver.verb.meleeArmorPenetrationBase, tool.armorPenetration, 0)) / 2f;
							}
							else
							{
								meleeSharp   = (meleeSharp + tool.power) / 2f;
								meleeSharpAp = (meleeSharpAp + Maths.Max(maneuver.verb.meleeArmorPenetrationBase, tool.armorPenetration, 0)) / 2f;
							}
						}
					}
				}
			}
		}

		private static float Adjust(float dmg, float ap)
		{
			if (Mod_CE.active)
			{
				return dmg / 18f + ap;
			}
			else
			{
				if (ap != 0)
				{
					return dmg / 12f * ap;
				}
				else
				{
					return dmg / 18f;
				}
			}
		}

		private float AdjustedSharp()
		{
			float damage;
			float ap;
			if (primaryIsRanged)
			{
				damage = rangedSharp;
				ap     = rangedSharpAp;
			}
			else
			{
				damage = meleeSharp;
				ap     = meleeSharpAp;
			}
			if (Mod_CE.active)
			{
				return damage / 12f + ap;
			}
			else
			{
				if (ap != 0)
				{
					return damage / 12f * ap;
				}
				else
				{
					return damage / 18f;
				}
			}
		}

		private float AdjustedBlunt()
		{
			float damage;
			float ap;
			if (primaryIsRanged)
			{
				damage = rangedBlunt;
				ap     = rangedBluntAp;
			}
			else
			{
				damage = meleeBlunt;
				ap     = meleeBluntAp;
			}
			if (Mod_CE.active)
			{
				return damage / 12f + ap;
			}
			else
			{
				if (ap != 0)
				{
					return damage / 12f * ap;
				}
				else
				{
					return damage / 18f;
				}
			}
		}

		private float GetArmorPenetration(ProjectileProperties projectile)
		{
			return Mod_CE.GetProjectileArmorPenetration(projectile);
		}
	}
}
