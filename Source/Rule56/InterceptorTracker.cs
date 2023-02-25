using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public class InterceptorTracker
    {
        private readonly HashSet<IntVec3>             _drawnCells  = new HashSet<IntVec3>(256);
        private readonly List<IBucketableInterceptor> _removalList = new List<IBucketableInterceptor>();

        private readonly IBuckets<IBucketableInterceptor> buckets;
        public readonly  MapComponent_CombatAI            combatAI;
        private readonly CellFlooder                      flooder;

        public readonly ITByteGrid grid;
        public readonly Map        map;
        private         bool       wait;

        public InterceptorTracker(MapComponent_CombatAI combatAI)
        {
            this.combatAI = combatAI;
            map           = combatAI.map;
            grid          = new ITByteGrid(map);
            buckets       = new IBuckets<IBucketableInterceptor>(30);
            flooder       = new CellFlooder(map);
        }

        public int Count
        {
            get => buckets.Count;
        }

        public void Tick()
        {
            //FlashMapGrid();
            if (wait)
            {
                return;
            }
            List<IBucketableInterceptor> bucket = buckets.Current;
            if (bucket.Count != 0)
            {
                _removalList.Clear();
                for (int i = 0; i < bucket.Count; i++)
                {
                    IBucketableInterceptor item = bucket[i];
                    if (item.parent.Destroyed || !item.parent.Spawned)
                    {
                        _removalList.Add(item);
                    }
                    if (!(item.parent is Pawn pawn && (pawn.needs?.energy != null && pawn.IsCharging() || pawn.IsDormant())) && item.interceptor.Active)
                    {
                        TryCastInterceptor(item);
                    }
                }
                if (_removalList.Count != 0)
                {
                    for (int i = 0; i < _removalList.Count; i++)
                    {
                        buckets.RemoveId(_removalList[i].UniqueIdNumber);
                    }
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
                IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    _drawnCells.Clear();
                    for (int i = center.x - 64; i < center.x + 64; i++)
                    {
                        for (int j = center.z - 64; j < center.z + 64; j++)
                        {
                            IntVec3 cell = new IntVec3(i, 0, j);
                            if (cell.InBounds(map) && !_drawnCells.Contains(cell))
                            {
                                _drawnCells.Add(cell);
                                byte value = grid.Get(cell);
                                if (value > 0)
                                {
                                    map.debugDrawer.FlashCell(cell, Mathf.Clamp(value / 7f, 0.1f, 0.99f), $"{value}", 15);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void TryRegister(CompProjectileInterceptor interceptor)
        {
            if (interceptor.parent != null && !interceptor.parent.Destroyed && interceptor.parent.Spawned && !buckets.ContainsId(interceptor.parent.thingIDNumber))
            {
                buckets.Add(new IBucketableInterceptor(interceptor, interceptor.parent.thingIDNumber % buckets.numBuckets));
            }
        }

        private void TryCastInterceptor(IBucketableInterceptor item)
        {
            Thing                     thing       = item.parent;
            CompProjectileInterceptor interceptor = item.interceptor;
            IntVec3                   root        = thing.Position;
            ulong flags = (ulong)((interceptor.Props.interceptAirProjectiles ? InterceptorFlags.interceptAirProjectiles : 0)
                                  | (interceptor.Props.interceptGroundProjectiles ? InterceptorFlags.interceptGroundProjectiles : 0)
                                  | (interceptor.Props.interceptNonHostileProjectiles ? InterceptorFlags.interceptNonHostileProjectiles : 0)
                                  | (interceptor.Props.interceptOutgoingProjectiles ? InterceptorFlags.interceptOutgoingProjectiles : 0));
            grid.Next();
            combatAI.EnqueueOffThreadAction(() =>
            {
                flooder.Flood(root, node => grid.Set(node.cell, 1, flags), null, null, Mathf.CeilToInt(interceptor.Props.radius) - 1);
            });
        }

        private struct IBucketableInterceptor : IBucketable
        {
            public readonly ThingWithComps            parent;
            public readonly CompProjectileInterceptor interceptor;
            public readonly int                       bucketIndex;

            public int BucketIndex
            {
                get => bucketIndex;
            }
            public int UniqueIdNumber
            {
                get => parent.thingIDNumber;
            }

            public IBucketableInterceptor(CompProjectileInterceptor interceptor, int bucketIndex)
            {
                this.interceptor = interceptor;
                parent           = interceptor.parent;
                this.bucketIndex = bucketIndex;
            }
        }
    }
}
