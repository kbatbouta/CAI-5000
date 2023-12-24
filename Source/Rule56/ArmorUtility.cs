using System.Collections.Generic;
using CombatAI.Gui;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public static class ArmorUtility
    {
        private static readonly Dictionary<ThingDef, bool>           shields = new Dictionary<ThingDef, bool>(128);
        private static readonly Dictionary<int, ArmorReport>         reports = new Dictionary<int, ArmorReport>(128);
        private static readonly Dictionary<BodyDef, BodyDefApparels> models  = new Dictionary<BodyDef, BodyDefApparels>();

        public static void Initialize()
        {
        }

        public static ArmorReport GetArmorReport(this Pawn pawn, Listing_Collapsible collapsible = null)
        {
            if (pawn == null)
            {
                return default(ArmorReport);
            }
            if (collapsible != null || !reports.TryGetValue(pawn.thingIDNumber, out ArmorReport report) || GenTicks.TicksGame - report.createdAt > 7200)
            {
                reports[pawn.thingIDNumber] = report = CreateReport(pawn, collapsible);
            }
            return report;
        }

        private static ArmorReport CreateReport(Pawn pawn, Listing_Collapsible collapsible)
        {
            ArmorReport report = new ArmorReport();
            report.pawn      = pawn;
            report.bodySize  = pawn.BodySize;
            report.bodyBlunt = pawn.GetStatValue_Fast(StatDefOf.ArmorRating_Blunt, 900);
            report.bodySharp = pawn.GetStatValue_Fast(StatDefOf.ArmorRating_Sharp, 900);
            FillApparel(ref report, collapsible);
            if (pawn.health?.hediffSet != null && !pawn.RaceProps.IsMechanoid)
            {
                float limit = pawn.GetStatValue_Fast(StatDefOf.PainShockThreshold, 1800);
                if (limit > 0)
                {
                    float painInt = 1.0f - Mathf.Clamp01(pawn.health.hediffSet.PainTotal / limit);
                    if (painInt > 0.85f)
                    {
                        painInt = 1.0f;
                    }
                    report.apparelBlunt *= painInt;
                    report.apparelSharp *= painInt;
                    report.bodyBlunt    *= painInt;
                    report.bodySharp    *= painInt;
                }
            }
            report.createdAt          = GenTicks.TicksGame;
            report.weaknessAttributes = pawn.GetWeaknessAttributes();
            return report;
        }

        private static void FillApparel(ref ArmorReport report, Listing_Collapsible collapsible)
        {
            Pawn pawn = report.pawn;
            if (pawn.apparel == null)
            {
                return;
            }
            bool debug = collapsible != null;
            if (debug)
            {
                collapsible.Line(4);
                collapsible.Label($"Apparel for {report.pawn}");
                collapsible.Line(1);
            }
            BodyDefApparels bodyApparels = GetBodyApparels(pawn.RaceProps.body);
            if (bodyApparels != null)
            {
                float         armor_blunt = 0;
                float         armor_sharp = 0;
                float         max_blunt   = 0f;
                float         max_sharp   = 0f;
                float         coverage    = 0;
                List<Apparel> apparels    = pawn.apparel.WornApparel;
                for (int i = 0; i < apparels.Count; i++)
                {
                    Apparel apparel = apparels[i];
                    if (!shields.TryGetValue(apparel.def, out bool isShield))
                    {
                        isShield = shields[apparel.def] = apparel.def.HasComp(typeof(CompShield));
                    }
                    report.hasShieldBelt |= isShield;
                    if (apparel != null && apparel.def.apparel != null)
                    {
                        if (isShield)
                        {
                            report.shield ??= apparel.GetComp<CompShield>();
                        }
                        float c = bodyApparels.Coverage(apparel.def.apparel);
                        coverage += c;
                        float blunt = apparel.GetStatValue_Fast(StatDefOf.ArmorRating_Blunt, 2700);
                        float sharp = apparel.GetStatValue_Fast(StatDefOf.ArmorRating_Blunt, 2700);
                        if (max_sharp < sharp)
                        {
                            max_sharp = sharp;
                        }
                        if (max_blunt < blunt)
                        {
                            max_blunt = blunt;
                        }
                        armor_blunt += c * blunt;
                        armor_sharp += c * sharp;
                        if (debug)
                        {
                            collapsible.Label($"{i}. {apparel.def.label},\tc={c}");
                        }
                    }
                }
                armor_blunt = Maths.Min(armor_blunt, max_blunt);
                armor_sharp = Maths.Min(armor_sharp, max_sharp);
                if (coverage != 0)
                {
                    report.apparelBlunt = armor_blunt;
                    report.apparelSharp = armor_sharp;
                    if (report.hasShieldBelt)
                    {
                        report.apparelBlunt *= 4;
                        report.apparelSharp *= 4;
                    }
                }
                if (debug)
                {
                    collapsible.Line(1);
                    collapsible.Label($"b:{report.apparelBlunt}\ts:{report.apparelSharp}\tt:{report.TankInt}");
                    collapsible.Line(1);
                }
            }
        }


        private static BodyDefApparels GetBodyApparels(BodyDef body)
        {
            if (!models.TryGetValue(body, out BodyDefApparels apparels))
            {
                models[body] = apparels = new BodyDefApparels(body);
            }
            return apparels;
        }

        public static void ClearCache()
        {
            reports.Clear();
        }

        private class BodyDefApparels
        {

            private readonly Dictionary<ApparelProperties, float> apparels = new Dictionary<ApparelProperties, float>();
            public readonly  BodyDef                              bodyDef;
            public readonly  PawnBodyModel                        model;

            public BodyDefApparels(BodyDef body)
            {
                bodyDef = body;
                model   = new PawnBodyModel(body);
            }

            public float Coverage(ApparelProperties apparel)
            {
                if (!apparels.TryGetValue(apparel, out float coverage))
                {
                    coverage = 0;
                    List<BodyPartGroupDef> groups = apparel.bodyPartGroups;
                    for (int i = 0; i < groups.Count; i++)
                    {
                        BodyPartGroupDef group = groups[i];
                        coverage = Maths.Max(model.Coverage(group), coverage);
                    }
                    apparels[apparel] = coverage;
                }
                return coverage;
            }
        }
        
        public static void Invalidate(Thing thing)
        {
	        if (reports.ContainsKey(thing.thingIDNumber))
	        {
		        reports.Remove(thing.thingIDNumber);
	        }
        }
    }
}
