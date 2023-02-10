using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public class Pawn_AbilityTracker_Patch
    {
        [HarmonyPatch(typeof(Pawn_AbilityTracker), nameof(Pawn_AbilityTracker.ExposeData))]
        public static class Pawn_AbilityTracker_ExposeData_Patch
        {
            public static Exception Finalizer(Pawn_AbilityTracker __instance, Exception __exception)
            {
                if (__exception != null && __instance.abilities == null)
                {
                    Log.Warning($"ISMA: Fixed loading Pawn_AbilityTracker for {__instance.pawn}");
                    __instance.abilities               = new List<Ability>();
                    __instance.allAbilitiesCachedDirty = true;
                }
                return null;
            }
        }
    }
}
