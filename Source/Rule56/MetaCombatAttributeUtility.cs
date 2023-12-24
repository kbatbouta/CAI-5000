using System.Collections.Generic;
using Verse;
namespace CombatAI
{
    public static class MetaCombatAttributeUtility
    {
        private static readonly Dictionary<ThingDef, MetaCombatAttribute>    w_race = new Dictionary<ThingDef, MetaCombatAttribute>();
        private static readonly Dictionary<PawnKindDef, MetaCombatAttribute> w_kind = new Dictionary<PawnKindDef, MetaCombatAttribute>();
        private static readonly Dictionary<int, MetaCombatAttribute>         w_pawn = new Dictionary<int, MetaCombatAttribute>();
        private static readonly Dictionary<ThingDef, MetaCombatAttribute>    s_race = new Dictionary<ThingDef, MetaCombatAttribute>();
        private static readonly Dictionary<PawnKindDef, MetaCombatAttribute> s_kind = new Dictionary<PawnKindDef, MetaCombatAttribute>();
        private static readonly Dictionary<int, MetaCombatAttribute>         s_pawn = new Dictionary<int, MetaCombatAttribute>();

        public static MetaCombatAttribute GetWeaknessAttributes(this Pawn pawn)
        {
            MetaCombatAttribute result = MetaCombatAttribute.None;
            if (pawn == null)
            {
                return result;
            }
            DamageReport report = DamageUtility.GetDamageReport(pawn);
            if (!report.primaryIsRanged)
            {
                result |= MetaCombatAttribute.AOELarge;
            }
            else
            {
                result |= MetaCombatAttribute.Melee;
            }
            if (!w_pawn.TryGetValue(pawn.thingIDNumber, out MetaCombatAttribute extras))
            {
                if (!w_kind.TryGetValue(pawn.kindDef, out MetaCombatAttribute kindExtras))
                {
                    kindExtras           = pawn.kindDef.HasModExtension<PawnKindDefExtension>() ? pawn.kindDef.GetModExtension<PawnKindDefExtension>().WeakCombatAttribute : MetaCombatAttribute.None;
                    w_kind[pawn.kindDef] = kindExtras;
                }
                extras |= kindExtras;
                if (!w_race.TryGetValue(pawn.def, out MetaCombatAttribute raceExtras))
                {
                    raceExtras       = pawn.def.HasModExtension<PawnDefExtension>() ? pawn.kindDef.GetModExtension<PawnDefExtension>().WeakCombatAttribute : MetaCombatAttribute.None;
                    w_race[pawn.def] = raceExtras;
                }
                extras                     |= raceExtras;
                w_pawn[pawn.thingIDNumber] =  extras;
            }
            return result | extras;
        }

        public static MetaCombatAttribute GetStrongAttributes(this Pawn pawn)
        {
            MetaCombatAttribute result = MetaCombatAttribute.None;
            if (pawn == null)
            {
                return result;
            }
            if (!s_pawn.TryGetValue(pawn.thingIDNumber, out MetaCombatAttribute extras))
            {
                if (!s_kind.TryGetValue(pawn.kindDef, out MetaCombatAttribute kindExtras))
                {
                    kindExtras           = pawn.kindDef.HasModExtension<PawnKindDefExtension>() ? pawn.kindDef.GetModExtension<PawnKindDefExtension>().StrongCombatAttribute : MetaCombatAttribute.None;
                    s_kind[pawn.kindDef] = kindExtras;
                }
                extras |= kindExtras;
                if (!s_race.TryGetValue(pawn.def, out MetaCombatAttribute raceExtras))
                {
                    raceExtras       = pawn.def.HasModExtension<PawnDefExtension>() ? pawn.kindDef.GetModExtension<PawnDefExtension>().StrongCombatAttribute : MetaCombatAttribute.None;
                    s_race[pawn.def] = raceExtras;
                }
                extras                     |= raceExtras;
                s_pawn[pawn.thingIDNumber] =  extras;
            }
            return result | extras;
        }

        public static MetaCombatAttribute Sum(this List<MetaCombatAttribute> attributes)
        {
            if (attributes == null)
            {
                return MetaCombatAttribute.None;
            }
            MetaCombatAttribute result = MetaCombatAttribute.None;
            for (int i = 0; i < attributes.Count; i++)
            {
                result |= attributes[i];
            }
            return result;
        }

        public static MetaCombatAttribute Sum(this IEnumerable<MetaCombatAttribute> attributes)
        {
            if (attributes == null)
            {
                return MetaCombatAttribute.None;
            }
            MetaCombatAttribute result = MetaCombatAttribute.None;
            foreach (MetaCombatAttribute attribute in attributes)
            {
                result |= attribute;
            }
            return result;
        }

        public static void ClearCache()
        {
	        w_pawn.Clear();
	        s_pawn.Clear();
        }
    }
}
