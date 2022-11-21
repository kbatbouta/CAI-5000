using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using static CombatAI.SightTracker;

namespace CombatAI
{
    public class AvoidanceTracker : MapComponent
    {
        private HashSet<IntVec3> _drawnCells = new HashSet<IntVec3>();

        private struct IBucketablePawn : IBucketable
        {
            public Pawn pawn;
            public int bucketIndex;

            public int BucketIndex => bucketIndex;
            public int UniqueIdNumber => pawn.thingIDNumber;

            public IBucketablePawn(Pawn pawn, int bucketIndex)
            {
                this.pawn = pawn;
                this.bucketIndex = bucketIndex;
            }
        }

        public class AvoidanceReader
        {                        
            private readonly CellIndices indices;            
            private readonly UInt64 iflags;

            public ITByteGrid path;
            public ITByteGrid proximity;
            public IHeatGrid affliction_dmg;
            public IHeatGrid affliction_pen;

            public AvoidanceReader(AvoidanceTracker tracker, UInt64 iflags)
            {                ;
                this.indices = tracker.map.cellIndices;
                this.iflags = iflags;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetDanger(IntVec3 cell) => GetDanger(indices.CellToIndex(cell));
            public float GetDanger(int index) => affliction_dmg.Get(index) + affliction_pen.Get(index);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetProximity(IntVec3 cell) => GetProximity(indices.CellToIndex(cell));
            public int GetProximity(int index)
            {                                
                return Maths.Max(proximity.Get(index) - ((proximity.GetFlags(index) & iflags) != 0 ? 1: 0), 0);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetPath(IntVec3 cell) => GetPath(indices.CellToIndex(cell));
            public int GetPath(int index)
            {
                return Maths.Max(path.Get(index) - ((path.GetFlags(index) & iflags) != 0 ? 1 : 0), 0);
            }            
        }
        
        private bool wait = false;
        private readonly AsyncActions asyncActions;
        private readonly IBuckets<IBucketablePawn> buckets;
        private readonly List<Pawn> _removalList = new List<Pawn>();        
        private readonly CellFlooder flooder;
        
        public IHeatGrid affliction_dmg;
        public IHeatGrid affliction_pen;
        public ITByteGrid shootLine;
        public ITByteGrid proximity;
        public ITByteGrid path;

        public AvoidanceTracker(Map map) : base(map)
        {
            path = new ITByteGrid(map);
            proximity = new ITByteGrid(map);
            shootLine = new ITByteGrid(map);
            buckets = new IBuckets<IBucketablePawn>(30);
            asyncActions = new AsyncActions();
            flooder = new CellFlooder(map);
            affliction_dmg = new IHeatGrid(map, 60, 64, 6);
            affliction_pen = new IHeatGrid(map, 60, 64, 6);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            asyncActions.ExecuteMainThreadActions();
            if(wait)
            {
                return;
            }
            _removalList.Clear();
            List<IBucketablePawn> items = buckets.Next();
            for (int i = 0; i < items.Count; i++)
            {
                IBucketablePawn item = items[i];
                if (!Valid(item.pawn))
                {
                    _removalList.Add(item.pawn);
                    continue;
                }
                if (item.pawn.Downed || GenTicks.TicksGame - (item.pawn.needs?.rest?.lastRestTick ?? 0) < 30)
                {
                    continue;
                }
                TryCastProximity(item.pawn, IntVec3.Invalid);
                if (item.pawn.pather?.MovingNow ?? false)
                {
                    TryCastPath(item.pawn, null);
                }
            }
            for (int i = 0; i < _removalList.Count; i++)
            {
                DeRegister(_removalList[i]);
            }
            if(buckets.Index == 0)
            {
                wait = true;
                asyncActions.EnqueueOffThreadAction(() =>
                {                    
                    proximity.NextCycle();
                    path.NextCycle();
                    wait = false;                    
                });
            }
            if (Finder.Settings.Debug_DrawAvoidanceGrid_Proximity)
            {
                if (Find.Selector.SelectedPawns.NullOrEmpty())
                {
                    IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                    if (center.InBounds(map))
                    {
                        for (int i = center.x - 64; i < center.x + 64; i++)
                        {
                            for (int j = center.z - 64; j < center.z + 64; j++)
                            {
                                IntVec3 cell = new IntVec3(i, 0, j);
                                if (cell.InBounds(map))
                                {
                                    var value = path.Get(cell) + proximity.Get(cell);
                                    if (value > 0)
                                    {
                                        map.debugDrawer.FlashCell(cell, Mathf.Clamp(value / 10f, 0f, 0.99f), $"{path.Get(cell)} {proximity.Get(cell)}", 15);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    _drawnCells.Clear();
                    foreach (Pawn pawn in Find.Selector.SelectedPawns)
                    {
                        TryGetReader(pawn, out AvoidanceReader reader);
                        if (reader != null)
                        {
                            IntVec3 center = pawn.Position;
                            if (center.InBounds(map))
                            {
                                for (int i = center.x - 64; i < center.x + 64; i++)
                                {
                                    for (int j = center.z - 64; j < center.z + 64; j++)
                                    {
                                        IntVec3 cell = new IntVec3(i, 0, j);
                                        if (cell.InBounds(map) && !_drawnCells.Contains(cell))
                                        {
                                            _drawnCells.Add(cell);
                                            var value = reader.GetPath(cell) + reader.GetProximity(cell);
                                            if (value > 0)
                                            {
                                                map.debugDrawer.FlashCell(cell, Mathf.Clamp(value / 10f, 0f, 0.99f), $"{reader.GetPath(cell)} {reader.GetProximity(cell)}", 15);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (Finder.Settings.Debug_DrawAvoidanceGrid_Danger)
            {                
                IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    for (int i = center.x - 64; i < center.x + 64; i++)
                    {
                        for (int j = center.z - 64; j < center.z + 64; j++)
                        {
                            IntVec3 cell = new IntVec3(i, 0, j);
                            if (cell.InBounds(map))
                            {
                                var value = affliction_pen.Get(cell) + affliction_dmg.Get(cell);
                                if (value > 0)
                                {
                                    map.debugDrawer.FlashCell(cell, Mathf.Clamp(value / 32f, 0f, 0.99f), $"{Math.Round(affliction_pen.Get(cell), 1)} {Math.Round(affliction_dmg.Get(cell), 1)}", 15);
                                }
                            }
                        }
                    }
                }                              
            }
        }

        public bool TryGetReader(Pawn pawn, out AvoidanceReader reader)
        {
            reader = null;
            if (buckets.ContainsId(pawn.thingIDNumber))
            {
                reader = new AvoidanceReader(this, pawn.GetThingFlags());
                reader.proximity = proximity;
                reader.path = path;
                reader.affliction_dmg = affliction_dmg;
                reader.affliction_pen = affliction_pen;
                return true;
            }
            return false;
        }

        public void Register(Pawn pawn)
        {
            if (buckets.ContainsId(pawn.thingIDNumber))
            {
                buckets.RemoveId(pawn.thingIDNumber);
            }
            if (Valid(pawn))
            {
                buckets.Add(new IBucketablePawn(pawn, pawn.thingIDNumber % buckets.numBuckets));
            }
        }

        public override void MapRemoved()
        {
            asyncActions.Kill();
            buckets.Release();
            base.MapRemoved();
        }

        public void DeRegister(Pawn pawn)
        {
            buckets.RemoveId(pawn.thingIDNumber);
        }        

        public void Notify_Injury(Pawn pawn, DamageInfo dinfo)
        {
            IntVec3 loc = pawn.Position;
            asyncActions.EnqueueOffThreadAction(() =>
            {                
                flooder.Flood(loc, (node) =>
                {
                    float f = Maths.Max(1 - node.dist / 5.65685424949f, 0.25f);
                    affliction_dmg.Push(node.cell, dinfo.Amount * f);
                    affliction_pen.Push(node.cell, dinfo.ArmorPenetrationInt * f);
                }, maxDist: 5);
            });
        }

        public void Notify_Death(Pawn pawn, IntVec3 cell)
        {            
        }

        public void Notify_PathFound(Pawn pawn, PawnPath path)
        {
        }

        public void Notify_CoverPositionSelected(Pawn pawn, IntVec3 cell)
        {
        }

        private void TryCastProximity(Pawn pawn, IntVec3 dest)
        {            
            IntVec3 orig;
            orig = pawn.Position;
            UInt64 flags = pawn.GetThingFlags();
            if (pawn.pather?.MovingNow == true && (dest.IsValid || (dest = pawn.pather.Destination.Cell).IsValid))
            {
                asyncActions.EnqueueOffThreadAction(() =>
                {
                    proximity.Next();
                    flooder.Flood(orig, (node) =>
                    {
                        proximity.Set(node.cell, 1, flags);
                    }, maxDist: 2);
                    flooder.Flood(dest, (node) =>
                    {
                        proximity.Set(node.cell, 1, flags);
                    }, maxDist: 2);
                });
            }
            else
            {
                asyncActions.EnqueueOffThreadAction(() =>
                {
                    proximity.Next();
                    flooder.Flood(orig, (node) =>
                    {
                        proximity.Set(node.cell, 1, flags);
                    }, maxDist: 2);
                });
            }
        }

        private void TryCastPath(Pawn pawn, PawnPath pawnPath = null)
        {            
            pawnPath ??= pawn.pather?.curPath;
            if (pawnPath?.nodes == null || pawnPath.curNodeIndex <= 5)
            {
                return;
            }
            UInt64 flags = pawn.GetThingFlags();
            List<IntVec3> cells = pawnPath.nodes.GetRange(Maths.Max(pawnPath.curNodeIndex - 80, 0), Maths.Min(pawnPath.curNodeIndex + 1, 80));
            //int count = Maths.Min(pawnPath.NodesLeftCount, 80);
            //for (int i = 0; i < count; i++)
            //{
            //    cells.Add(pawnPath.Peek(i));
            //}
            if (cells.Count == 0)
            {
                return;
            }
            asyncActions.EnqueueOffThreadAction(() =>
            {
                path.Next();
                IntVec3 prev = cells[0];
                for (int i = 1; i < cells.Count; i++)
                {
                    IntVec3 cur = cells[i];
                    path.Set(cur, 1, flags);
                    int dx = Math.Sign(prev.x - cur.x);
                    int dz = Math.Sign(prev.z - cur.z);
                    prev = cur;
                    if (dx == 0)
                    {
                        path.Set(cur + new IntVec3(-1, 0, 0), 1, flags);
                        path.Set(cur + new IntVec3(1, 0, 0), 1, flags);
                    }
                    else if(dz == 0)
                    {
                        path.Set(cur + new IntVec3(0, 0, -1), 1, flags);
                        path.Set(cur + new IntVec3(0, 0, 1), 1, flags);
                    }
                    else
                    {
                        path.Set(cur + new IntVec3(dx, 0, 0), 1, flags);
                        path.Set(cur + new IntVec3(0, 0, dz), 1, flags);
                    }
                }
                cells.Clear();
            });
        }
        
        private void TryCastShootLine(Pawn pawn)
        {

        }

        private bool Valid(Pawn pawn)
        {           
            return !pawn.Destroyed && pawn.Spawned && !pawn.Dead && (pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid);
        }
    }
}

