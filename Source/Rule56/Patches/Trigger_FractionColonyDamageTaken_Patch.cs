using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class Trigger_FractionColonyDamageTaken_Patch
    {
        [HarmonyPatch(typeof(Trigger_FractionColonyDamageTaken), MethodType.Constructor, typeof(float), typeof(float))]
        private static class Trigger_FractionColonyDamageTaken_Constructor
        {
            public static void Prefix(ref float desiredColonyDamageFraction, ref float minDamage)
            {
                minDamage                   *= 15f;
                desiredColonyDamageFraction =  Maths.Max(Rand.Range(0.75f, 5.0f), desiredColonyDamageFraction);
            }
        }

        [HarmonyPatch(typeof(Trigger_FractionColonyDamageTaken), nameof(Trigger_FractionColonyDamageTaken.ActivateOn))]
        private static class Trigger_FractionColonyDamageTaken_ActivateOn
        {
            public static void Postfix(ref bool __result)
            {
                if (__result && !Rand.Chance(0.0001f))
                {
                    __result = false;
                }
            }
        }
    }
}
