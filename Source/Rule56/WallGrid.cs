using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
namespace CombatAI
{
    public class WallGrid : MapComponent
    {
        private readonly CellIndices cellIndices;
        private readonly float[]     grid;
        private readonly float[]     gridNoDoors;

        public WallGrid(Map map) : base(map)
        {
            cellIndices = map.cellIndices;
            grid        = new float[cellIndices.NumGridCells];
            gridNoDoors = new float[cellIndices.NumGridCells];
        }

        public float this[IntVec3 cell]
        {
            get => this[cellIndices.CellToIndex(cell)];
        }

        public float this[int index]
        {
            get => grid[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FillCategory GetFillCategory(IntVec3 cell)
        {
            return GetFillCategory(cellIndices.CellToIndex(cell));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FillCategory GetFillCategory(int index)
        {
            float f = grid[index];
            if (f == 0)
            {
                return FillCategory.None;
            }
            if (f < 1f)
            {
                return FillCategory.Partial;
            }
            return FillCategory.Full;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FillCategory GetFillCategoryNoDoors(IntVec3 cell)
        {
            return GetFillCategoryNoDoors(cellIndices.CellToIndex(cell));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FillCategory GetFillCategoryNoDoors(int index)
        {
            float f = gridNoDoors[index];
            if (f == 0)
            {
                return FillCategory.None;
            }
            if (f < 1f)
            {
                return FillCategory.Partial;
            }
            return FillCategory.Full;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBeSeenOver(IntVec3 cell)
        {
            return CanBeSeenOver(cellIndices.CellToIndex(cell));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBeSeenOver(int index)
        {
            return grid[index] < 0.998f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBeSeenOverNoDoors(IntVec3 cell)
        {
            return CanBeSeenOverNoDoors(cellIndices.CellToIndex(cell));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBeSeenOverNoDoors(int index)
        {
            return gridNoDoors[index] < 0.998f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecalculateCell(IntVec3 cell, Thing t)
        {
            RecalculateCell(cellIndices.CellToIndex(cell), t);
        }
        public void RecalculateCell(int index, Thing t)
        {
            if (t != null)
            {
                if (t.def.plant != null)
                {
                    if (t.def.plant.IsTree)
                    {
                        if (t is Plant plant)
                        {
                            gridNoDoors[index] = grid[index] = plant.Growth * t.def.fillPercent / 4f;
                        }
                        else
                        {
                            gridNoDoors[index] = grid[index] = t.def.fillPercent / 4f;
                        }
                    }
                }
                else if (t is Building_Door door)
                {
                    grid[index]        = 1 - door.OpenPct;
                    gridNoDoors[index] = 0;
                }
                else if (t is Building ed && ed.def.Fillage == FillCategory.Full)
                {
                    gridNoDoors[index] = grid[index] = 1.0f;
                }
                else
                {
                    gridNoDoors[index] = grid[index] = t.def.fillPercent;
                }
            }
            else
            {
                gridNoDoors[index] = grid[index] = 0;
            }
        }
    }
}
