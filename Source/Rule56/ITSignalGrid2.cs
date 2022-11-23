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

		private short r_cycle	= 19;
		private short r_sig		= 19;

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
		/// <param name="center">Center of casting.</param>
		/// <param name="range">Expected range of casting.</param>
		/// <param name="casterFlags">caster's Flags</param>
		public void Next()
		{
			if (r_sig++ == short.MaxValue)
				r_sig = 19;
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

