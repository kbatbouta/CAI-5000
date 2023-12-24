using System;
using System.Collections.Generic;
using CombatAI.Comps;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public static class SightUtility
    {
	    public static float GetSightRadius_Fast(Thing thing)
	    {
		    if (!TKVCache<Thing, SightGrid.ISightRadius, float>.TryGet(thing, out float val, 12000))
		    {
			    TKVCache<Thing, SightGrid.ISightRadius, float>.Put(thing, val = GetSightRadius(thing).sight);
		    }
		    return val;
	    }
	    
	    public static SightGrid.ISightRadius GetSightRadius(Thing thing)
        {
            bool                   isSmartPawn = false;
            SightGrid.ISightRadius result;
            ThingComp_Sighter      sighter = thing.GetComp_Fast<ThingComp_Sighter>();
            if (sighter != null)
            {
                result = GetSightRadius_Sighter(sighter);
                Faction f = thing.Faction;

                if (f != null && (f.IsPlayerSafe() || (!f.HostileTo(Faction.OfPlayer) && Finder.Settings.FogOfWar_Allies)))
                {
                    result.fog = Maths.Max(Mathf.CeilToInt(GetFogRadius(thing, result.sight) * Finder.Settings.FogOfWar_RangeMultiplier), 3);
                }
            }
            else if (thing is Pawn pawn)
            {
                result = GetSightRadius_Pawn(pawn);
                Faction f = thing.Faction;
                isSmartPawn = !pawn.RaceProps.Animal && !(pawn.Dead || pawn.Downed);
                if (f != null && (f.IsPlayerSafe() || (!f.HostileTo(Faction.OfPlayer) && Finder.Settings.FogOfWar_Allies)))
                {
                    if (pawn.RaceProps.Animal)
                    {
                        if (!Finder.Settings.FogOfWar_Animals)
                        {
                            goto finalize;
                        }
                        if (Finder.Settings.FogOfWar_AnimalsSmartOnly && pawn.RaceProps.trainability == TrainabilityDefOf.None)
                        {
                            goto finalize;
                        }
                    }
                    result.fog = Maths.Max(Mathf.CeilToInt(GetFogRadius(thing, result.sight) * Finder.Settings.FogOfWar_RangeMultiplier), 3);
                }
            }
            else if (thing is Building_Turret turret)
            {
                result = GetSightRadius_Turret(turret);
                if (Finder.Settings.FogOfWar_Turrets || turret.GetComp_Fast<CompMannable>() is CompMannable mannable && mannable.MannedNow)
                {
                    Faction f = thing.Faction;

                    if (f != null && (f.IsPlayerSafe() || (!f.HostileTo(Faction.OfPlayer) && Finder.Settings.FogOfWar_Allies)))
                    {
                        result.fog = Maths.Max(Mathf.CeilToInt(GetFogRadius(thing, result.sight) * Finder.Settings.FogOfWar_RangeMultiplier), 3);
                    }
                }
            }
            else
            {
                throw new Exception($"ISMA: GetSightRadius got an object that is niether a pawn, turret nor does it have sighter. {thing}");
            }
        finalize:
            if (isSmartPawn)
            {
                result.scan  = result.scan + 16;
                result.sight = result.sight + 8;
            }
            result.createdAt = GenTicks.TicksGame;
            TKVCache<int, SightGrid.ISightRadius, float>.Put(thing.thingIDNumber, result.sight);
            return result;
        }

        private static SightGrid.ISightRadius GetSightRadius_Pawn(Pawn pawn)
        {
            SightGrid.ISightRadius result = new SightGrid.ISightRadius();
            if (pawn.RaceProps.Animal && pawn.Faction == null)
            {
                result.sight = Mathf.FloorToInt(Mathf.Clamp(pawn.BodySize * 3f, 2f, 10f));
                result.scan  = result.sight;
                return result;
            }
            Verb verb = pawn.TryGetAttackVerb();
            if (verb == null || !verb.Available())
            {
                result.sight = 4;
                result.scan  = 0;
                return result;
            }
            if (pawn.RaceProps.Insect || pawn.RaceProps.IsMechanoid || pawn.RaceProps.Animal)
            {
                if (verb.IsMeleeAttack)
                {
                    result.sight = 10;
                    result.scan  = 0;
                    return result;
                }
                result.scan  = Mathf.CeilToInt(verb.EffectiveRange + 10f);
                result.sight = Maths.Max((int)verb.EffectiveRange, 5);
                return result;
            }
            if (verb.IsMeleeAttack)
            {
                SkillRecord melee = pawn.skills?.GetSkill(SkillDefOf.Melee) ?? null;
                if (melee != null)
                {
                    result.sight = Maths.Max((int)(Mathf.Clamp(melee.Level, 5, 13) * ((pawn.equipment?.Primary?.def.IsMeleeWeapon ?? null) != null ? 1.5f : 0.85f)), 15);
                    result.scan  = Maths.Max(result.sight, 15);
                }
                else
                {
                    result.sight = 5;
                    result.scan  = 0;
                }
                return result;
            }
            result.scan  = Mathf.CeilToInt(verb.EffectiveRange);
            result.sight = Mathf.CeilToInt(verb.EffectiveRange * 0.75f);
            SkillRecord shooting = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
            float       skill    = 5;
            if (shooting != null)
            {
                skill = shooting.Level;
            }
            result.sight = Mathf.CeilToInt(Maths.Max(result.sight * Mathf.Clamp(skill / 7.5f, 0.778f, 1.425f), 4));
            return result;
        }

        private static SightGrid.ISightRadius GetSightRadius_Turret(Building_Turret turret)
        {
            SightGrid.ISightRadius result = new SightGrid.ISightRadius();
            Verb                   verb   = turret.AttackVerb;
            if (verb != null)
            {
                result.sight = Mathf.CeilToInt(verb.EffectiveRange);
                result.scan  = Mathf.CeilToInt(verb.EffectiveRange);
            }
            return result;
        }

        private static SightGrid.ISightRadius GetSightRadius_Sighter(ThingComp_Sighter sighter)
        {
            SightGrid.ISightRadius result = new SightGrid.ISightRadius();
            result.sight = sighter.SightRadius;
            result.scan  = result.sight;
            return result;
        }

        private static float GetFogRadius(Thing thing, float sightRadius)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                CompMannable mannable = thing.GetComp_Fast<CompMannable>();
                if (mannable != null)
                {
                    pawn = mannable.ManningPawn;
                }
            }
            if (pawn != null)
            {
                if (pawn.Downed || GenTicks.TicksGame - pawn.needs?.rest?.lastRestTick < 120)
                {
                    return 3;
                }
                float vision        = Maths.Sqr(pawn.health.capacities?.GetLevel(PawnCapacityDefOf.Sight) ?? 1f);
                float consciousness = Maths.Sqr(pawn.health.capacities?.GetLevel(PawnCapacityDefOf.Consciousness) ?? 1f);
                float hearing       = Mathf.Lerp(0.80f, 1f, pawn.health.capacities?.GetLevel(PawnCapacityDefOf.Hearing) ?? 1f);
                float rest          = Mathf.Lerp(0.40f, 1f, pawn.needs?.rest?.curLevelInt ?? 1f);
                float mul           = Mathf.Clamp(Maths.Min(rest, Maths.Min(hearing, vision, consciousness)) * 0.80f + Maths.Max(rest, Maths.Max(hearing, vision, consciousness)) * 0.20f, 0.15f, 2.5f);
                return Maths.Max(Maths.Max(sightRadius, 10) * mul, 3);
            }
            return sightRadius;
        }

        public static void ClearCache()
        {
        }
    }
}
