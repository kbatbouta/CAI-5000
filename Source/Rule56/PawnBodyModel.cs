using System;
using System.Collections.Generic;
using RimWorld;
using Steamworks;
using Verse;

namespace CombatAI
{
	public class PawnBodyModel
	{
		private static List<BodyPartInfo> _temp = new List<BodyPartInfo>();
		private static List<BodyPartInfo> _majorParts = new List<BodyPartInfo>(32);

		private struct BodyPartInfo
		{
			public readonly BodyPartRecord part;
			public readonly float weightedCoverage;

			public List<BodyPartRecord> Parts => part.parts;

			public BodyPartInfo(BodyPartRecord part, float weightCoverage)
			{
				this.part = part;
				weightedCoverage = weightCoverage;
			}
		}

		public readonly Dictionary<BodyPartGroupDef, float> coverageByPartGroup =
			new Dictionary<BodyPartGroupDef, float>(32);

		public readonly BodyDef body;

		public PawnBodyModel(BodyDef body)
		{
			this.body = body;
			_temp.Clear();
			_majorParts.Clear();
			// push the first node.
			_temp.Add(new BodyPartInfo(body.corePart, 1));
			_majorParts.Add(_temp[0]);
			// start.
			List<BodyPartRecord> parts;
			BodyPartInfo partInfo;
			while (_temp.Count > 0)
			{
				partInfo = _temp.Pop();
				parts = partInfo.Parts;
				for (var i = 0; i < parts.Count; i++)
				{
					var subPart = parts[i];
					if (subPart.depth != BodyPartDepth.Inside)
					{
						var weightedCoverage = partInfo.weightedCoverage * subPart.coverage;
						if (weightedCoverage > 0.04)
						{
							var subInfo = new BodyPartInfo(subPart, weightedCoverage);
							_temp.Add(subInfo);
							_majorParts.Add(subInfo);
						}
					}
				}
			}

			_temp.Clear();
			_majorParts.SortBy(m => -1 * m.weightedCoverage);
			for (var i = 0; i < _majorParts.Count; i++)
			{
				partInfo = _majorParts[i];
				var groups = partInfo.part.groups;
				if (groups != null)
					for (var j = 0; j < groups.Count; j++)
					{
						var group = groups[j];
						if (!coverageByPartGroup.ContainsKey(group))
							coverageByPartGroup[group] = partInfo.weightedCoverage *
							                             (partInfo.part.height == BodyPartHeight.Top ? 4 : 1);
					}
			}

			_majorParts.Clear();
		}

		public float Coverage(BodyPartGroupDef groupDef)
		{
			if (coverageByPartGroup.TryGetValue(groupDef, out var coverage)) return coverage;
			return 0f;
		}

		public void Release()
		{
			coverageByPartGroup.Clear();
			_temp.Clear();
			_majorParts.Clear();
		}
	}
}