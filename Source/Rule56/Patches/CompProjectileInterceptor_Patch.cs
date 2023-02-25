using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class CompProjectileInterceptor_Patch
    {
        [HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.CompTick))]
        private static class CompProjectileInterceptor_CompTick_Patch
        {
            public static void Postfix(CompProjectileInterceptor __instance)
            {
                if ((__instance.parent?.IsHashIntervalTick(30) ?? false) && !__instance.parent.Destroyed && __instance.parent.Spawned && __instance.Active)
                {
                    __instance.parent.Map.GetComp_Fast<MapComponent_CombatAI>().interceptors.TryRegister(__instance);
                }
            }
        }
    }
}
