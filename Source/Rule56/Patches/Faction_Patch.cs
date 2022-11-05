using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatAI.Patches
{
    public static class Faction_Patch
    {
        [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberTookDamage))]
        static class Faction_Notify_MemberTookDamage_Patch
        {
            public static void Prefix(Pawn member, DamageInfo dinfo)
            {
                if (member?.Spawned ?? false)
                {
                    member.Map.GetComp_Fast<AvoidanceTracker>().Notify_Injury(member, member.Position);
                }
            }
        }

        [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberDied))]
        static class Faction_Notify_MemberDied_Patch
        {
            public static void Prefix(Pawn member, bool wasWorldPawn, bool wasGuilty)
            {
                if (!wasWorldPawn && (member?.Spawned ?? false))
                {                    
                    member.Map.GetComp_Fast<AvoidanceTracker>().Notify_Death(member, member.Position);
                }
            }
        }       
    }
}

