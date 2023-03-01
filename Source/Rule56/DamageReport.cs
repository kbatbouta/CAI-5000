using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public struct DamageReport
    {
        private static readonly Dictionary<ToolCapacityDef, List<ManeuverDef>> _maneuvers = new Dictionary<ToolCapacityDef, List<ManeuverDef>>(128);

        private int  _createdAt;
        private bool _finalized;

        /// <summary>
        ///     Report thing.
        /// </summary>
        public Thing thing;
        /// <summary>
        ///     Ranged blunt damage potential per second.
        /// </summary>
        public float rangedBlunt;
        /// <summary>
        ///     Ranged sharp damage potential per second.
        /// </summary>
        public float rangedSharp;
        /// <summary>
        ///     Ranged blunt armor penetration potential.
        /// </summary>
        public float rangedBluntAp;
        /// <summary>
        ///     Ranged sharp armor penetration potential.
        /// </summary>
        public float rangedSharpAp;
        /// <summary>
        ///     Whether report thing can melee.
        /// </summary>
        public bool canMelee;
        /// <summary>
        ///     Melee blunt damage potential per second.
        /// </summary>
        public float meleeBlunt;
        /// <summary>
        ///     Melee sharp damage potential per second.
        /// </summary>
        public float meleeSharp;
        /// <summary>
        ///     Melee blunt armor penetration potential.
        /// </summary>
        public float meleeBluntAp;
        /// <summary>
        ///     Melee sharp armor penetration potential.
        /// </summary>
        public float meleeSharpAp;
        /// <summary>
        ///     Report meta flags.
        /// </summary>
        public MetaCombatAttribute attributes;
        /// <summary>
        ///     Whether the primary damage model is ranged.
        /// </summary>
        public bool primaryIsRanged;
        /// <summary>
        ///     The combined and the adjusted sharp value.
        /// </summary>
        public float adjustedSharp;
        /// <summary>
        ///     The combined and the adjusted blunr value.
        /// </summary>
        public float adjustedBlunt;
        /// <summary>
        ///     Primary verb properties.
        /// </summary>
        public VerbProperties primaryVerbProps;
        /// <summary>
        ///     Damage def for the primary attack verb.
        /// </summary>
        public DamageDef primaryVerbDamageDef;

        public bool IsValid
        {
            get => _finalized && GenTicks.TicksGame - _createdAt < 60000;
        }

        public float GetAdjustedDamage(DamageArmorCategoryDef def)
        {
            if (def == null)
            {
                return adjustedSharp * 0.6f + adjustedBlunt * 0.4f;
            }
            if (def == DamageArmorCategoryDefOf.Sharp)
            {
                return adjustedSharp;
            }
            return adjustedBlunt;
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
                        float burstShotCount = Mathf.Clamp(verb.verbProps.burstShotCount * 0.66f, 0.75f, 3f);
                        if (projectile.damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
                        {
                            rangedSharp = Maths.Max(projectile.damageAmountBase * burstShotCount, rangedSharp);
                            ;
                            rangedSharpAp = Maths.Max(GetArmorPenetration(projectile), rangedSharpAp);
                        }
                        else
                        {
                            rangedBlunt   = Maths.Max(projectile.damageAmountBase * burstShotCount, rangedBlunt);
                            rangedBluntAp = Maths.Max(GetArmorPenetration(projectile), rangedBluntAp);
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

        public float SimulatedDamage(ArmorReport armorReport, int iterations = 5)
        {
            float damage = 0f;
//			bool  hasWorkingShield = includeShields && armorReport.shield?.PawnOwner != null;
            for (int i = 0; i < iterations; i++)
            {
                damage += SimulatedDamage_Internal(armorReport, (i + 1f) / (iterations + 2f));
//				if (!hasWorkingShield || armorReport.shield.Energy - damage * armorReport.shield.Props.energyLossPerDamage <= 0)
//				{
//					damage += temp;
//				}
            }
            return damage / iterations;
        }

        private float SimulatedDamage_Internal(ArmorReport armorReport, float roll)
        {
            DamageDef              damageDef = primaryVerbDamageDef ?? (primaryIsRanged ? DamageDefOf.Bullet : DamageDefOf.Bullet);
            DamageArmorCategoryDef category  = damageDef?.armorCategory ?? DamageArmorCategoryDefOf.Sharp;
            float                  damage    = 0f;
            float                  armorPen  = 0f;
            if (category == DamageArmorCategoryDefOf.Sharp)
            {
                Sharp(out damage, out armorPen);
            }
            else
            {
                Blunt(out damage, out armorPen);
            }
            ApplyDamage(ref damage, armorPen, armorReport.GetArmor(damageDef), ref damageDef, roll);
            if (damage > 0.01f)
            {
                ApplyDamage(ref damage, armorPen, armorReport.GetBodyArmor(damageDef), ref damageDef, roll);
            }
            return damage;
        }

        private void ApplyDamage(ref float damageAmount, float armorPenetration, float armorRating, ref DamageDef damageDef, float roll)
        {
            float pen     = Mathf.Max(armorRating - armorPenetration, 0f);
            float blocked = pen * 0.5f;
            float reduced = pen;
            if (roll < blocked)
            {
                // stopped.
                damageAmount = 0f;
            }
            else if (roll < reduced)
            {
                // reduced enough to become blunt damage.
                damageAmount = damageAmount / 2f;
                if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
                {
                    damageDef = DamageDefOf.Blunt;
                }
            }
        }

        private static float Adjust(float dmg, float ap)
        {
            if (Mod_CE.active)
            {
                return dmg / 18f + ap;
            }
            if (ap != 0)
            {
                return dmg / 12f * ap;
            }
            return dmg / 18f;
        }

        private void Sharp(out float damage, out float ap)
        {
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
        }

        private void Blunt(out float damage, out float ap)
        {
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
            if (ap != 0)
            {
                return damage / 12f * ap;
            }
            return damage / 18f;
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
            if (ap != 0)
            {
                return damage / 12f * ap;
            }
            return damage / 18f;
        }

        private float GetArmorPenetration(ProjectileProperties projectile)
        {
            return Mod_CE.GetProjectileArmorPenetration(projectile);
        }
    }
}
