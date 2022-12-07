using System;
using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;
using NAudio.Utils;
using UnityEngine.UI;
using CombatAI.Comps;

namespace CombatAI
{
    public static class SightUtility
    {		
		public static SightGrid.ISightRadius GetSightRadius(Thing thing)
		{
			SightGrid.ISightRadius result;
			ThingComp_Sighter sighter = thing.GetComp_Fast<ThingComp_Sighter>();
			if (sighter != null)
			{
				result = GetSightRadius_Sighter(sighter);				
			}
			else if (thing is Pawn pawn)
			{
				result = GetSightRadius_Pawn(pawn);				
			}
			else if (thing is Building_Turret turret)
			{
				result = GetSightRadius_Turret(turret);				
			}
			else
			{
				throw new Exception($"ISMA: GetSightRadius got an object that is niether a pawn, turret nor does it have sighter. {thing}");
			}
			Faction f = thing.Faction;
			if (f != null && (f.IsPlayerSafe() || f.HostileTo(Faction.OfPlayer)))
			{
				result.fog = Maths.Max((Mathf.CeilToInt(GetFogRadius(thing, result.sight) *  Finder.Settings.FogOfWar_RangeMultiplier)), 3);
			}
			result.createdAt = GenTicks.TicksGame;
			return result;
		}

		private static SightGrid.ISightRadius GetSightRadius_Pawn(Pawn pawn)
		{
			SightGrid.ISightRadius result = new SightGrid.ISightRadius();
			if (pawn.RaceProps.Animal && pawn.Faction == null)
			{
				result.sight = Mathf.FloorToInt(Mathf.Clamp(pawn.BodySize * 3f, 2f, 10f));
				result.scan = result.sight;
				return result;
			}
			Verb verb = pawn.equipment?.PrimaryEq?.PrimaryVerb ?? null;
			if (verb == null || !verb.Available())
			{
				verb = pawn.verbTracker?.AllVerbs.Where(v => v.Available()).MaxBy(v => v.IsMeleeAttack ? 0 : v.EffectiveRange) ?? null;
			}
			if (verb == null)
			{
				result.sight = 4;
				result.scan = 0;
				return result;
			}
			if (pawn.RaceProps.Insect || pawn.RaceProps.IsMechanoid || pawn.RaceProps.Animal)
			{
				if (verb.IsMeleeAttack)
				{
					result.sight = 10;
					result.scan = 0;
					return result;
				}
				result.scan = Mathf.CeilToInt(verb.EffectiveRange + 10f);
				result.sight = Maths.Max((int)verb.EffectiveRange, 5);
				return result;
			}
			if (verb.IsMeleeAttack)
			{
				SkillRecord melee = pawn.skills?.GetSkill(SkillDefOf.Melee) ?? null;
				if (melee != null)
				{
					result.sight = Maths.Max((int)(Mathf.Clamp(melee.Level, 5, 13) * ((pawn.equipment?.Primary?.def.IsMeleeWeapon ?? null) != null ? 1.5f : 0.85f)), 15);
					result.scan = 0;
				}
				else
				{
					result.sight = 5;
					result.scan = 0;
				}
				return result;
			}
			else
			{
				result.scan = Mathf.CeilToInt(verb.EffectiveRange);
				result.sight = Mathf.CeilToInt(verb.EffectiveRange * 0.75f);
				SkillRecord shooting = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
				float skill = 5;
				if (shooting != null)
				{
					skill = shooting.Level;
				}
				result.sight = Mathf.CeilToInt(Maths.Max(result.sight * Mathf.Clamp(skill / 7.5f, 0.778f, 1.425f), 4));
				return result;
			}
		}

		private static SightGrid.ISightRadius GetSightRadius_Turret(Building_Turret turret)
		{
			SightGrid.ISightRadius result = new SightGrid.ISightRadius();
			Verb verb = turret.AttackVerb;
			if (verb != null)
			{
				result.sight = Mathf.CeilToInt(verb.EffectiveRange);
				result.scan	 = Mathf.CeilToInt(verb.EffectiveRange);				
			}
			return result;
		}

		private static SightGrid.ISightRadius GetSightRadius_Sighter(ThingComp_Sighter sighter)
		{
			SightGrid.ISightRadius result = new SightGrid.ISightRadius();
			result.sight = sighter.SightRadius;
			result.scan	 = result.sight;
			return result;
		}

		private static float GetFogRadius(Thing thing, float sightRadius)
		{
			Pawn  pawn = thing as Pawn;
			if (pawn == null)
			{
				CompMannable mannable = thing.GetComp_Fast<CompMannable>();
				if (mannable != null)
				{
					pawn = mannable.ManningPawn;
				}
			}
			if(pawn != null)
			{
				if (pawn.Downed)
				{
					return 3;
				}
				float vision  = pawn.health.capacities?.GetLevel(PawnCapacityDefOf.Sight) ?? 1f;
				float hearing = pawn.health.capacities?.GetLevel(PawnCapacityDefOf.Hearing) ?? 1f;
				float rest    = Mathf.Lerp(0.65f, 1f, pawn.needs?.rest?.curLevelInt ?? 1f);
				float mul     = Mathf.Clamp(Maths.Min(vision, hearing, rest) * 0.6f + Maths.Max(vision, hearing, rest) * 0.4f, 0.5f, 1.5f);
				return sightRadius * mul;
			}
			return sightRadius;
		}

