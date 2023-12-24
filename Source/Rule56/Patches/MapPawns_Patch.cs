using HarmonyLib;
using Verse;
namespace CombatAI.Patches
{
    public static class MapPawns_Patch
    {
        [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.RegisterPawn))]
        public static class MapPawns_RegisterPawn_Patch
        {
            public static void Prefix(MapPawns __instance, Pawn p)
            {
                __instance.map.GetComp_Fast<SightTracker>().Register(p);
                __instance.map.GetComp_Fast<AvoidanceTracker>().Register(p);
            }
        }

        [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.DeRegisterPawn))]
        public static class MapPawns_DeRegisterPawn_Patch
        {
            public static void Prefix(MapPawns __instance, Pawn p)
            {
                __instance.map.GetComp_Fast<SightTracker>().DeRegister(p);
                __instance.map.GetComp_Fast<AvoidanceTracker>().DeRegister(p);
                CacheUtility.ClearThingCache(p);
            }
        }
    }
}
