using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Runtime.CompilerServices;
using System.Reflection;
using RimWorld.BaseGen;
using static Mono.Math.BigInteger;
using Verse.Noise;

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
		private struct IField<T> where T : struct
		{
			public T value;
			public T valuePrev;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void ReSet(T newVal, bool expired)
			{				
				this.valuePrev = expired ? default(T) : this.value;
				this.value = newVal;
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

		public readonly int NumGridCells;

		private readonly CellIndices		indices;
		private readonly IFieldInfo[]		cells;
		private readonly IField<float>[]	cells_strength;
		private readonly IField<Vector2>[]	cells_dir;
		private readonly IField<UInt64>[]	cells_flags;
		private readonly IField<float>[] cells_blunt;
		private readonly IField<float>[] cells_sharp;
		private readonly IField<MetaCombatAttribute>[] cells_meta;


		private short r_cycle	= 19;
		private short r_sig		= 19;
		private float curBlunt;
		private float curSharp;
		private MetaCombatAttribute curMeta;

		public short CycleNum
		{
			get
			{
				return r_cycle;
			}
		}

		public ITSignalGrid(Map map)
		{
			this.indices = map.cellIndices;
			this.NumGridCells = indices.NumGridCells;

			cells =				new IFieldInfo[NumGridCells];
			cells_strength =	new IField<float>[NumGridCells];
			cells_dir =			new IField<Vector2>[NumGridCells];
			cells_flags =		new IField<UInt64>[NumGridCells];
			cells_blunt	=		new IField<float>[NumGridCells];
			cells_sharp =		new IField<float>[NumGridCells];
			cells_meta =		new IField<MetaCombatAttribute>[NumGridCells];
		}

		public void Set(IntVec3 cell, float signalStrength, Vector2 dir) => Set(indices.CellToIndex(cell), signalStrength, dir);
		public void Set(int index, float signalStrength, Vector2 dir)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo info = cells[index];
				if (info.sig != r_sig)
				{
					int dc = r_cycle - info.cycle;
					if (dc == 0)
					{
						info.num += 1;
						cells_strength[index].value += signalStrength;
						cells_dir[index].value += dir;						
						cells_meta[index].value |= curMeta;
						cells_sharp[index].value += curSharp;
						cells_blunt[index].value += curBlunt;						
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
						cells_flags[index].ReSet(0, expired);
						cells_meta[index].ReSet(curMeta, expired);
						cells_sharp[index].ReSet(curSharp, expired);
						cells_blunt[index].ReSet(curBlunt, expired);
						info.cycle = r_cycle;						
					}
					info.sig = r_sig;
					cells[index] = info;
				}
			}
		}

		public void Set(IntVec3 cell, float signalStrength, Vector2 dir, MetaCombatAttribute metaAttributes) => Set(indices.CellToIndex(cell), signalStrength, dir, metaAttributes);
		public void Set(int index, float signalStrength, Vector2 dir, MetaCombatAttribute metaAttributes)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo info = cells[index];
				if (info.sig != r_sig)
				{
					int dc = r_cycle - info.cycle;
					if (dc == 0)
					{
						info.num += 1;
						cells_strength[index].value += signalStrength;
						cells_dir[index].value += dir;
						cells_meta[index].value |= metaAttributes;
						cells_sharp[index].value += curSharp;
						cells_blunt[index].value += curBlunt;
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
						cells_flags[index].ReSet(0, expired);
						cells_meta[index].ReSet(metaAttributes, expired);
						cells_sharp[index].ReSet(curSharp, expired);
						cells_blunt[index].ReSet(curBlunt, expired);
						info.cycle = r_cycle;
					}
					info.sig = r_sig;
					cells[index] = info;
				}
			}
		}

		public void Set(IntVec3 cell, UInt64 flags) => Set(indices.CellToIndex(cell), flags);
		public void Set(int index, UInt64 flags)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo info = cells[index];
				if (info.sig != r_sig)
				{
					int dc = r_cycle - info.cycle;
					if (dc == 0)
					{
						cells_flags[index].value |= flags;
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
						cells_flags[index].ReSet(flags, expired);
						cells_meta[index].ReSet(0, expired);
						cells_sharp[index].ReSet(0, expired);
						cells_blunt[index].ReSet(0, expired);
						info.cycle = r_cycle;
					}
					info.sig = r_sig;
					cells[index] = info;
				}
			}
		}

		public void Set(IntVec3 cell, float signalStrength, Vector2 dir, UInt64 flags) => Set(indices.CellToIndex(cell), signalStrength, dir, flags);
		public void Set(int index, float signalStrength, Vector2 dir, UInt64 flags)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo info = cells[index];
				if (info.sig != r_sig)
				{
					int dc = r_cycle - info.cycle;
					if (dc == 0)
					{
						info.num += 1;
						cells_strength[index].value += signalStrength;
						cells_dir[index].value += dir;
						cells_flags[index].value |= flags;
						cells_meta[index].value |= curMeta;
						cells_sharp[index].value += curSharp;
						cells_blunt[index].value += curBlunt;
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
						cells_flags[index].ReSet(flags, expired);
						cells_meta[index].ReSet(curMeta, expired);
						cells_sharp[index].ReSet(curSharp, expired);
						cells_blunt[index].ReSet(curBlunt, expired);
						info.cycle = r_cycle;
					}
					info.sig = r_sig;
					cells[index] = info;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetSignalNum(IntVec3 cell) => GetSignalNum(indices.CellToIndex(cell));
		public int GetSignalNum(int index)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo cell = cells[index];				
				switch (r_cycle - cell.cycle)
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
		public float GetRawSignalStrengthAt(IntVec3 cell) => GetRawSignalStrengthAt(indices.CellToIndex(cell));
		public float GetRawSignalStrengthAt(int index)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo cell = cells[index];
				switch (r_cycle - cell.cycle)
				{
					case 0:
						IField<float> strength = cells_strength[index];

						return Maths.Max(strength.value, strength.valuePrev);
					case 1:
						return cells_strength[index].value;
					default:
						break;
				}
			}
			return 0;
		}		

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetSignalStrengthAt(IntVec3 cell) => GetSignalStrengthAt(indices.CellToIndex(cell));
		public float GetSignalStrengthAt(int index)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo cell = cells[index];				
				switch (r_cycle - cell.cycle)
				{
					case 0:						
						IField<float> strength = cells_strength[index];

						return Maths.Max(strength.value, strength.valuePrev) * 0.9f + Maths.Max(cell.num, cell.numPrev) * 0.1f;
					case 1:
						return cells_strength[index].value * 0.9f + cell.num * 0.1f;
					default:
						break;
				}
			}
			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetSignalStrengthAt(IntVec3 cell, out int signalNum) => GetSignalStrengthAt(indices.CellToIndex(cell), out signalNum);
		public float GetSignalStrengthAt(int index, out int signalNum)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo cell = cells[index];
				switch (r_cycle - cell.cycle)
				{
					case 0:
						IField<float> strength = cells_strength[index];
						signalNum = Maths.Max(cell.num, cell.numPrev); 
						return Maths.Max(strength.value, strength.valuePrev) * 0.9f + signalNum * 0.1f;
					case 1:
						signalNum = cell.num;
						return cells_strength[index].value * 0.9f + signalNum * 0.1f;
					default:
						break;
				}
			}
			return signalNum = 0;
		}

		public UInt64 GetFlagsAt(IntVec3 cell) => GetFlagsAt(indices.CellToIndex(cell));
		public UInt64 GetFlagsAt(int index)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo cell = cells[index];
				switch (r_cycle - cell.cycle)
				{
					case 0:
						IField<UInt64> flags = cells_flags[index];

						return flags.value | flags.valuePrev;
					case 1:
						return cells_flags[index].value;
					default:
						break;
				}
			}
			return 0;
		}

		public MetaCombatAttribute GetCombatAttributesAt(IntVec3 cell) => GetCombatAttributesAt(indices.CellToIndex(cell));
		public MetaCombatAttribute GetCombatAttributesAt(int index)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo cell = cells[index];
				switch (r_cycle - cell.cycle)
				{
					case 0:
						IField<MetaCombatAttribute> flags = cells_meta[index];

						return flags.value | flags.valuePrev;
					case 1:
						return cells_meta[index].value;
					default:
						break;
				}
			}
			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetSharp(IntVec3 cell) => GetSharp(indices.CellToIndex(cell));
		public float GetSharp(int index)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo cell = cells[index];
				switch (r_cycle - cell.cycle)
				{
					case 0:
						IField<float> sharp = cells_sharp[index];

						return Maths.Max(sharp.value, sharp.valuePrev);
					case 1:
						return cells_sharp[index].value;
					default:
						break;
				}
			}
			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetBlunt(IntVec3 cell) => GetBlunt(indices.CellToIndex(cell));
		public float GetBlunt(int index)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo cell = cells[index];
				switch (r_cycle - cell.cycle)
				{
					case 0:
						IField<float> blunt = cells_blunt[index];

						return Maths.Max(blunt.value, blunt.valuePrev);
					case 1:
						return cells_blunt[index].value;
					default:
						break;
				}
			}
			return 0;
		}

		public Vector2 GetSignalDirectionAt(IntVec3 cell) => GetSignalDirectionAt(indices.CellToIndex(cell));
		public Vector2 GetSignalDirectionAt(int index)
		{
			if (index >= 0 && index < NumGridCells)
			{
				IFieldInfo cell = cells[index];
				switch (r_cycle - cell.cycle)
				{
					case 0:
						IField<Vector2> dir = cells_dir[index];
						
						return cell.num >= cell.numPrev ? dir.value / (cell.num + 0.01f) : dir.valuePrev / (cell.numPrev + 0.01f);
					case 1:
						return cells_dir[index].value / (cell.num + 0.01f);
					default:
						break;
				}

			}
			return Vector2.zero;
		}


		/// <summary>
		/// Prepare the grid for a new casting operation.
		/// </summary>
		/// <param name="sharp">Sharp damage output/s</param>
		/// <param name="blunt">Blunt damage output/s</param>
		/// <param name="meta">Meta flags.</param>
		public void Next(float sharp, float blunt, MetaCombatAttribute meta)
		{
			if (r_sig++ == short.MaxValue)
				r_sig = 19;
			this.curSharp = sharp;
			this.curBlunt = blunt;
			this.curMeta = meta;
		}

		/// <summary>
		/// TODO
		/// </summary>
		public void NextCycle()
		{
			if (r_sig++ == short.MaxValue)
			{
				r_sig = 19;
			}
			if (r_cycle++ == short.MaxValue)
			{
				r_cycle = 13;
			}
		}
	}
}

