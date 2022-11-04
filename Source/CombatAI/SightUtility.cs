using System;
using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;

namespace CombatAI
{
    public static class SightUtility
    {
        public static int GetSightRange(Thing thing)
        {
            if (thing is Pawn pawn)
            {
                return GetSightRange(pawn);
            }
            else if (thing is Building_TurretGun turret)
            {
                return GetSightRange(turret);
            }
            throw new NotImplementedException();
        }

        public static int GetSightRange(Pawn pawn)
        {
            if (pawn.RaceProps.Animal && pawn.Faction == null)
            {
                return Mathf.FloorToInt(Mathf.Clamp(pawn.BodySize * 2f, 2f, 8f));
            }
            Verb verb = pawn.equipment?.PrimaryEq?.PrimaryVerb ?? null;
            if (verb == null || !verb.Available())
            {
                verb = pawn.verbTracker?.AllVerbs.Where(v => v.Available()).MaxBy(v => v.IsMeleeAttack ? 0 : v.EffectiveRange) ?? null;
            }
            if (verb == null)
            {
                return -1;
            }
            float range;
            if (pawn.RaceProps.Insect || pawn.RaceProps.IsMechanoid || pawn.RaceProps.Animal)
            {
                if (verb.IsMeleeAttack)
                {
                    return 15;
                }
                if ((range = verb.EffectiveRange) > 2.5f)
                {
                    return (int)Mathf.Max(range * 0.75f, 5f);
                }
                return -1;
            }
            if (verb.IsMeleeAttack)
            {
                SkillRecord melee = pawn.skills?.GetSkill(SkillDefOf.Melee) ?? null;
                if (melee != null)
                {
                    float skill = melee.Level;
                    return (int)Mathf.Clamp(skill, 4, 15) * 2;
                }
                return 5;
            }
            if ((range = verb.EffectiveRange * 0.75f) > 2.5f)
            {
                SkillRecord shooting = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
                float skill = 5;
                if (shooting != null)
                {
                    skill = shooting.Level;
                }
                range = Mathf.Max(range * Mathf.Clamp(skill / 7.5f, 0.778f, 1.425f), 5);
                return Mathf.CeilToInt(range);
            }
            return -1;
        }

        public static int GetSightRange(Building_TurretGun turret)
        {
            float range = turret.CurrentEffectiveVerb?.EffectiveRange ?? -1;            
            if (range != 0 && turret.IsMannable)
            {
                Pawn user = turret.mannableComp.ManningPawn;
                if (user != null)
                {
                    SkillRecord shooting = user.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
                    float skill = 5;
                    if (shooting != null)
                    {
                        skill = shooting.Level;
                    }
                    range = Mathf.Max(range * Mathf.Clamp(skill / 7.5f, 0.778f, 1.225f), 5);
                }
            } 
            return Mathf.CeilToInt(range);
        }
    }
}

