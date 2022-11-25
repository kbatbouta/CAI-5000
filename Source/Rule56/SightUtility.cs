using System;
using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;
using System.Collections.ObjectModel;
using NAudio.Utils;
using UnityEngine.UI;

namespace CombatAI
{
    public static class SightUtility
    {                
		private static readonly Dictionary<int, Pair<int, int>> rangeCache = new Dictionary<int, Pair<int, int>>(256);

		public static int GetSightRange(Thing thing)
		{
			return GetSightRange(thing, !(Faction.OfPlayerSilentFail?.HostileTo(thing.Faction) ?? true));
		}

		public static int GetSightRange(Thing thing, bool isPlayer)
		{
			if (rangeCache.TryGetValue(thing.thingIDNumber, out Pair<int, int> store) && GenTicks.TicksGame - store.First <= 600)
			{
				return store.Second;
			}
			int range = 0;
			if (thing is Pawn pawn)
			{				
				if (Finder.Settings.FogOfWar_Enabled && isPlayer)
				{
					range = Mathf.CeilToInt(GetSightRangePlayer(pawn, true) * pawn.GetStatValue(CombatAI.CombatAI_StatDefOf.CombatAI_SightMul));
				}
				else
				{
					range = GetSightRange(pawn);
				}							
			}
			else if (thing is Building_TurretGun turretGun)
			{
				range = GetSightRange(turretGun);
			}
			else if(Mod_CE.active && thing is Building_Turret turret)
			{	
				range = Mathf.CeilToInt(turret.AttackVerb?.EffectiveRange ?? 0f);
			}			
			if (Finder.Settings.FogOfWar_Enabled && isPlayer)
			{
				range = Mathf.CeilToInt(range * Finder.Settings.FogOfWar_RangeMultiplier);
			}	
			rangeCache[thing.thingIDNumber] = new Pair<int, int>(GenTicks.TicksGame, range);
			return range;
		}		

		private static int GetSightRangePlayer(Pawn pawn, bool checkCapcities)
		{
			bool downed = pawn.Downed;
			float multiplier = 1.0f;
			if (checkCapcities)
			{
				float vision = !downed ? (pawn.health.capacities?.GetLevel(PawnCapacityDefOf.Sight) ?? 1f) : 0.2f;
				float hearing = !downed ? (pawn.health.capacities?.GetLevel(PawnCapacityDefOf.Hearing) ?? 1f) : 1.0f;
				multiplier = Maths.Max(vision * hearing, 0.3f);
			}
			if (downed)
			{
				return (int)Maths.Max(5 * multiplier, 3);
			}
			if (GenTicks.TicksGame - pawn.needs?.rest?.lastRestTick < 30)
			{
				return (int)Maths.Max(10 * multiplier, 4);
			}
			if (pawn.RaceProps.Animal || pawn.RaceProps.Insect)
			{
				return (int)Mathf.Clamp(pawn.BodySize * multiplier * 10f, 10, 30);
			}
			else
			{
				Verb verb = pawn.CurrentEffectiveVerb;
				if (verb == null)
				{
					return (int)Maths.Max(15 * multiplier, 12);
				}
				if (verb.IsMeleeAttack)
				{
					SkillRecord melee = pawn.skills?.GetSkill(SkillDefOf.Melee) ?? null;
					if (melee != null && melee.Level > 5)
					{
						multiplier += melee.Level / 20f;
					}
					return (int)Maths.Max(20 * multiplier, 12);
				}
				else
				{
					SkillRecord ranged = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
					if (ranged != null && ranged.Level > 5)
					{
						multiplier += (ranged.Level - 5f) / 15f;
					}
					return (int)Maths.Max(verb.EffectiveRange * multiplier, 20f * multiplier, 10f);
				}
			}
		}

		private static int GetSightRange(Pawn pawn)
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
					return (int)Maths.Max(range * 0.75f, 5f);
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
				range = Maths.Max(range * Mathf.Clamp(skill / 7.5f, 0.778f, 1.425f), 4);
				return Mathf.CeilToInt(range);
			}
		}

		private static int GetSightRange(Building_TurretGun turret)
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
					range = Maths.Max(range * Mathf.Clamp(skill / 7.5f, 0.778f, 1.225f), 5);
				}
			}
			return Mathf.CeilToInt(range);
		}
		
		public static void ClearCache()
        {            
			rangeCache.Clear();			
        }
    }
}

