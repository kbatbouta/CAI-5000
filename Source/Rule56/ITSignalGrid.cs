using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;
namespace CombatAI
{
    /*
     * -----------------------------
     *
     *
     * ------ Important note -------
     * 
     * when casting update the grid at a regualar intervals for a pawn/Thing or risk exploding value issues.
     */
    [StaticConstructorOnStartup]
    public class ITSignalGrid
    {
        private readonly IFieldInfo[]                  cells;
        private readonly IField<byte>[]                cells_aiming;
        private readonly IField<Vector2>[]             cells_dist;
        private readonly IField<Vector2>[]             cells_dir;
        private readonly IField<ulong>[]               cells_staticFlags;
        private readonly IField<ulong>[]               cells_dynamicFlags;
        private readonly IField<MetaCombatAttribute>[] cells_meta;
        private readonly IField<float>[]               cells_sharp;
        private readonly IField<float>[]               cells_blunt;
        private readonly IField<float>[]               cells_strength;

        private readonly CellIndices indices;

        public readonly int                 NumGridCells;
        public          IntVec3             curRoot;
        public          byte                curAimAvailablity;
        public          float               curBlunt;
        public          MetaCombatAttribute curMeta;
        public          ulong               curFlags;
        public          float               curSharp;


        private short r_sig = 19;

        public ITSignalGrid(Map map)
        {
            indices      = map.cellIndices;
            NumGridCells = indices.NumGridCells;

            cells              = new IFieldInfo[NumGridCells];
            cells_strength     = new IField<float>[NumGridCells];
            cells_dir          = new IField<Vector2>[NumGridCells];
            cells_dist         = new IField<Vector2>[NumGridCells];
            cells_staticFlags  = new IField<ulong>[NumGridCells];
            cells_dynamicFlags = new IField<ulong>[NumGridCells];
            cells_blunt        = new IField<float>[NumGridCells];
            cells_sharp        = new IField<float>[NumGridCells];
            cells_meta         = new IField<MetaCombatAttribute>[NumGridCells];
            cells_aiming       = new IField<byte>[NumGridCells];
        }

        public short CycleNum
        {
            get;
            private set;
        } = 19;

        private Vector2 ToV2(IntVec3 vec)
        {
	        return new Vector2(vec.x, vec.z);
        }
        
        private IntVec3 ToIntV3(Vector2 vec)
        {
	        return new IntVec3((int)vec.x, 0, (int)vec.y);
        }

