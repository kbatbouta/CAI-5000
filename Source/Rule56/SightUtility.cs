using System;
using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;
using System.Collections.ObjectModel;

namespace CombatAI
{
    public static class SightUtility
    {
        private static readonly Dictionary<int, Pair<int, int>> rangeCache = new Dictionary<int, Pair<int, int>>(256);
        private static readonly Dictionary<int, Pair<int, float>> moveSpeed = new Dictionary<int, Pair<int, float>>(256);

        public static float GetMoveSpeed(this Pawn pawn)
        {
            if(moveSpeed.TryGetValue(pawn.thingIDNumber, out var store) && GenTicks.TicksGame - store.First <= 240)
            {
                return store.second;
            }
            float speed = pawn.GetStatValue(StatDefOf.MoveSpeed);
            moveSpeed[pawn.thingIDNumber] = new Pair<int, float>(GenTicks.TicksGame, speed);
            return speed;
        }

        public static IntVec3 GetMovingShiftedPosition(this Pawn pawn, float ticksAhead)
        {
            PawnPath path;
            if (!(pawn.pather?.moving ?? false) || (path = pawn.pather.curPath) == null || path.NodesLeftCount <= 1)
            {
                return pawn.Position;
            }

            float distanceTraveled = Mathf.Min(pawn.GetMoveSpeed() * ticksAhead / 60f, path.NodesLeftCount - 1);
            return path.Peek(Mathf.FloorToInt(distanceTraveled));
        }

        public static int GetSightRange(Thing thing)
        {
            if (rangeCache.TryGetValue(thing.thingIDNumber, out Pair<int, int> store) && GenTicks.TicksGame - store.First <= 60)
            {
                return store.second;
            }
            if (thing is Pawn pawn)
            {
                int range = GetSightRange(pawn);
                rangeCache[thing.thingIDNumber] = new Pair<int, int>(GenTicks.TicksGame, range);
                return range;                
            }
            else if (thing is Building_TurretGun turret)
            {
                int range = GetSightRange(turret);
                rangeCache[thing.thingIDNumber] = new Pair<int, int>(GenTicks.TicksGame, range);
                return range;                
            }
            throw new NotImplementedException();
        }

        public static int GetSightRange(Pawn pawn)
        {
            if (pawn.RaceProps.Animal && pawn.Faction == null)
            {
                return Mathf.FloorToInt(Mathf.Clamp(pawn.BodySize * 3f, 2f, 10f));
            }
            Verb verb = pawn.equipment?.PrimaryEq?.PrimaryVerb ?? null;
            if (verb == null || !verb.Available())
            {
                verb = pawn.verbTracker?.AllVerbs.Where(v => v.Available()).MaxBy(v => v.IsMeleeAttack ? 0 : v.EffectiveRange) ?? null;
            }
            if (verb == null)
            {
                return 4;
            }
            float range;
            if (pawn.RaceProps.Insect || pawn.RaceProps.IsMechanoid || pawn.RaceProps.Animal)
            {
                if (verb.IsMeleeAttack)
                {
                    return 10;
                }
                if ((range = verb.EffectiveRange) > 2.5f)
                {
                    return (int)Mathf.Max(range * 0.75f, 5f);
                }
                return 4;
            }
            if (verb.IsMeleeAttack)
            {
                SkillRecord melee = pawn.skills?.GetSkill(SkillDefOf.Melee) ?? null;
                if (melee != null)
                {
                    float meleeSkill = melee.Level;
                    return (int)(Mathf.Clamp(meleeSkill, 5, 13) * ((pawn.equipment?.Primary?.def.IsMeleeWeapon ?? null) != null ? 1.5f : 0.85f));
                }
                return 4;
            }
            else
            {
                range = verb.EffectiveRange * 0.75f;
                SkillRecord shooting = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
                float skill = 5;
                if (shooting != null)
                {
                    skill = shooting.Level;
                }
                range = Mathf.Max(range * Mathf.Clamp(skill / 7.5f, 0.778f, 1.425f), 4);
                return Mathf.CeilToInt(range);
            }
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

        public static void ClearCache()
        {
            moveSpeed.Clear();
            rangeCache.Clear();
        }
    }
}

