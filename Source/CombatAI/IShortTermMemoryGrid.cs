using System;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace CombatAI
{
    public class IShortTermMemoryGrid
    {        
        private struct IShortTermMemory
        {
            public int expireAt;
            public float Value
            {                
                get
                {
                    int ticks = GenTicks.TicksGame;
                    if (expireAt >= ticks)
                    {
                        return (float)(expireAt - ticks) / 60f;
                    }
                    return 0f;
                }
            }

            public void Set(float value, float max)
            {
                expireAt = (int) Math.Min((int)(value * 60), max) + GenTicks.TicksGame;
            }
        }

        public Map map;
        public CellIndices cellIndices;

        private float alpha;       
        private float maxTicks;
        private IShortTermMemory[] records;

        public IShortTermMemoryGrid(Map map, int ticksPerUnit, int maxUnit)
        {
            this.alpha = ticksPerUnit / 60f;            
            this.maxTicks = maxUnit * alpha * 60;
            this.map = map;
            this.cellIndices = map.cellIndices;
            this.records = new IShortTermMemory[this.cellIndices.NumGridCells];            
        }

        public float this[IntVec3 cell]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this[cellIndices.CellToIndex(cell)];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this[cellIndices.CellToIndex(cell)] = value;
        }

        public float this[int index]
        {
            get
            {
                if(index >= 0 && index < map.cellIndices.NumGridCells)
                {                    
                    return records[index].Value / alpha;
                }
                return 0f;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index >= 0 && index < map.cellIndices.NumGridCells)
                {
                    records[index].Set(value * alpha, maxTicks);                    
                }
            }
        }
    }
}

