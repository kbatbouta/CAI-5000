using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public class IHeatGrid
    {
        private readonly float       f1;
        private readonly int[]       grid;
        private readonly CellIndices indices;
        private readonly int         maxHeat;
        private readonly int         maxTicks;
        private readonly int         numGridCells;
        private readonly TickManager tickManager;
        private readonly int         ticksPerUnit;

        public IHeatGrid(Map map, int ticksPerUnit, int maxHeat, float f1)
        {
            indices           = map.cellIndices;
            grid              = new int[indices.NumGridCells];
            this.ticksPerUnit = ticksPerUnit;
            this.maxHeat      = maxHeat;
            maxTicks          = maxHeat * ticksPerUnit;
            tickManager       = Current.Game.tickManager;
            if (tickManager == null)
            {
                throw new Exception("TickManager cannot be null.");
            }
            numGridCells = indices.NumGridCells;
            this.f1      = f1;
        }

        public void Push(IntVec3 cell, float amount)
        {
            Push(indices.CellToIndex(cell), amount);
        }
        public void Push(int index, float amount)
        {
            if (index >= 0 && index < numGridCells)
            {
                if (amount >= maxHeat)
                {
                    grid[index] = GenTicks.TicksGame + maxTicks;
                }
                else
                {
                    grid[index] = Maths.Min(Maths.Max(grid[index], tickManager.ticksGameInt) + Mathf.CeilToInt(amount * ticksPerUnit), tickManager.ticksGameInt + maxTicks);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Get(IntVec3 cell)
        {
            return Get(indices.CellToIndex(cell));
        }
        public float Get(int index)
        {
            if (index >= 0 && index < numGridCells)
            {
                float dt = grid[index] - tickManager.ticksGameInt;
                if (dt > 0)
                {
                    float value = Maths.Max(dt / ticksPerUnit, 0f);
                    if (value > f1)
                    {
                        return value - f1 + 1;
                    }
                    return value / f1;
                }
            }
            return 0f;
        }
    }
}
