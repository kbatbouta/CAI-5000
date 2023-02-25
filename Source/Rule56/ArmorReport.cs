using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public struct ArmorReport
    {
        /// <summary>
        ///     Report pawn.
        /// </summary>
        public Pawn pawn;
        /// <summary>
        ///     Pawn natural blunt armor rating.
        /// </summary>
        public float bodyBlunt;
        /// <summary>
        ///     Pawn natural sharp armor rating.
        /// </summary>
        public float bodySharp;
        /// <summary>
        ///     Apparel blunt armor rating.
        /// </summary>
        public float apparelBlunt;
        /// <summary>
        ///     Apparel sharp armor rating.
        /// </summary>
        public float apparelSharp;
        /// <summary>
        ///     Pawn bodysize.
        /// </summary>
        public float bodySize;
        /// <summary>
        ///     Whether the pawn has a shield belt.
        /// </summary>
        public bool hasShieldBelt;
        /// <summary>
        ///     Whether the can die.
        /// </summary>
        public bool immortal;
        /// <summary>
        ///     Creation tick.
        /// </summary>
        public int createdAt;
        /// <summary>
        ///     Comp shield for the shield belt.
        /// </summary>
        public CompShield shield;
        /// <summary>
        ///     Weak attributes.
        /// </summary>
        public MetaCombatAttribute weaknessAttributes;
        /// <summary>
        ///     Total sharp armor.
        /// </summary>
        public float Sharp
        {
            get => bodySharp + apparelSharp;
        }
        /// <summary>
        ///     Total blunt armor.
        /// </summary>
        public float Blunt
        {
            get => bodyBlunt + apparelBlunt;
        }
        /// <summary>
        ///     How tanky is this pawn.
        /// </summary>
        public float TankInt
        {
            get => Mathf.Lerp(0f, 1f, (bodyBlunt * 0.5f + bodySharp * 0.5f + apparelBlunt * 0.5f + apparelSharp * 0.5f) / 38f);
        }
        /// <summary>
        ///     Whether this is a valid report.
        /// </summary>
        public bool IsValid
        {
            get => createdAt != 0;
        }

        /// <summary>
        ///     Get the appropriate armor for a damage def.
        /// </summary>
        /// <param name="damage">Damage def</param>
        /// <returns>Armor value</returns>
        public float GetArmor(DamageDef damage)
        {
            return damage != null ? damage.armorCategory == DamageArmorCategoryDefOf.Sharp ? Sharp : Blunt : 0f;
        }

        /// <summary>
        ///     Get the appropriate armor for a damage def.
        /// </summary>
        /// <param name="damage">Damage def</param>
        /// <returns>Armor value</returns>
        public float GetBodyArmor(DamageDef damage)
        {
            return damage != null ? damage.armorCategory == DamageArmorCategoryDefOf.Sharp ? bodySharp : bodyBlunt : 0f;
        }

        /// <summary>
        ///     Get the appropriate armor for a damage def.
        /// </summary>
        /// <param name="damage">Damage def</param>
        /// <returns>Armor value</returns>
        public float GetArmor(DamageArmorCategoryDef category)
        {
            return category != null ? category == DamageArmorCategoryDefOf.Sharp ? Sharp : Blunt : 0f;
        }
    }
}
