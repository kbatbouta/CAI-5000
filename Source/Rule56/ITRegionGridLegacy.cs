using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Verse;
namespace CombatAI
{
    public class ITRegionGridLegacy : ITRegionGrid
    {
        private readonly int[]        cells_ids;
        private readonly IFieldInfo[] regions;
        
        public ITRegionGridLegacy(Map map) : base(map)
        {
            cells_ids    = new int[NumGridCells];
            regions      = new IFieldInfo[short.MaxValue];
            for (int i = 0; i < NumGridCells; i++)
            {
                cells_ids[i] = -1;
            }
        }
        
        /// <summary>
        ///     Set region by id.
        /// </summary>
        /// <param name="index">Cell index.</param>
        public override void Set(int index)
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
        /// <param name="index">Cell index</param>
        /// <param name="region">region</param>
        public override void SetRegionAt(int index, Region region)
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
        public override int GetSignalNumByRegion(Region region)
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
        ///     Returns region id for cell index.
        /// </summary>
        /// <param name="index">Cell index.</param>
        /// <returns>Region id</returns>
        public override int GetRegionId(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                return cells_ids[index];
            }
            return -1;
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