		private static readonly Dictionary<int, Pair<int, int>> rangeCache = new Dictionary<int, Pair<int, int>>(256);

		//public static int GetSightRange(Thing thing)
		//{
		//	return GetSightRange(thing, !(Faction.OfPlayerSilentFail?.HostileTo(thing.Faction) ?? true));
		//}

		//public static int GetSightRange(ThingComp_Sighter sighter, bool isPlayer)
		//{
		//	if (!isPlayer || !Finder.Settings.FogOfWar_Enabled)
		//	{
		//		return 0;
		//	}
		//	if (rangeCache.TryGetValue(sighter.parent.thingIDNumber, out Pair<int, int> store) && GenTicks.TicksGame - store.First <= 600)
		//	{
		//		return store.Second;
		//	}
		//	int range = Mathf.CeilToInt(sighter.SightRadius * Finder.Settings.FogOfWar_RangeMultiplier);
		//	rangeCache[sighter.parent.thingIDNumber] = new Pair<int, int>(GenTicks.TicksGame, range);			
		//	return range;
		//}

		//public static int GetSightRange(Thing thing, bool isPlayer)
		//{			
		//	if (rangeCache.TryGetValue(thing.thingIDNumber, out Pair<int, int> store) && GenTicks.TicksGame - store.First <= 600)
		//	{
		//		return store.Second;
		//	}
		//	int range = 0;
		//	if (thing is Pawn pawn)
		//	{
		//		range = GetSightRange(pawn);
		//	}
		//	else if (thing is Building_Turret turret)
		//	{
		//		Verb verb = turret.AttackVerb;
		//		if (verb != null)
		//		{
		//			if (verb.verbProps.isMortar)
		//			{
		//				range = Mathf.CeilToInt(Maths.Min(48, verb.EffectiveRange));
		//			}
		//			else
		//			{
		//				range = Mathf.CeilToInt(turret.AttackVerb?.EffectiveRange ?? 0f);
		//			}
		//		}
		//		else
		//		{
		//			range = 0;
		//		}
		//	}			
		//	rangeCache[thing.thingIDNumber] = new Pair<int, int>(GenTicks.TicksGame, range);
		//	return range;
		//}		

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
					return (int)Maths.Max(verb.EffectiveRange * multiplier, 20f * multiplier, verb.EffectiveRange * 0.8f);
				}
			}
		}

		//private static int GetSightRange(Pawn pawn)
		//{
		//	if (pawn.RaceProps.Animal && pawn.Faction == null)
		//	{
		//		return Mathf.FloorToInt(Mathf.Clamp(pawn.BodySize * 3f, 2f, 10f));
		//	}
		//	Verb verb = pawn.equipment?.PrimaryEq?.PrimaryVerb ?? null;
		//	if (verb == null || !verb.Available())
		//	{
		//		verb = pawn.verbTracker?.AllVerbs.Where(v => v.Available()).MaxBy(v => v.IsMeleeAttack ? 0 : v.EffectiveRange) ?? null;
		//	}
		//	if (verb == null)
		//	{
		//		return 4;
		//	}
		//	float range;
		//	if (pawn.RaceProps.Insect || pawn.RaceProps.IsMechanoid || pawn.RaceProps.Animal)
		//	{
		//		if (verb.IsMeleeAttack)
		//		{
		//			return 10;
		//		}
		//		if ((range = verb.EffectiveRange) > 2.5f)
		//		{
		//			return (int)Maths.Max(range * 0.75f, 5f);
		//		}
		//		return 4;
		//	}
		//	if (verb.IsMeleeAttack)
		//	{
		//		SkillRecord melee = pawn.skills?.GetSkill(SkillDefOf.Melee) ?? null;
		//		if (melee != null)
		//		{
		//			float meleeSkill = melee.Level;
		//			return (int)(Mathf.Clamp(meleeSkill, 5, 13) * ((pawn.equipment?.Primary?.def.IsMeleeWeapon ?? null) != null ? 1.5f : 0.85f));
		//		}
		//		return 4;
		//	}
		//	else
		//	{
		//		range = verb.EffectiveRange * 0.75f;
		//		SkillRecord shooting = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
		//		float skill = 5;
		//		if (shooting != null)
		//		{
		//			skill = shooting.Level;
		//		}
		//		range = Maths.Max(range * Mathf.Clamp(skill / 7.5f, 0.778f, 1.425f), 4);
		//		return Mathf.CeilToInt(range);
		//	}
		//}

		public static void ClearCache()
        {            
			rangeCache.Clear();			
        }
    }
}

