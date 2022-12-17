using System;
using System.Collections.Generic;
using Verse;
namespace CombatAI
{
	public class WeightedRegionFlooder
	{
		private static readonly FastHeap<Node> queue   = new FastHeap<Node>();
		private static readonly HashSet<int>   flooded = new HashSet<int>(256);

		public static void Flood(IntVec3 root, IntVec3 target, Map map, Func<Region, int, bool> action, Func<Region, bool> validator = null, Func<Region, float> cost = null, int minRegions = 0, int maxRegions = 9999, float maxDist = 9999f, bool depthCost = true)
		{
			Region rootRegion = root.GetRegion(map);
			if (rootRegion == null || (validator != null && !validator(rootRegion)))
			{
				return;
			}
			int   num        = 1;
			queue.Clear();
			queue.Enqueue(new Node(){ region = rootRegion, score = Maths.Sqrt_Fast(target.DistanceToSquared(rootRegion.extentsClose.TopRight), 3)});
			flooded.Clear();
			flooded.Add(rootRegion.id);
			while (!queue.IsEmpty && (num < minRegions || num <= maxRegions))
			{
				Node   node   = queue.Dequeue();
				Region region = node.region;
//				foreach (var cell in region.Cells)
//				{
//					map.debugDrawer.FlashCell(cell, node.depth / 20f, $"{node.depth}");
//				}
				if (action(region, node.depth))
				{
					break;
				}
				num++;
				List<RegionLink> links = region.links;
				for (int li = 0; li < links.Count; li++)
				{
					RegionLink link = links[li];
					for (int ri = 0; ri < 2; ri++)
					{
						Region other = link.regions[ri];
						if (other != null && other != region && !flooded.Contains(other.id) && other.valid)
						{
							flooded.Add(other.id);
							if (validator == null || validator(other))
							{
								float distToTarget = Maths.Sqrt_Fast(target.DistanceToSquared(other.extentsClose.TopRight), 3);
								if (distToTarget < maxDist)
								{
									queue.Enqueue(new Node()
									{
										region = other,
										score  = distToTarget + (!depthCost ? 0 : ((node.depth + 1) * 12)) + (cost?.Invoke(other) ?? 0),
										depth  = node.depth + 1,
									});
								}
							}
						}
					}
				}
			}
			queue.Clear();
			flooded.Clear();
		}
		
		private struct Node : IComparable<Node>
		{
			public Region region;
			public float  score;
			public int    depth;

			public int CompareTo(Node other)
			{
				return score.CompareTo(other.score) * -1;
			}
		}
	}
}
