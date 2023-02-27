using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class Faction_Patch
    {
        [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_RelationKindChanged))]
        private static class Faction_Notify_Notify_RelationKindChanged_Patch
        {
            public static void Prefix(Faction __instance, Faction other)
            {
                if (other.IsPlayerSafe())
                {
                    foreach (Map map in Find.Maps)
                    {
                        map.GetComp_Fast<SightTracker>()?.Notify_PlayerRelationChanged(__instance);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberTookDamage))]
        private static class Faction_Notify_MemberTookDamage_Patch
        {
            public static void Prefix(Pawn member, DamageInfo dinfo)
            {
                if (member?.Spawned ?? false)
                {
                    if (!member.Dead && !member.Downed)
                    {
                        member.AI()?.Notify_TookDamage(dinfo);
                    }
                    member.Map?.GetComp_Fast<AvoidanceTracker>()?.Notify_Injury(member, dinfo);
                    member.GetComp_Fast<ThingComp_Statistics>()?.Notify_PawnTookDamage();
                }
            }
        }

        [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberDied))]
        private static class Faction_Notify_MemberDied_Patch
        {
            public static void Prefix(Pawn member, bool wasWorldPawn, bool wasGuilty)
            {
                if (!wasWorldPawn && (member?.Spawned ?? false))
                {
                    member.Map.GetComp_Fast<AvoidanceTracker>()?.Notify_Death(member, member.Position);
                }
            }
        }
    }
}
