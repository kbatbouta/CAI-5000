using System;
using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace CombatAI
{
	public class InterceptorTracker
	{
		private HashSet<IntVec3> _drawnCells = new HashSet<IntVec3>(256);
		private List<IBucketableInterceptor> _removalList = new List<IBucketableInterceptor>();

		private struct IBucketableInterceptor : IBucketable
		{
			public ThingWithComps parent;
			public CompProjectileInterceptor interceptor;
			public int bucketIndex;

			public int BucketIndex => bucketIndex;
			public int UniqueIdNumber => parent.thingIDNumber;

			public IBucketableInterceptor(CompProjectileInterceptor interceptor, int bucketIndex)
			{
				this.interceptor = interceptor;
				parent = interceptor.parent;
				this.bucketIndex = bucketIndex;
			}
		}

		public readonly ITByteGrid grid;
		public readonly Map map;
		public readonly MapComponent_CombatAI combatAI;

		private readonly IBuckets<IBucketableInterceptor> buckets;
		private readonly CellFlooder flooder;
		private bool wait = false;

		public int Count => buckets.Count;

		public InterceptorTracker(MapComponent_CombatAI combatAI)
		{
			this.combatAI = combatAI;
			map = combatAI.map;
			grid = new ITByteGrid(map);
			buckets = new IBuckets<IBucketableInterceptor>(30);
			flooder = new CellFlooder(map);
		}

		public void Tick()
		{
			//FlashMapGrid();
			if (wait) return;
			var bucket = buckets.Current;
			if (bucket.Count != 0)
			{
				_removalList.Clear();
				for (var i = 0; i < bucket.Count; i++)
				{
					var item = bucket[i];
					if (item.parent.Destroyed || !item.parent.Spawned) _removalList.Add(item);
					if (item.interceptor.Active) TryCastInterceptor(item);
				}

				if (_removalList.Count != 0)
				{
					for (var i = 0; i < _removalList.Count; i++) buckets.RemoveId(_removalList[i].UniqueIdNumber);
					_removalList.Clear();
				}
			}

			buckets.Next();
			if (buckets.Index == 0)
			{
				wait = true;
				combatAI.EnqueueOffThreadAction(() =>
				{
					grid.NextCycle();
					wait = false;
				});
			}
		}

		public void FlashMapGrid()
		{
			if (GenTicks.TicksGame % 15 == 0)
			{
				var center = UI.MouseMapPosition().ToIntVec3();
				if (center.InBounds(map))
				{
					_drawnCells.Clear();
					for (var i = center.x - 64; i < center.x + 64; i++)
					for (var j = center.z - 64; j < center.z + 64; j++)
					{
						var cell = new IntVec3(i, 0, j);
						if (cell.InBounds(map) && !_drawnCells.Contains(cell))
						{
							_drawnCells.Add(cell);
							var value = grid.Get(cell);
							if (value > 0)
								map.debugDrawer.FlashCell(cell, Mathf.Clamp(value / 7f, 0.1f, 0.99f), $"{value}", 15);
						}
					}
				}
			}
		}

		public void TryRegister(CompProjectileInterceptor interceptor)
		{
			if (interceptor.parent != null && !interceptor.parent.Destroyed && interceptor.parent.Spawned &&
			    !buckets.ContainsId(interceptor.parent.thingIDNumber))
				buckets.Add(new IBucketableInterceptor(interceptor,
					interceptor.parent.thingIDNumber % buckets.numBuckets));
		}

		private void TryCastInterceptor(IBucketableInterceptor item)
		{
			Thing thing = item.parent;
			var interceptor = item.interceptor;
			var root = thing.Position;
			var flags = (ulong)((interceptor.Props.interceptAirProjectiles
				                    ? InterceptorFlags.interceptAirProjectiles
				                    : 0)
			                    | (interceptor.Props.interceptGroundProjectiles
				                    ? InterceptorFlags.interceptGroundProjectiles
				                    : 0)
			                    | (interceptor.Props.interceptNonHostileProjectiles
				                    ? InterceptorFlags.interceptNonHostileProjectiles
				                    : 0)
			                    | (interceptor.Props.interceptOutgoingProjectiles
				                    ? InterceptorFlags.interceptOutgoingProjectiles
				                    : 0));
			grid.Next();
			combatAI.EnqueueOffThreadAction(() =>
			{
				flooder.Flood(root, (node) => grid.Set(node.cell, 1, flags), null, null,
					Mathf.CeilToInt(interceptor.Props.radius) - 1);
			});
		}
	}
}