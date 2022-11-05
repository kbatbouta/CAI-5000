using System;
using HarmonyLib;
using Verse;

namespace CombatAI.Patches
{
    public static class Game_Patch
    {
        [HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
        static class Game_DeinitAndRemoveMap_Patch
        {
            public static void Prefix(Map map)
            {
                CompsCache.Notify_MapRemoved(map);
            }
        }
    }
}

