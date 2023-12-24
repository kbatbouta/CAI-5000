using System.Collections.Generic;
using System.Linq;
using CombatAI.Comps;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI.Patches
{
    public static class LordToil_AssaultColony_Patch
    {
	    private static readonly List<Pawn>              rangedLong  = new List<Pawn>();
	    private static readonly List<Pawn>              rangedShort = new List<Pawn>();
	    private static readonly List<Pair<Pawn, float>> ranged      = new List<Pair<Pawn, float>>();
	    private static readonly List<Pawn>              melee       = new List<Pawn>();
	    
        private static readonly List<Pawn>[] forces = new List<Pawn>[10];
        private static readonly List<Thing>  things = new List<Thing>();
        private static readonly List<Thing>  thingsImportant = new List<Thing>();
        private static readonly List<Zone>   zones  = new List<Zone>();

        static LordToil_AssaultColony_Patch()
        {
            forces[0] = new List<Pawn>();
            forces[1] = new List<Pawn>();
            forces[2] = new List<Pawn>();
            forces[3] = new List<Pawn>();
            forces[4] = new List<Pawn>();
            forces[5] = new List<Pawn>();
            forces[6] = new List<Pawn>();
            forces[7] = new List<Pawn>();
            forces[8] = new List<Pawn>();
            forces[9] = new List<Pawn>();
        }

        public static void ClearCache()
        {
            zones.Clear();
            things.Clear();
            rangedLong.Clear();
            rangedShort.Clear();
            ranged.Clear();
            melee.Clear();
            thingsImportant.Clear();
            forces[0].Clear();
            forces[1].Clear();
            forces[2].Clear();
            forces[3].Clear();
            forces[4].Clear();
            forces[5].Clear();
            forces[6].Clear();
            forces[7].Clear();
            forces[8].Clear();
            forces[9].Clear();
        }

        private static float GetZoneTotalMarketValue(Zone zone)
        {
            if (!TKVCache<int, Zone_Stockpile, float>.TryGet(zone.ID, out float val, 6000))
            {
                val = zone.AllContainedThings.Sum(t => t.GetStatValue_Fast(StatDefOf.MarketValue, 1200));
                TKVCache<int, Zone_Stockpile, float>.Put(zone.ID, val);
            }
            return val;
        }

        [HarmonyPatch(typeof(LordToil_AssaultColony), nameof(LordToil_AssaultColony.UpdateAllDuties))]
        private static class LordToil_AssaultColony_UpdateAllDuties_Patch
        {
            public static void Postfix(LordToil_AssaultColony __instance)
            {
                if (Finder.Settings.Enable_Groups && __instance.lord.ownedPawns.Count > 10)
                {
                    ClearCache();
                    Map map = __instance.Map;
                    things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.Bed).Where(b => b is Building_Bed bed && bed.CompAssignableToPawn.AssignedPawns.Any(p => p.Faction == map.ParentFaction)));
                    things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.ResearchBench).Where(t => t.Faction == map.ParentFaction));
                    things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.FoodDispenser).Where(t => t.Faction == map.ParentFaction));
                    things.AddRange(map.mapPawns.PrisonersOfColonySpawned);
                    // add custom and modded raid targets.
                    foreach (ThingDef def in RaidTargetDatabase.allDefs)
                    {
                        if (map.listerThings.listsByDef.TryGetValue(def, out List<Thing> things) && things != null)
                        {
                            if (Finder.Settings.Debug)
                            {
                                Log.Message($"ISMA: Added things of def {def} to the current raid pool.");
                            }
                            thingsImportant.AddRange(things);
                        }
                    }
                    if (ModsConfig.BiotechActive)
                    {
                        things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.MechCharger).Where(t => t.Faction == map.ParentFaction));
                        things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.GenepackHolder));
                    }
                    if (ModsConfig.IdeologyActive)
                    {
                        things.AddRange(map.mapPawns.SlavesOfColonySpawned);
                    }
                    if (ModsConfig.RoyaltyActive)
                    {
                        things.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.Throne));
                    }
                    zones.AddRange(__instance.Map.zoneManager.AllZones.Where(z => z is Zone_Stockpile || z is Zone_Growing));
                    int taskForceNum = Maths.Min(__instance.lord.ownedPawns.Count / 5, 10);
                    int m            = Rand.Range(1, 7);
                    int c            = 0;
                    for (int i = 0; i < __instance.lord.ownedPawns.Count; i++)
                    {
                        int k = Rand.Range(0, taskForceNum + m);
                        if (k < m)
                        {
                            c++;
                            continue;
                        }
                        forces[k - m].Add(__instance.lord.ownedPawns[i]);
                    }
                    if (Finder.Settings.Debug)
                    {
                        Log.Message($"{__instance.lord.ownedPawns.Count - c} pawns are assigned to attack specific targets and {c} are assigned to assault duties. {__instance.lord.ownedPawns.Count - c}/{__instance.lord.ownedPawns.Count} ");
                    }
                    for (int i = 0; i < taskForceNum; i++)
                    {
                        List<Pawn> force = forces[i];
                        if (zones.Count != 0 && (Rand.Chance(0.333f) || things.Count == 0))
                        {
                            Zone zone = zones.RandomElementByWeight(s => GetZoneTotalMarketValue(s) / 100f + (s.Position.Roofed(__instance.Map) ? 2 : 0f));
                            for (int j = 0; j < force.Count; j++)
                            {
                                ThingComp_CombatAI comp = force[j].AI();
                                if (comp == null &&  force[j] is Pawn p)
                                {
                                    Log.Error($"IMSA: {p} has no ThingComp_CombatAI");
                                }
                                if (comp != null && !comp.duties.Any(CombatAI_DutyDefOf.CombatAI_AssaultPoint))
                                {
                                    Pawn_CustomDutyTracker.CustomPawnDuty customDuty = CustomDutyUtility.AssaultPoint(zone.Position, Rand.Range(7, 15), 3600 * Rand.Range(3, 8));
                                    if (force[j].TryStartCustomDuty(customDuty))
                                    {
                                        if (Finder.Settings.Debug)
                                        {
                                            Log.Message($"{comp.parent} task force {i} attacking {zone}");
                                        }
                                        if (Rand.Chance(0.33f))
                                        {
                                            Pawn_CustomDutyTracker.CustomPawnDuty customDuty2 = CustomDutyUtility.DefendPoint(zone.Position, Rand.Range(30, 60), true, 3600 + Rand.Range(0, 60000));
                                            force[j].EnqueueFirstCustomDuty(customDuty2);
                                            if (Finder.Settings.Debug)
                                            {
                                                Log.Message($"{comp.parent} task force {i} occupying area around {zone}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (things.Count != 0)
                        {
                            List<Thing> collection;
                            if (thingsImportant.NullOrEmpty())
                            {
                                collection = things;
                            }
                            else
                            {
                                collection = Rand.Chance(0.5f) ? thingsImportant : things;
                            }
                            Thing       thing = collection.RandomElementByWeight(t => t.GetStatValue_Fast(StatDefOf.MarketValue, 1200) * (t is Pawn ? 10 : 1));
                            if (thing != null)
                            {
                                for (int j = 0; j < force.Count; j++)
                                {
                                    ThingComp_CombatAI comp = force[j].AI();
                                    if (comp != null && !comp.duties.Any(CombatAI_DutyDefOf.CombatAI_AssaultPoint))
                                    {
                                        Pawn_CustomDutyTracker.CustomPawnDuty customDuty = CustomDutyUtility.AssaultPoint(thing.Position, Rand.Range(7, 15), 3600 * Rand.Range(3, 8));
                                        if (force[j].TryStartCustomDuty(customDuty))
                                        {
                                            if (Finder.Settings.Debug)
                                            {
                                                Log.Message($"{comp.parent} task force {i} attacking {thing}");
                                            }
                                            if (Rand.Chance(0.33f))
                                            {
                                                Pawn_CustomDutyTracker.CustomPawnDuty customDuty2 = CustomDutyUtility.DefendPoint(thing.Position, Rand.Range(30, 60), true, 3600 + Rand.Range(0, 60000));
                                                force[j].EnqueueFirstCustomDuty(customDuty2);
                                                if (Finder.Settings.Debug)
                                                {
                                                    Log.Message($"{comp.parent} task force {i} occupying area around {thing}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
//                    foreach (Pawn pawn in __instance.lord.ownedPawns)
//                    {
//	                    DamageReport report = DamageUtility.GetDamageReport(pawn);
//	                    if (report.IsValid)
//	                    {
//		                    if (!report.primaryIsRanged)
//		                    {
//			                    melee.Add(pawn);
//			                    ranged.Add(new Pair<Pawn, float>(pawn, 0));
//		                    }
//		                    else
//		                    {
//			                    float range = report.primaryVerbProps?.range ?? 10;
//			                    ranged.Add(new Pair<Pawn, float>(pawn, range));
//			                    if (range > 16)
//			                    {
//				                    rangedLong.Add(pawn);
//			                    }
//			                    else
//			                    {
//				                    rangedShort.Add(pawn);
//			                    }
//		                    }
//	                    }
//                    }
//                    ranged.SortBy(p => p.second);
//                    int maxRange = Rand.Range(15, 30);
//                    int num      = 0;
//                    int limit    = (int) (ranged.Count * Rand.Range(0.25f, 0.4f));
//                    for (int i = 0; i < ranged.Count && num < limit; i += (Rand.Int % 2 + 1))
//                    {
//	                    Pair<Pawn, float> pair = ranged[i];
//	                    if (pair.second > maxRange)
//	                    {
//		                    break;
//	                    }
//	                    SkillRecord record = pair.First.skills?.GetSkill(SkillDefOf.Mining) ?? null;
//	                    if (record != null && Rand.Chance(record.Level / 15f))
//	                    {
//		                    continue;
//	                    }
//	                    if (!Rand.Chance(i / (ranged.Count * 2f) + 0.01f))
//	                    {
//		                    for (int j = i + 1; j < ranged.Count; j++)
//		                    {
//			                    Pair<Pawn, float> other = ranged[j];
//			                    if (other.second  > pair.second)
//			                    {
//				                    int               index    = Rand.Range(j, ranged.Count - 1);
//				                    if (index >= 0)
//				                    {
//					                    Pair<Pawn, float>                     escortee   = ranged[index];
//					                    Pawn_CustomDutyTracker.CustomPawnDuty customDuty = CustomDutyUtility.Escort(escortee.First, 15, 64, 2400 + Rand.Range(0, 12000));
//					                    customDuty.endOnTookDamage = true;
//					                    pair.First.TryStartCustomDuty(customDuty);
//					                    num++;
//					                    if (Finder.Settings.Debug)
//					                    {
//						                    Log.Message($"{num}. {pair.first}({pair.second}) escorting {escortee.first}({escortee.second})");
//					                    }
//				                    }
//				                    break;
//			                    }
//		                    }
//	                    }
//                    }
//                    foreach (Pawn pawn in melee)
//                    {
//	                    ThingComp_CombatAI comp = pawn.AI();
//	                    if (comp != null)
//	                    {
//		                    Pawn ally = null;
//		                    if (rangedLong.Count > 0 && Rand.Chance(Maths.Min(0.5f, rangedLong.Count / 5f)))
//		                    {
//			                    ally = rangedLong.RandomElement();
//		                    }
//		                    else if (rangedShort.Count > 0 && Rand.Chance(Maths.Min(0.5f, rangedShort.Count / 5f)))
//		                    {
//			                    ally = rangedShort.RandomElement();
//		                    }
//		                    if (ally != null)
//		                    {
//			                    Pawn_CustomDutyTracker.CustomPawnDuty customDuty = CustomDutyUtility.Escort(ally, 15, 64, 3600 + Rand.Range(0, 9600));
//			                    customDuty.endOnTookDamage = true;
//			                    if (comp.duties != null)
//			                    {
//				                    pawn.TryStartCustomDuty(customDuty);
//			                    }
//		                    }
//	                    }
//                    }
//                    foreach (Pawn pawn in rangedShort)
//                    {
//	                    ThingComp_CombatAI comp = pawn.AI();
//	                    if (comp != null)
//	                    {
//		                    Pawn ally = null;
//		                    if (rangedShort.Count > 0 && Rand.Chance(Maths.Min(0.5f, rangedLong.Count / 5f)))
//		                    {
//			                    ally = rangedLong.RandomElement();
//		                    }
//		                    if (ally != null)
//		                    {
//			                    Pawn_CustomDutyTracker.CustomPawnDuty customDuty = CustomDutyUtility.Escort(ally, 15, 64, 3600 + Rand.Range(0, 9600));
//			                    customDuty.endOnTookDamage = true;
//			                    if (comp.duties != null)
//			                    {
//				                    pawn.TryStartCustomDuty(customDuty);
//			                    }
//		                    }
//	                    }
//                    }
                    ClearCache();
                }
            }
        }
    }
}
