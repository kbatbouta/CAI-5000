using System.Runtime.CompilerServices;
using Verse;
namespace CombatAI
{
    public class RoofGrid : MapComponent
    {
        private readonly CellIndices cellIndices;
        private readonly RoofType[]  grid;

        public RoofGrid(Map map) : base(map)
        {
            cellIndices = map.cellIndices;
            grid        = new RoofType[cellIndices.NumGridCells];
        }

        public RoofType this[IntVec3 c]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this[cellIndices.CellToIndex(c)];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this[cellIndices.CellToIndex(c)] = value;
        }

        public RoofType this[int index]
        {
            get => grid[index];
            set => grid[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRoof(IntVec3 c, RoofDef def)
        {
            SetRoof(cellIndices.CellToIndex(c), def);
        }
        public void SetRoof(int index, RoofDef def)
        {
            if (index >= 0 && index < cellIndices.NumGridCells)
            {
                if (def == null)
                {
                    grid[index] = RoofType.None;
                }
                else
                {
                    RoofType val = RoofType.None;
                    if (def.isThickRoof)
                    {
                        val |= RoofType.RockThick;
                    }
                    else
                    {
                        val |= RoofType.RockThin;
                    }
                    if (!def.isNatural)
                    {
                        val |= RoofType.Constructed;
                    }
                    grid[index] = val;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RoofType GetRoofType(IntVec3 c)
        {
            return GetRoofType(cellIndices.CellToIndex(c));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RoofType GetRoofType(int index)
        {
            if (index >= 0 && index < cellIndices.NumGridCells)
            {
                return grid[index];
            }
            return RoofType.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Roofed(IntVec3 c)
        {
            return Roofed(cellIndices.CellToIndex(c));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Roofed(int index)
        {
            return GetRoofType(index) != RoofType.None;
        }
    }
}