        public void Set(IntVec3 cell, float signalStrength, Vector2 dir)
        {
            Set(indices.CellToIndex(cell), signalStrength, dir);
        }
        public void Set(int index, float signalStrength, Vector2 dir)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo info = cells[index];
                if (info.sig != r_sig)
                {
                    int dc = CycleNum - info.cycle;
                    if (dc == 0)
                    {
                        info.num                        += 1;
                        cells_strength[index].value     += signalStrength;
                        cells_dir[index].value          += dir;
                        cells_meta[index].value         |= curMeta;
                        if (curRoot.IsValid)
                        {
	                        Vector2 distVec = ToV2( curRoot - indices.IndexToCell(index));
	                        if (distVec.sqrMagnitude < cells_dist[index].value.sqrMagnitude)
	                        {
		                        cells_dist[index].value = distVec;
	                        }
                        }
                        cells_sharp[index].value        =  Maths.Max(curSharp, cells_sharp[index].value);
                        cells_blunt[index].value        =  Maths.Max(curBlunt, cells_blunt[index].value);
                        cells_aiming[index].value       += curAimAvailablity;
                        cells_dynamicFlags[index].value |= curFlags;
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
                        cells_strength[index].ReSet(signalStrength, expired);
                        cells_dir[index].ReSet(dir, expired);
                        if (curRoot.IsValid)
                        {
	                        cells_dist[index].ReSet(ToV2(curRoot - indices.IndexToCell(index) ), expired);
                        }
                        else
                        {
	                        cells_dist[index].ReSet(new Vector2(1000, 1000), expired);
                        }
                        cells_staticFlags[index].ReSet(0, expired);
                        cells_dynamicFlags[index].ReSet(curFlags, expired);
                        cells_meta[index].ReSet(curMeta, expired);
                        cells_sharp[index].ReSet(curSharp, expired);
                        cells_blunt[index].ReSet(curBlunt, expired);
                        cells_aiming[index].ReSet(curAimAvailablity, expired);
                        info.cycle = CycleNum;
                    }
                    info.sig     = r_sig;
                    cells[index] = info;
                }
            }
        }

        public void Set(IntVec3 cell, float signalStrength, Vector2 dir, MetaCombatAttribute metaAttributes)
        {
            Set(indices.CellToIndex(cell), signalStrength, dir, metaAttributes);
        }
        public void Set(int index, float signalStrength, Vector2 dir, MetaCombatAttribute metaAttributes)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo info = cells[index];
                if (info.sig != r_sig)
                {
                    int dc = CycleNum - info.cycle;
                    if (dc == 0)
                    {
                        info.num                        += 1;
                        cells_strength[index].value     += signalStrength;
                        cells_dir[index].value          += dir;
                        cells_meta[index].value         |= metaAttributes;
                        if (curRoot.IsValid)
                        {
	                        Vector2 distVec = ToV2( curRoot - indices.IndexToCell(index) );
	                        if (distVec.sqrMagnitude < cells_dist[index].value.sqrMagnitude)
	                        {
		                        cells_dist[index].value = distVec;
	                        }
                        }
                        cells_sharp[index].value        =  Maths.Max(curSharp, cells_sharp[index].value);
                        cells_blunt[index].value        =  Maths.Max(curBlunt, cells_blunt[index].value);
                        cells_aiming[index].value       += curAimAvailablity;
                        cells_dynamicFlags[index].value |= curFlags;
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
                        cells_strength[index].ReSet(signalStrength, expired);
                        cells_dir[index].ReSet(dir, expired);
                        cells_staticFlags[index].ReSet(0, expired);
                        cells_dynamicFlags[index].ReSet(curFlags, expired);
                        if (curRoot.IsValid)
                        {
	                        cells_dist[index].ReSet(ToV2( curRoot - indices.IndexToCell(index)), expired);
                        }
                        else
                        {
	                        cells_dist[index].ReSet(new Vector2(1000, 1000), expired);
                        }
                        cells_meta[index].ReSet(metaAttributes, expired);
                        cells_sharp[index].ReSet(curSharp, expired);
                        cells_blunt[index].ReSet(curBlunt, expired);
                        cells_aiming[index].ReSet(curAimAvailablity, expired);
                        info.cycle = CycleNum;
                    }
                    info.sig     = r_sig;
                    cells[index] = info;
                }
            }
        }

        public void Set(IntVec3 cell, MetaCombatAttribute metaAttributes)
        {
            Set(indices.CellToIndex(cell), metaAttributes);
        }
        public void Set(int index, MetaCombatAttribute metaAttributes)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo info = cells[index];
                if (info.sig != r_sig)
                {
                    int dc = CycleNum - info.cycle;
                    if (dc == 0)
                    {
                        cells_meta[index].value         |= metaAttributes;
                        cells_sharp[index].value        =  Maths.Max(curSharp, cells_sharp[index].value);
                        cells_blunt[index].value        =  Maths.Max(curBlunt, cells_blunt[index].value);
                        cells_aiming[index].value       += curAimAvailablity;
                        cells_dynamicFlags[index].value |= curFlags;
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
                        info.num = 0;
                        cells_strength[index].ReSet(0, expired);
                        cells_dir[index].ReSet(Vector2.zero, expired);
                        cells_staticFlags[index].ReSet(0, expired);
                        cells_dynamicFlags[index].ReSet(curFlags, expired);
                        cells_meta[index].ReSet(metaAttributes, expired);
                        cells_dist[index].ReSet(new Vector2(1000, 1000), expired);
                        cells_sharp[index].ReSet(curSharp, expired);
                        cells_blunt[index].ReSet(curBlunt, expired);
                        cells_aiming[index].ReSet(curAimAvailablity, expired);
                        info.cycle = CycleNum;
                    }
                    info.sig     = r_sig;
                    cells[index] = info;
                }
            }
        }

        public void Set(IntVec3 cell, ulong flags)
        {
            Set(indices.CellToIndex(cell), flags);
        }
        public void Set(int index, ulong flags)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo info = cells[index];
                if (info.sig != r_sig)
                {
                    int dc = CycleNum - info.cycle;
                    if (dc == 0)
                    {
                        cells_staticFlags[index].value  |= flags;
                        cells_sharp[index].value  =  Maths.Max(curSharp, cells_sharp[index].value);
                        cells_blunt[index].value  =  Maths.Max(curBlunt, cells_blunt[index].value);
                        cells_aiming[index].value += curAimAvailablity;
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
                        info.num = 0;
                        cells_strength[index].ReSet(0, expired);
                        cells_dir[index].ReSet(Vector2.zero, expired);
                        cells_staticFlags[index].ReSet(flags, expired);
                        cells_dynamicFlags[index].ReSet(curFlags, expired);
                        cells_meta[index].ReSet(curMeta, expired);
                        cells_dist[index].ReSet(new Vector2(1000, 1000), expired);
                        cells_sharp[index].ReSet(curSharp, expired);
                        cells_blunt[index].ReSet(curBlunt, expired);
                        cells_aiming[index].ReSet(curAimAvailablity, expired);
                        info.cycle = CycleNum;
                    }
                    info.sig     = r_sig;
                    cells[index] = info;
                }
            }
        }

        public void Set(IntVec3 cell, float signalStrength, Vector2 dir, ulong flags)
        {
            Set(indices.CellToIndex(cell), signalStrength, dir, flags);
        }
        public void Set(int index, float signalStrength, Vector2 dir, ulong flags)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo info = cells[index];
                if (info.sig != r_sig)
                {
                    int dc = CycleNum - info.cycle;
                    if (dc == 0)
                    {
                        info.num                    += 1;
                        cells_strength[index].value += signalStrength;
                        cells_dir[index].value      += dir;
                        cells_staticFlags[index].value    |= flags;
                        cells_meta[index].value     |= curMeta;
                        if (curRoot.IsValid)
                        {
	                        Vector2 distVec = ToV2( curRoot - indices.IndexToCell(index));
	                        if (distVec.sqrMagnitude < cells_dist[index].value.sqrMagnitude)
	                        {
		                        cells_dist[index].value = distVec;
	                        }
                        }
                        cells_sharp[index].value    =  Maths.Max(curSharp, cells_sharp[index].value);
                        cells_blunt[index].value    =  Maths.Max(curBlunt, cells_blunt[index].value);
                        cells_aiming[index].value   += curAimAvailablity;
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
                        cells_strength[index].ReSet(signalStrength, expired);
                        cells_dir[index].ReSet(dir, expired);
                        cells_staticFlags[index].ReSet(flags, expired);
                        cells_dynamicFlags[index].ReSet(curFlags, expired);
                        if (curRoot.IsValid)
                        {
	                        cells_dist[index].ReSet(ToV2(curRoot - indices.IndexToCell(index) ), expired);
                        }
                        else
                        {
	                        cells_dist[index].ReSet(new Vector2(1000, 1000), expired);
                        }
                        cells_meta[index].ReSet(curMeta, expired);
                        cells_sharp[index].ReSet(curSharp, expired);
                        cells_blunt[index].ReSet(curBlunt, expired);
                        cells_aiming[index].ReSet(curAimAvailablity, expired);
                        info.cycle = CycleNum;
                    }
                    info.sig     = r_sig;
                    cells[index] = info;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(IntVec3 cell)
        {
            return IsSet(indices.CellToIndex(cell));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                return cells[index].sig == r_sig;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSignalNum(IntVec3 cell)
        {
            return GetSignalNum(indices.CellToIndex(cell));
        }
        public int GetSignalNum(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo cell = cells[index];
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete]
        public float GetRawSignalStrengthAt(IntVec3 cell)
        {
            return GetRawSignalStrengthAt(indices.CellToIndex(cell));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete]
        public float GetRawSignalStrengthAt(int index)
        {
            return GetSignalStrengthAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetSignalStrengthAt(IntVec3 cell)
        {
            return GetSignalStrengthAt(indices.CellToIndex(cell));
        }
        public float GetSignalStrengthAt(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo cell = cells[index];
                switch (CycleNum - cell.cycle)
                {
                    case 0:
                        IField<float> strength = cells_strength[index];

                        return Maths.Max(strength.value, strength.valuePrev);
                    case 1:
                        return cells_strength[index].value;
                }
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetSignalStrengthAt(IntVec3 cell, out int signalNum)
        {
            return GetSignalStrengthAt(indices.CellToIndex(cell), out signalNum);
        }
        public float GetSignalStrengthAt(int index, out int signalNum)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo cell = cells[index];
                switch (CycleNum - cell.cycle)
                {
                    case 0:
                        IField<float> strength = cells_strength[index];
                        signalNum = Maths.Max(cell.num, cell.numPrev);
                        return Maths.Max(strength.value, strength.valuePrev);
                    case 1:
                        signalNum = cell.num;
                        return cells_strength[index].value;
                }
            }
            return signalNum = 0;
        }

        public ulong GetStaticFlagsAt(IntVec3 cell)
        {
            return GetStaticFlagsAt(indices.CellToIndex(cell));
        }
        public ulong GetStaticFlagsAt(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo cell = cells[index];
                switch (CycleNum - cell.cycle)
                {
                    case 0:
                        IField<ulong> flags = cells_staticFlags[index];

                        return flags.value | flags.valuePrev;
                    case 1:
                        return cells_staticFlags[index].value;
                }
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntVec3 GetNearestSourceAt(int index)
        {
	        return GetNearestSourceAtInner(index, indices.IndexToCell(index));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntVec3 GetNearestSourceAt(IntVec3 pos)
        {
	        return GetNearestSourceAtInner(indices.CellToIndex(pos), pos);
        }
        private IntVec3 GetNearestSourceAtInner(int index, IntVec3 pos)
        {
	        if (index >= 0 && index < NumGridCells)
	        {
		        IFieldInfo cell = cells[index];
		        Vector2    offset;
		        switch (CycleNum - cell.cycle)
		        {
			        case 0:
				        IField<Vector2> dist = cells_dist[index];
				        offset = (dist.value.sqrMagnitude < dist.valuePrev.sqrMagnitude ? dist.value : dist.valuePrev);
				        if (offset.x > 300 || offset.y > 300)
				        {
					        return IntVec3.Invalid;
				        }
				        return pos + ToIntV3(offset);
			        case 1:
				        offset = cells_dist[index].value;
				        if (offset.x > 300 || offset.y > 300)
				        {
					        return IntVec3.Invalid;
				        }
				        return pos + ToIntV3(offset);
		        }
	        }
	        return IntVec3.Invalid;
        }

        public ulong GetDynamicFlagsAt(IntVec3 cell)
        {
	        return GetDynamicFlagsAt(indices.CellToIndex(cell));
        }
        public ulong GetDynamicFlagsAt(int index)
        {
	        if (index >= 0 && index < NumGridCells)
	        {
		        IFieldInfo cell = cells[index];
		        switch (CycleNum - cell.cycle)
		        {
			        case 0:
				        IField<ulong> flags = cells_dynamicFlags[index];

				        return flags.value | flags.valuePrev;
			        case 1:
				        return cells_dynamicFlags[index].value;
		        }
	        }
	        return 0;
        }

        public MetaCombatAttribute GetCombatAttributesAt(IntVec3 cell)
        {
            return GetCombatAttributesAt(indices.CellToIndex(cell));
        }
        public MetaCombatAttribute GetCombatAttributesAt(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo cell = cells[index];
                switch (CycleNum - cell.cycle)
                {
                    case 0:
                        IField<MetaCombatAttribute> flags = cells_meta[index];

                        return flags.value | flags.valuePrev;
                    case 1:
                        return cells_meta[index].value;
                }
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetSharp(IntVec3 cell)
        {
            return GetSharp(indices.CellToIndex(cell));
        }
        public float GetSharp(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo cell = cells[index];
                switch (CycleNum - cell.cycle)
                {
                    case 0:
                        IField<float> sharp = cells_sharp[index];

                        return Maths.Max(sharp.value, sharp.valuePrev);
                    case 1:
                        return cells_sharp[index].value;
                }
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetBlunt(IntVec3 cell)
        {
            return GetBlunt(indices.CellToIndex(cell));
        }
        public float GetBlunt(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo cell = cells[index];
                switch (CycleNum - cell.cycle)
                {
                    case 0:
                        IField<float> blunt = cells_blunt[index];

                        return Maths.Max(blunt.value, blunt.valuePrev);
                    case 1:
                        return cells_blunt[index].value;
                }
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetAvailability(IntVec3 cell)
        {
            return GetAvailability(indices.CellToIndex(cell));
        }
        public int GetAvailability(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo cell = cells[index];
                switch (CycleNum - cell.cycle)
                {
                    case 0:
                        IField<byte> availability = cells_aiming[index];
                        return Maths.Max(availability.value, availability.valuePrev);
                    case 1:
                        return cells_aiming[index].value;
                }
            }
            return 0;
        }

        public Vector2 GetSignalDirectionAt(IntVec3 cell)
        {
            return GetSignalDirectionAt(indices.CellToIndex(cell));
        }
        public Vector2 GetSignalDirectionAt(int index)
        {
            if (index >= 0 && index < NumGridCells)
            {
                IFieldInfo cell = cells[index];
                switch (CycleNum - cell.cycle)
                {
                    case 0:
                        IField<Vector2> dir = cells_dir[index];

                        return cell.num >= cell.numPrev ? dir.value / (cell.num + 0.01f) : dir.valuePrev / (cell.numPrev + 0.01f);
                    case 1:
                        return cells_dir[index].value / (cell.num + 0.01f);
                }

            }
            return Vector2.zero;
        }


        /// <summary>
        ///     Prepare the grid for a new casting operation.
        /// </summary>
        /// <param name="sharp">Sharp damage output/s</param>
        /// <param name="blunt">Blunt damage output/s</param>
        /// <param name="meta">Meta flags.</param>
        public void Next(IntVec3 root, ulong flags, float sharp, float blunt, MetaCombatAttribute meta)
        {
            if (r_sig++ == short.MaxValue)
            {
                r_sig = 19;
            }
            curRoot  = root;
            curFlags = flags;
            curSharp = sharp;
            curBlunt = blunt;
            curMeta  = meta;
            if ((meta & MetaCombatAttribute.Free) != 0)
            {
                curAimAvailablity = 1;
            }
            else
            {
                curAimAvailablity = 0;
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
