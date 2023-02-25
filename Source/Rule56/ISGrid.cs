using System;
using System.Runtime.CompilerServices;
using Verse;
namespace CombatAI
{
    public class ISGrid<T> where T : struct, IComparable<T>
    {
        private readonly Cell[]      cells;
        private readonly CellIndices indices;

        private int sig = 1;

        public ISGrid(Map map)
        {
            indices = map.cellIndices;
            cells   = new Cell[indices.NumGridCells];
        }

        public T this[IntVec3 cell]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this[indices.CellToIndex(cell)];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this[indices.CellToIndex(cell)] = value;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Cell cell = cells[index];
                return cell.sig == sig ? cell.value : default(T);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => cells[index] = new Cell
            {
                sig   = sig,
                value = value
            };
        }

        public void Reset()
        {
            sig++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(IntVec3 cell)
        {
            return IsSet(indices.CellToIndex(cell));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int index)
        {
            return cells[index].sig == sig;
        }

        private struct Cell
        {
            public T   value;
            public int sig;
        }
    }
}
