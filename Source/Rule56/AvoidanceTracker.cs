using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CombatAI.Comps;
using CombatAI.Squads;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public class AvoidanceTracker : MapComponent
    {
        private readonly HashSet<IntVec3>          _drawnCells  = new HashSet<IntVec3>();
        private readonly List<Pawn>                _removalList = new List<Pawn>();
        private readonly AsyncActions              asyncActions;
        private readonly IBuckets<IBucketablePawn> buckets;
        private readonly CellFlooder               flooder;

        public IHeatGrid  affliction_dmg;
        public IHeatGrid  affliction_pen;
        public ITByteGrid path;
        public ITByteGrid path_squads;
        public ITByteGrid proximity;
        public ITByteGrid proximity_squads;
        public ITByteGrid shootLine;

        private bool wait;

        public AvoidanceTracker(Map map) : base(map)
        {
            path             = new ITByteGrid(map);
            path_squads      = new ITByteGrid(map);
            proximity        = new ITByteGrid(map);
            proximity_squads = new ITByteGrid(map);
            shootLine        = new ITByteGrid(map);
            buckets          = new IBuckets<IBucketablePawn>(30);
            flooder          = new CellFlooder(map);
            affliction_dmg   = new IHeatGrid(map, 60, 64, 6);
            affliction_pen   = new IHeatGrid(map, 60, 64, 6);
            asyncActions     = new AsyncActions();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            asyncActions.Start();
        }

        public override void MapComponentUpdate()
        {
	        base.MapComponentUpdate();
	        asyncActions.ExecuteMainThreadActions();
	        if (wait)
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
			        TryCastPath(item);
		        }
	        }
	        for (int i = 0; i < _removalList.Count; i++)
	        {
		        DeRegister(_removalList[i]);
	        }
	        if (buckets.Index == 0)
	        {
		        wait = true;
		        asyncActions.EnqueueOffThreadAction(() =>
		        {
			        proximity.NextCycle();
			        proximity_squads.NextCycle();
			        path.NextCycle();
			        path_squads.NextCycle();
			        wait = false;
		        });
	        }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
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
                                    int value = Maths.Max(path_squads.Get(cell), path.Get(cell)) + Maths.Max(proximity_squads.Get(cell), proximity.Get(cell));
                                    if (value > 0)
                                    {
                                        map.debugDrawer.FlashCell(cell, Mathf.Clamp(value / 10f, 0f, 0.99f), $"{Maths.Max(path_squads.Get(cell), path.Get(cell)) } { Maths.Max(proximity_squads.Get(cell), proximity.Get(cell))}", 15);
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
                                            int value = reader.GetPath(cell) + reader.GetProximity(cell);
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
                                float value = affliction_pen.Get(cell) + affliction_dmg.Get(cell);
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
                reader           = new AvoidanceReader(this, pawn.GetThingFlags(), pawn.GetThingFlags());
                reader.proximity = proximity;
                reader.path      = path;
                if (!pawn.Faction.IsPlayerSafe())
                {
	                reader.path_squads      = path_squads;
	                reader.proximity_squads = proximity_squads;
                }
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
                flooder.Flood(loc, node =>
                {
                    float f = Maths.Max(1 - node.dist / 5.65685424949f, 0.25f);
                    affliction_dmg.Push(node.cell, dinfo.Amount * f);
                    affliction_pen.Push(node.cell, dinfo.ArmorPenetrationInt * f);
                }, maxDist: 30, maxCellNum: 25, passThroughDoors: true);
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
            ulong flags = pawn.GetThingFlags();
            if (pawn.pather?.MovingNow == true && (dest.IsValid || (dest = pawn.pather.Destination.Cell).IsValid))
            {
                asyncActions.EnqueueOffThreadAction(() =>
                {
                    proximity.Next();
                    proximity_squads.Next();
                    flooder.Flood(orig, node =>
                    {
                        proximity.Set(node.cell, 1, flags);
                    }, maxDist: 4, maxCellNum: 9, passThroughDoors: true);
                    flooder.Flood(dest, node =>
                    {
                        proximity.Set(node.cell, 1, flags);
                    }, maxDist: 4, maxCellNum: 9, passThroughDoors: true);
                });
            }
            else
            {
                asyncActions.EnqueueOffThreadAction(() =>
                {
                    proximity.Next();
                    flooder.Flood(orig, node =>
                    {
                        proximity.Set(node.cell, 1, flags);
                    }, maxDist: 4, maxCellNum: 9, passThroughDoors: true);
                });
            }
        }

        private void TryCastPath(IBucketablePawn item, PawnPath pawnPath = null)
        {
            Pawn pawn = item.pawn;
            pawnPath ??= pawn.pather?.curPath;
            if (pawnPath?.nodes == null || pawnPath.curNodeIndex <= 5)
            {
                return;
            }
            ulong         flags = pawn.GetThingFlags();
            List<IntVec3> cells = item.tempPath;
            List<IntVec3> cells2 = item.tempPathSquad;
            cells.Clear();
            cells2.Clear();
            int index = Maths.Max(pawnPath.curNodeIndex - 90, 0);
            int limit = Maths.Min(index + 80, pawnPath.curNodeIndex + 1);
            for (int i = index; i < limit; i++)
            {
                cells.Add(pawnPath.nodes[i]);
            }
            if (cells.Count == 0)
            {
                return;
            }
            int                width  = Finder.Settings.Pathfinding_SquadPathWidth;
            ulong              sflags = flags; 
            ThingComp_CombatAI comp   = pawn.AI();
            Squad              squad  = null;
            if (comp != null)
            {
	            squad = comp.squad;
	            if (squad != null)
	            {
		            width  = Maths.Max(width, squad.members.Count);
		            sflags = squad.GetSquadFlags();
	            }
            }
            WallGrid           walls  = map.GetComp_Fast<WallGrid>();
            asyncActions.EnqueueOffThreadAction(() =>
            {
	            path.Next();
                path_squads.Next();
                IntVec3 prev = cells[0];
                for (int i = 1; i < cells.Count; i++)
                {
                    IntVec3 cur = cells[i];
                    int     dx  = Math.Sign(prev.x - cur.x);
                    int     dz  = Math.Sign(prev.z - cur.z);
                    int     val = 1;
                    IntVec3 leftOffset;
                    IntVec3 rightOffset;
                    if (dx == 0)
                    {
                        leftOffset  = new IntVec3(-1, 0, 0);
                        rightOffset = new IntVec3(1, 0, 0);
                    }
                    else if (dz == 0)
                    {
                        leftOffset  = new IntVec3(0, 0, -1);
                        rightOffset = new IntVec3(0, 0, 1);
                    }
                    else
                    {
                        leftOffset  = new IntVec3(dx, 0, 0);
                        rightOffset = new IntVec3(0, 0, dz);
                    }
                    IntVec3 left  = cur + leftOffset;
                    IntVec3 right = cur + rightOffset;
                    if (!left.InBounds(map) || walls.GetFillCategory(left) == FillCategory.Full)
                    {
	                    val++;
                    }
                    if (!right.InBounds(map) || walls.GetFillCategory(right) == FillCategory.Full)
                    {
	                    val++;
                    }
                    path.Set(left, (byte)val, flags);
                    path.Set(right, (byte)val, flags);
                    path.Set(cur, (byte)val, flags);
                    prev = cur;
                    if (sflags != 0)
                    {
	                    cells2.Clear();
	                    for (int j = 2; j <= width; j++)
	                    {
		                    IntVec3 l = cur + leftOffset * j;
		                    if (!l.InBounds(map) || walls.GetFillCategory(l) == FillCategory.Full)
		                    {
			                    val++;
		                    }
		                    IntVec3 r = cur + rightOffset * j;
		                    if (!r.InBounds(map) || walls.GetFillCategory(r) == FillCategory.Full)
		                    {
			                    val++;
		                    }
		                    cells2.Add(r);
		                    cells2.Add(l);
	                    }
	                    for (int j = 0; j < cells2.Count; j++)
	                    {
		                    path_squads.Set(cells2[j], (byte)val, sflags);
	                    }
	                    path_squads.Set(left, (byte)val, flags);
	                    path_squads.Set(right, (byte)val, flags);
	                    path_squads.Set(cur, (byte)val, flags);
                    }
                }
                cells.Clear();
                cells2.Clear();
            });
        }

        private void TryCastShootLine(Pawn pawn)
        {

        }

        private bool Valid(Pawn pawn)
        {
            return !pawn.Destroyed && pawn.Spawned && !pawn.Dead && (pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid);
        }

        private struct IBucketablePawn : IBucketable
        {
            public readonly Pawn          pawn;
            public readonly int           bucketIndex;
            public readonly List<IntVec3> tempPath;
            public readonly List<IntVec3> tempPathSquad;

            public int BucketIndex
            {
                get => bucketIndex;
            }
            public int UniqueIdNumber
            {
                get => pawn.thingIDNumber;
            }

            public IBucketablePawn(Pawn pawn, int bucketIndex)
            {
                this.pawn        = pawn;
                this.bucketIndex = bucketIndex;
                tempPath         = new List<IntVec3>(64);
                tempPathSquad    = new List<IntVec3>();
            }
        }

        public class AvoidanceReader
        {
            private readonly ulong       iflags;
            private readonly ulong       sflags;
            private readonly CellIndices indices;
            public           IHeatGrid   affliction_dmg;
            public           IHeatGrid   affliction_pen;

            public ITByteGrid path;
            public ITByteGrid path_squads;
            public ITByteGrid proximity;
            public ITByteGrid proximity_squads;

            public AvoidanceReader(AvoidanceTracker tracker, ulong iflags, ulong sflags)
            {
                ;
                indices     = tracker.map.cellIndices;
                this.iflags = iflags;
                this.sflags = sflags;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetDanger(IntVec3 cell)
            {
                return GetDanger(indices.CellToIndex(cell));
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetDanger(int index)
            {
                return affliction_dmg.Get(index) + affliction_pen.Get(index);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetProximity(IntVec3 cell)
            {
                return GetProximity(indices.CellToIndex(cell));
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetProximity(int index)
            {
                return Maths.Max(proximity.Get(index) - ((proximity.GetFlags(index) & iflags) != 0 ? 1 : 0), proximity_squads != null ? (proximity_squads.Get(index) - ((proximity_squads.GetFlags(index) & sflags) != 0 ? 1 : 0)) : 0, 0);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetPath(IntVec3 cell)
            {
                return GetPath(indices.CellToIndex(cell)) ;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetPath(int index)
            {
                return Maths.Max(path.Get(index) - ((path.GetFlags(index) & iflags) != 0 ? 1 : 0), path_squads != null ? (path_squads.Get(index) - ((path_squads.GetFlags(index) & sflags) != 0 ? 1 : 0)) : 0, 0);
            }
        }
    }
}
