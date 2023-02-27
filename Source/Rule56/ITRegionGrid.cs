using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Verse;
namespace CombatAI
{
    public class ITRegionGrid
    {
        private readonly CellIndices cellIndices;
        private readonly int[]       cells_ids;
        private readonly Map         map;

        private readonly int          NumGridCells;
        private readonly IFieldInfo[] regions;

        private short r_sig = 19;

        public ITRegionGrid(Map map)
        {
            this.map     = map;
            cellIndices  = map.cellIndices;
            NumGridCells = cellIndices.NumGridCells;
            cells_ids    = new int[NumGridCells];
            regions      = new IFieldInfo[short.MaxValue];
            for (int i = 0; i < NumGridCells; i++)
            {
                cells_ids[i] = -1;
            }
        }

        public short CycleNum
        {
            get;
            private set;
        } = 19;

        /// <summary>
        ///     Set region by id.
        /// </summary>
        /// <param name="cell">Cell</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(IntVec3 cell)
        {
            Set(cellIndices.CellToIndex(cell));
        }
        /// <summary>
        ///     Set region by id.
        /// </summary>
        /// <param name="index">Cell index.</param>
        public void Set(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                int id = cells_ids[index];
                if (id >= 0 && id < short.MaxValue)
                {
                    IFieldInfo info = regions[id];
                    if (info.sig != r_sig)
                    {
                        int dc = CycleNum - info.cycle;
                        if (dc == 0)
                        {
                            info.num += 1;
                        }
                        else
                        {
                            bool expired = dc > 1;
                            if (expired)
                            {
                                info.numPrev = 0;
                            }
                            else
                            {
                                info.numPrev = info.num;
                            }
                            info.num = 1;
                        }
                        info.cycle  = CycleNum;
                        info.sig    = r_sig;
                        regions[id] = info;
                    }
                }
            }
        }
        /// <summary>
        ///     Update the region id grids.
        /// </summary>
        /// <param name="cell">Cell</param>
        /// <param name="region">Region</param>
        public void SetRegionAt(IntVec3 cell, Region region)
        {
            SetRegionAt(cellIndices.CellToIndex(cell), region);
        }
        /// <summary>
        ///     Update the region id grids.
        /// </summary>
        /// <param name="index">Cell index</param>
        /// <param name="region">region</param>
        public void SetRegionAt(int index, Region region)
        {
            if (index >= 0 && index < NumGridCells)
            {
                cells_ids[index] = region?.id ?? -1;
            }
        }

        /// <summary>
        ///     Returns number of sources who can view a region.
        /// </summary>
        /// <param name="region">Region.</param>
        /// <returns>Number of sources.</returns>
        public int GetSignalNumByRegion(Region region)
        {
            if (region != null)
            {
                return GetSignalNumById(region.id);
            }
            return 0;
        }
        /// <summary>
        ///     Returns number of sources who can view a region.
        /// </summary>
        /// <param name="id">Region id.</param>
        /// <returns>Number of sources.</returns>
        public int GetSignalNumById(int id)
        {
            if (id != -1)
            {
                IFieldInfo cell = regions[id];
                switch (CycleNum - cell.cycle)
                {
                    case 0:
                        return Maths.Max(cell.num, cell.numPrev);
                    case 1:
                        return cell.num;
                    default:
                        return 0;
                }
            }
            return 0;
        }
        /// <summary>
        ///     Returns region id for cell.
        /// </summary>
        /// <param name="cell">Cell.</param>
        /// <returns>Region id</returns>
        public int GetRegionId(IntVec3 cell)
        {
            return GetRegionId(cellIndices.CellToIndex(cell));
        }
        /// <summary>
        ///     Returns region id for cell index.
        /// </summary>
        /// <param name="index">Cell index.</param>
        /// <returns>Region id</returns>
        public int GetRegionId(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                return cells_ids[index];
            }
            return -1;
        }

        /// <summary>
        ///     TODO
        /// </summary>
        public void Next()
        {
            if (r_sig++ == short.MaxValue)
            {
                r_sig = 19;
            }
        }

        /// <summary>
        ///     TODO
        /// </summary>
        public void NextCycle()
        {
            if (r_sig++ == short.MaxValue)
            {
                r_sig = 19;
            }
            if (CycleNum++ == short.MaxValue)
            {
                CycleNum = 13;
            }
        }

        private struct IField<T> where T : struct
        {
            public T value;
            public T valuePrev;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReSet(T newVal, bool expired)
            {
                valuePrev = expired ? default(T) : value;
                value     = newVal;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IFieldInfo
        {
            public short cycle;
            public short sig;
            public short num;
            public short numPrev;
        }
    }
}
