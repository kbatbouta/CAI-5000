using System.Collections.Generic;
using CombatAI.Comps;
using Verse;
namespace CombatAI
{
    public class WallCCTVTracker : MapComponent
    {
        private static readonly List<Thing> _destroyList = new List<Thing>();
        private readonly        FlagArray   grid;
        private readonly        CellIndices indices;

        public WallCCTVTracker(Map map) : base(map)
        {
            indices = map.cellIndices;
            grid    = new FlagArray(indices.NumGridCells);
        }

        public void Register(Thing thing)
        {
            if (thing == null || !thing.Spawned)
            {
                return;
            }
            ThingComp_CCTVTop top = thing.GetComp_Fast<ThingComp_CCTVTop>();
            if (top != null && top.Props.wallMounted)
            {
                Rot4    rot    = thing.Rotation;
                IntVec3 center = thing.Position;
                IntVec3 wall;
                if (rot == Rot4.North || rot == Rot4.South)
                {
                    wall = center + IntVec3.North.RotatedBy(rot);
                }
                else
                {
                    wall = center + IntVec3.South.RotatedBy(rot);
                }
                if (wall.InBounds(map))
                {
                    grid.Set(indices.CellToIndex(wall), true);
                }
            }
        }

        public void Register(ThingComp_CCTVTop top)
        {
            if (top != null && top.Props.wallMounted && top.parent.Spawned)
            {
                Rot4    rot    = top.parent.Rotation;
                IntVec3 center = top.parent.Position;
                IntVec3 wall;
                if (rot == Rot4.North || rot == Rot4.South)
                {
                    wall = center + IntVec3.North.RotatedBy(rot);
                }
                else
                {
                    wall = center + IntVec3.South.RotatedBy(rot);
                }
                if (wall.InBounds(map))
                {
                    grid.Set(indices.CellToIndex(wall), true);
                }
            }
        }

        public void Notify_CellChanged(IntVec3 cell)
        {
            if (cell.InBounds(map) && grid.Get(indices.CellToIndex(cell)) && !cell.Impassable(map))
            {
                grid.Set(indices.CellToIndex(cell), false);
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 loc = cell + GenAdj.AdjacentCells[i];
                    _destroyList.Clear();
                    if (loc.InBounds(map))
                    {
                        foreach (Thing thing in loc.GetThingList(map))
                        {
                            ThingComp_CCTVTop top = thing.GetComp_Fast<ThingComp_CCTVTop>();
                            if (top != null && top.Props.wallMounted)
                            {
                                Rot4    rot    = thing.Rotation;
                                IntVec3 center = thing.Position;
                                IntVec3 wall;
                                if (rot == Rot4.North || rot == Rot4.South)
                                {
                                    wall = center + IntVec3.North.RotatedBy(rot);
                                }
                                else
                                {
                                    wall = center + IntVec3.South.RotatedBy(rot);
                                }
                                if (wall.InBounds(map) && wall == cell)
                                {
                                    _destroyList.Add(thing);
                                }
                            }
                        }
                    }
                    for (int j = 0; j < _destroyList.Count; j++)
                    {
                        _destroyList[j].Destroy(DestroyMode.Deconstruct);
                    }
                }
                _destroyList.Clear();
            }
        }
    }
}
