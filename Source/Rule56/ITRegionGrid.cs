using System;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RimWorld;
namespace CombatAI
{
	public class ITRegionGrid
	{
		private readonly Map                           map;
		private readonly CellIndices                   cellIndices;
		private readonly IFieldInfo[]                  regions;
		private readonly IField<UInt64>[]              regions_flags;
		private readonly IField<MetaCombatAttribute>[] regions_meta;
		private readonly IField<float>[]               regions_blunt;
		private readonly IField<float>[]               regions_sharp;
		private readonly int[]                         cells_ids;
		
		private readonly int   NumGridCells;
		private         short r_sig = 19;
		
		private float               curBlunt;
		private float               curSharp;
		private UInt64              curFlag;
		private MetaCombatAttribute curMeta;

		public ITRegionGrid(Map map)
		{
			this.map      = map;
			cellIndices   = map.cellIndices;
			NumGridCells  = cellIndices.NumGridCells;
			cells_ids     = new int[NumGridCells];
			regions       = new IFieldInfo[short.MaxValue];
			regions_flags = new IField<UInt64>[short.MaxValue];
			regions_meta  = new IField<MetaCombatAttribute>[short.MaxValue];
			regions_blunt = new IField<float>[short.MaxValue];
			regions_sharp = new IField<float>[short.MaxValue];
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
		/// Set region by id.
		/// </summary>
		/// <param name="cell">Cell</param>
		public void Set(IntVec3 cell)
		{
			Set(cellIndices.CellToIndex(cell));
		}
		/// <summary>
		/// Set region by id.
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
							info.num                += 1;
							regions_flags[id].value |= curFlag;
							regions_sharp[id].value =  Maths.Max(curSharp, regions_sharp[id].value);
							regions_blunt[id].value =  Maths.Max(curBlunt, regions_blunt[id].value);
							regions_meta[id].value  |= curMeta;
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
							regions_flags[id].ReSet(curFlag, expired);
							regions_sharp[id].ReSet(curSharp, expired);
							regions_blunt[id].ReSet(curBlunt, expired);
							regions_meta[id].ReSet(curMeta, expired);
						}
						info.cycle  = CycleNum;
						info.sig    = r_sig;
						regions[id] = info;
					}
				}
			}
		}
		/// <summary>
		/// Update the region id grids.
		/// </summary>
		/// <param name="cell">Cell</param>
		/// <param name="region">Region</param>
		public void SetRegionAt(IntVec3 cell, Region region)
		{
			SetRegionAt(cellIndices.CellToIndex(cell), region);
		}
		/// <summary>
		/// Update the region id grids.
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
		/// Returns number of sources who can view a region.
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
		/// Returns number of sources who can view a region.
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
		/// Returns avg sharp at a region.
		/// </summary>
		/// <param name="region">Region.</param>
		/// <returns>Sharp</returns>
		public float GetSharpAt(Region region)
		{
			if (region != null)
			{
				return GetSharpAt(region.id);
			}
			return 0;
		}
		/// <summary>
		/// Returns avg sharp at a region.
		/// </summary>
		/// <param name="id">Region id</param>
		/// <returns>Sharp</returns>
		public float GetSharpAt(int id)
		{
			if (id != -1)
			{
				IFieldInfo cell = regions[id];
				switch (CycleNum - cell.cycle)
				{
					case 0:
						IField<float> field = regions_sharp[id];
						return Maths.Max(field.value, field.valuePrev);
					case 1:
						return regions_sharp[id].value;
					default:
						return 0;
				}
			}
			return 0;
		}
		/// <summary>
		/// Returns avg blunt at a region.
		/// </summary>
		/// <param name="region">Region.</param>
		/// <returns>Blunt</returns>
		public float GetBluntAt(Region region)
		{
			if (region != null)
			{
				return GetBluntAt(region.id);
			}
			return 0;
		}
		/// <summary>
		/// Returns avg blunt at a region.
		/// </summary>
		/// <param name="id">Region id</param>
		/// <returns>Blunt</returns>
		public float GetBluntAt(int id)
		{
			if (id != -1)
			{
				IFieldInfo cell = regions[id];
				switch (CycleNum - cell.cycle)
				{
					case 0:
						IField<float> field = regions_blunt[id];
						return Maths.Max(field.value, field.valuePrev);
					case 1:
						return regions_blunt[id].value;
					default:
						return 0;
				}
			}
			return 0;
		}
		/// <summary>
		/// Return region meta combat attributes.
		/// </summary>
		/// <param name="region">Region</param>
		/// <returns>Meta combat attribute</returns>
		public MetaCombatAttribute GetCombatAttributesAt(Region region)
		{
			if (region != null)
			{
				return GetCombatAttributesAt(region.id);
			}
			return 0;
		}
		/// <summary>
		/// Return region meta combat attributes.
		/// </summary>
		/// <param name="id">Region id</param>
		/// <returns>Meta combat attribute</returns>
		public MetaCombatAttribute GetCombatAttributesAt(int id)
		{
			if (id != -1)
			{
				IFieldInfo    cell  = regions[id];
				switch (CycleNum - cell.cycle)
				{
					case 0:
						IField<MetaCombatAttribute> field = regions_meta[id];
						return field.value | field.valuePrev;
					case 1:
						return regions_meta[id].value;
					default:
						return 0;
				}
			}
			return 0;
		}
		/// <summary>
		/// Return region flags.
		/// </summary>
		/// <param name="region">Region</param>
		/// <returns>Region flags.</returns>
		public UInt64 GetFlagsAt(Region region)
		{
			if (region != null)
			{
				return GetFlagsAt(region.id);
			}
			return 0;
		}
		/// <summary>
		/// Return region flags.
		/// </summary>
		/// <param name="id">Region id</param>
		/// <returns>Region flags.</returns>
		public UInt64 GetFlagsAt(int id)
		{
			if (id != -1)
			{
				IFieldInfo cell = regions[id];
				switch (CycleNum - cell.cycle)
				{
					case 0:
						IField<ulong> field = regions_flags[id];
						return field.value | field.valuePrev;
					case 1:
						return regions_flags[id].value;
					default:
						return 0;
				}
			}
			return 0;
		}
		/// <summary>
		/// Returns region id for cell.
		/// </summary>
		/// <param name="cell">Cell.</param>
		/// <returns>Region id</returns>
		public int GetRegionId(IntVec3 cell)
		{
			return GetRegionId(cellIndices.CellToIndex(cell));
		}
		/// <summary>
		/// Returns region id for cell index.
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
		///		TODO
		/// </summary>
		public void Next(UInt64 flag, float sharp, float blunt, MetaCombatAttribute meta)
		{
			if (r_sig++ == short.MaxValue)
			{
				r_sig = 19;
			}
			curSharp = sharp;
			curBlunt = blunt;
			curMeta  = meta;
			curFlag  = flag;
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
