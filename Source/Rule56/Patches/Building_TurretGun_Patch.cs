using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatAI.Patches
{
    public static class Building_TurretGun_Patch
    {
        [HarmonyPatch(typeof(Building_TurretGun), nameof(Building_TurretGun.SpawnSetup))]
        static class Building_TurretGun_SpawnSetup_Patch
        {
            public static void Postfix(Building_TurretGun __instance)
            {
                __instance.Map.GetComp_Fast<TurretTracker>().Register(__instance);
            }
        }

        [HarmonyPatch(typeof(Building_TurretGun), nameof(Building_TurretGun.DeSpawn))]
        static class Building_TurretGun_DeSpawn_Patch
        {
            public static void Prefix(Building_TurretGun __instance)
            {
                __instance.Map.GetComp_Fast<TurretTracker>().DeRegister(__instance);
            }
        }
    }
}

