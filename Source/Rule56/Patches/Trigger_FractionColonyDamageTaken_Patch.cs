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
				minDamage                   *= 10f;
				desiredColonyDamageFraction =  Maths.Max(Rand.Range(0.25f, 0.80f), desiredColonyDamageFraction);
			}
		}
	}
}
