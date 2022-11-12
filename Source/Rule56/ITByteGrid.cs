using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatAI
{

    public class ITByteGrid
    {        
        private struct ITCell
        {
            public byte value;
            public byte valuePrev;
            public ushort sig;
            public ushort cycleNum;
            public UInt64 flags;
            public UInt64 flagsPrev;
        }

        private ushort sig = 13;
        private ushort cycleNum = 19;        
        private readonly CellIndices cellIndices;
        private readonly ITCell[] grid;

        public readonly int mapCellNum;

        public int CycleNum
        {
            get => cycleNum;
        }

        public ITByteGrid(Map map)
        {
            this.cellIndices = map.cellIndices;
            this.grid = new ITCell[cellIndices.NumGridCells];
            this.mapCellNum = cellIndices.NumGridCells;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(IntVec3 cell, byte value, UInt64 flags) => Set(cellIndices.CellToIndex(cell), value, flags);
        public void Set(int index, byte value, UInt64 flags)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ITCell cell = grid[index];
                if (cell.sig != sig)
                {
                    int dc = cycleNum - cell.cycleNum;
                    if (dc == 0)
                    {
                        cell.value = (byte)Mathf.Clamp(cell.value + value, 0, byte.MaxValue);
                        cell.flags |= flags;
                    }
                    else
                    {
                        if (dc == 1)
                        {
                            cell.valuePrev = cell.value;
                            cell.flagsPrev = cell.flags;
                        }
                        else
                        {
                            cell.valuePrev = 0;
                            cell.flagsPrev = 0;
                        }
                        cell.cycleNum = cycleNum;
                        cell.value = value;
                        cell.flags = flags;
                    }                    
                    cell.sig = sig;
                    grid[index] = cell;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Get(IntVec3 cell) => Get(cellIndices.CellToIndex(cell));
        public byte Get(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ITCell cell = grid[index];
                int dc = cycleNum - cell.cycleNum;
                switch (dc)
                {
                    case 0:
                        return Math.Max(cell.value, cell.valuePrev);
                    case 1:
                        return cell.value;
                    default:
                        return 0;
                }
            }
            return 0;
        }

        public UInt64 GetFlags(IntVec3 cell) => GetFlags(cellIndices.CellToIndex(cell));
        public UInt64 GetFlags(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ITCell cell = grid[index];
                int dc = cycleNum - cell.cycleNum;
                switch (dc)
                {
                    case 0:
                        return cell.flags | cell.flagsPrev;
                    case 1:
                        return cell.flags;
                    default:
                        return 0;
                }
            }
            return 0;
        }        

        public void Next()
        {
            if (sig++ == short.MaxValue)
            {
                sig = 13;
            }
        }

        public void NextCycle()
        {
            Next();
            if (cycleNum++ == short.MaxValue)
            {
                sig = 13;
            }
        }
    }
}

