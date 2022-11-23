using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using RimWorld;
using TMPro;
using Verse;

namespace CombatAI
{
	public static class ArmorUtility
	{
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

		private static BodyDefApparels GetBodyApparels(BodyDef body)
		{
			if(!models.TryGetValue(body, out BodyDefApparels apparels))
			{
				models[body] = apparels = new BodyDefApparels(body);
			}
			return apparels;
		}

		public static float GetAvgBodyArmor(Pawn pawn)
		{
			if(pawn == null || pawn.apparel == null)
			{
				return 0;
			}
			BodyDefApparels bodyApparels = GetBodyApparels(pawn.RaceProps.body);
			if (bodyApparels != null)
			{
				float armor = 0;
				//string message = $"coverage report {bodyApparels.bodyDef.defName}:\n";
				//foreach (var p in bodyApparels.model.coverageByPartGroup)
				//{
				//	message += $"{p.Key}\t{p.Value}\n";
				//}
				//message += "---------\n";
				List<Apparel> apparels = pawn.apparel.WornApparel;
				for (int i = 0; i < apparels.Count; i++)
				{
					Apparel apparel = apparels[i];
					if (apparel != null && apparel.def.apparel != null)
					{
						armor += bodyApparels.Coverage(apparel.def.apparel);
						//message += $"{apparel.def.defName}\t{bodyApparels.Coverage(apparel.def.apparel)}\n";
					}
				}
				//Log.Message(message);
				return armor;
			}
			return 0;
		}

		//private static readonly List<BodyPartInfo> _temp = new List<BodyPartInfo>(32);
		//private static readonly Dictionary<>
		////private static readonly Dictionary<BodyPartRecord, float> _dict = new Dictionary<BodyPartRecord, float>(32);

		//public static float GetAvgArmorRating(Pawn pawn)
		//{			
		//	if (pawn.apparel == null)
		//	{
		//		return 0f;
		//	}
		//	BodyPartRecord core = pawn.RaceProps.body.corePart;

		//	//List<BodyPartRecord> bodyParts = core.parts;
		//	//_list.Clear();
		//	//_dict.Clear();
		//	//_dict[core] = 0f;
		//	//_list.Add(core);
		//	//for (int i = 0;i < bodyParts.Count; i++)
		//	//{
		//	//	BodyPartRecord part = bodyParts[i];
		//	//	if (part.depth != BodyPartDepth.Inside)
		//	//	{
		//	//		_dict[part] = 0f;
		//	//		_list.Add(part);
		//	//	}
		//	//}
		//	//List<Apparel> wornApparel = pawn.apparel.WornApparel;			
		//	//for (int i = 0; i < wornApparel.Count; i++)
		//	//{
		//	//	Apparel apparel = wornApparel[i];
		//	//	if(apparel.def.apparel.bod)
		//	//}
		//	return 0f;
		//}

		//private static void ExploreBodyRecord(BodyPartRecord body)
		//{			
		//	_temp.Clear();
		//	_temp.Add(new BodyPartInfo(body, 1));
		//	List<BodyPartRecord> parts;
		//	BodyPartInfo partInfo;
		//	while (_temp.Count > 0)
		//	{
		//		partInfo = _temp.Pop();				
		//		parts = partInfo.Parts;
		//		for (int i = 0; i < parts.Count; i++)
		//		{
		//			BodyPartRecord subPart = parts[i];
		//			if (subPart.depth != BodyPartDepth.Inside)
		//			{
		//				float weightedCoverage = partInfo.weightedCoverage * subPart.coverage;
		//				if (weightedCoverage > 0.04)
		//				{
		//					_temp.Add(new BodyPartInfo(subPart, weightedCoverage));
		//				}
		//			}
		//		}
		//	}
		//	_temp.Clear();
		//}

		//private static float GetAvgApparelArmorRating(Apparel apparel, RaceProperties properties)
		//{
		//	//apparel.def.apparel.bodyPartGroups
		//}
	}
}

