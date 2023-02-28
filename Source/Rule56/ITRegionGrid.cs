using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Verse;
namespace CombatAI
{
    public abstract class ITRegionGrid
    {
        protected readonly int         NumGridCells;
        protected readonly CellIndices cellIndices;
        protected readonly Map         map;
        protected          short       r_sig = 19;

        public ITRegionGrid(Map map)
        {
            this.map          = map;
            this.cellIndices  = map.cellIndices;
            this.NumGridCells = map.cellIndices.NumGridCells;
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
        public abstract void Set(int index);
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
        public abstract void SetRegionAt(int index, Region region);
        /// <summary>
        ///     Returns number of sources who can view a region.
        /// </summary>
        /// <param name="region">Region.</param>
        /// <returns>Number of sources.</returns>
        public abstract int GetSignalNumByRegion(Region region);
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
        public abstract int GetRegionId(int index);

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
    }
}
