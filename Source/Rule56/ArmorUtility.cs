using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using CombatAI.Gui;
using RimWorld;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Verse;

namespace CombatAI
{
	public static class ArmorUtility
	{
		private static Dictionary<ThingDef, bool> shields = new Dictionary<ThingDef, bool>(128);
		private static Dictionary<int, ArmorReport> reports = new Dictionary<int, ArmorReport>(128);
		private static Dictionary<ThingDef, Pair<float, float>> baseArmors = new Dictionary<ThingDef, Pair<float, float>>(128);
		private static Dictionary<BodyDef, BodyDefApparels> models = new Dictionary<BodyDef, BodyDefApparels>();

		private class BodyDefApparels
		{
			public readonly BodyDef bodyDef;
			public readonly PawnBodyModel model;

			private readonly Dictionary<ApparelProperties, float> apparels = new Dictionary<ApparelProperties, float>();

			public BodyDefApparels(BodyDef body)
			{
				this.bodyDef = body;
				this.model = new PawnBodyModel(body);
			}

			public float Coverage(ApparelProperties apparel)
			{
				if (!apparels.TryGetValue(apparel, out float coverage))
				{
					coverage = 0;
					List<BodyPartGroupDef> groups = apparel.bodyPartGroups;
					for(int i = 0; i < groups.Count; i++)
					{
						BodyPartGroupDef group = groups[i];
						coverage = Maths.Max(model.Coverage(group), coverage);
					}		
					apparels[apparel] = coverage;
				}
				return coverage;
			}
		}

		public static void Initialize()
		{
		}

		public static ArmorReport GetArmorReport(this Pawn pawn, Listing_Collapsible collapsible = null)
		{
			if (pawn == null)
			{
				return default(ArmorReport);
			}
			if (collapsible != null || !reports.TryGetValue(pawn.thingIDNumber, out ArmorReport report) || GenTicks.TicksGame - report.createdAt > 900)
			{
				reports[pawn.thingIDNumber] = report = CreateReport(pawn, collapsible);
			}
			return report;
		}

		private static ArmorReport CreateReport(Pawn pawn, Listing_Collapsible collapsible)
		{
			ArmorReport report = new ArmorReport();
			report.pawn = pawn;
			report.bodySize = pawn.BodySize;
			if (!baseArmors.TryGetValue(pawn.def, out Pair<float, float> baseArmor))
			{
				baseArmor = new Pair<float, float>(pawn.GetStatValue(StatDefOf.ArmorRating_Blunt), pawn.GetStatValue(StatDefOf.ArmorRating_Sharp));				
			}
			report.bodyBlunt = baseArmor.First;
			report.bodySharp = baseArmor.Second;			
			FillApparel(ref report, collapsible);
			if (pawn.health?.hediffSet != null && !pawn.RaceProps.IsMechanoid)
			{
				float limit = pawn.GetStatValue(StatDefOf.PainShockThreshold);
				if (limit > 0)
				{
					float painInt = 1.0f - Mathf.Lerp(0, 1.0f, pawn.health.hediffSet.PainTotal / limit);
					if (painInt > 0.85f)
					{
						painInt = 1.0f;
					}
					report.apparelBlunt *= painInt;
					report.apparelSharp *= painInt;
					report.bodyBlunt *= painInt;
					report.bodySharp *= painInt;
				}
			}
			report.createdAt = GenTicks.TicksGame;
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
				float armor_blunt = 0;
				float armor_sharp = 0;
				float coverage = 0;				
				List<Apparel> apparels = pawn.apparel.WornApparel;
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
						float c = bodyApparels.Coverage(apparel.def.apparel);
						coverage += c;
						armor_blunt += c * apparel.GetStatValue(StatDefOf.ArmorRating_Blunt);
						armor_sharp += c * apparel.GetStatValue(StatDefOf.ArmorRating_Sharp);
						if (debug)
						{
							collapsible.Label($"{i}. {apparel.def.label},\tc={c}");
						}
					}
				}
				if (coverage != 0)
				{
					report.apparelBlunt = armor_blunt;
					report.apparelSharp = armor_sharp;
				}
				if (debug)
				{
					collapsible.Line(1);
					collapsible.Label($"b:{report.apparelBlunt}\ts:{report.apparelSharp}");
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
			baseArmors.Clear();
		}
	}
}

