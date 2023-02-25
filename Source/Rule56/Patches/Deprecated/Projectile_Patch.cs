namespace CombatAI.Patches
{
    public static class Projectile_Patch
    {
        //[HarmonyPatch(typeof(Projectile), nameof(Projectile.Tick))]
        //static class Projectile_Tick_Patch
        //{
        //    public static void Prefix(Projectile __instance)
        //    {
        //        if (__instance.IsHashIntervalTick(6))
        //        {
        //            //__instance.Map.GetComp_Fast<AvoidanceTracker>().Notify_Bullet(__instance.Position);
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(Projectile), nameof(Projectile.Impact))]
        //static class Projectile_Impact_Patch
        //{
        //    public static void Prefix(Projectile __instance)
        //    {
        //        if (__instance.Spawned)
        //        {
        //            //__instance.Map.GetComp_Fast<AvoidanceTracker>().Notify_Bullet(__instance.Position);
        //        }                
        //    }
        //}
    }
}
