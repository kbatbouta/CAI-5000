﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatAI
{
	public class ITFloatGrid
	{
		private struct ITCell
		{
			public float value;
			public float valuePrev;
			public ushort sig;
			public ushort cycleNum;
		}

		private ushort sig = 13;
		private ushort cycleNum = 19;
		private readonly CellIndices cellIndices;
		private readonly ITCell[] grid;

		public readonly int mapCellNum;

		public int CycleNum => cycleNum;

		public ITFloatGrid(Map map)
		{
			cellIndices = map.cellIndices;
			grid = new ITCell[cellIndices.NumGridCells];
			mapCellNum = cellIndices.NumGridCells;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(IntVec3 cell, float value)
		{
			Set(cellIndices.CellToIndex(cell), value);
		}

		public void Set(int index, float value)
		{
			if (index >= 0 && index < mapCellNum)
			{
				var cell = grid[index];
				if (cell.sig != sig)
				{
					var dc = cycleNum - cell.cycleNum;
					if (dc == 0)
					{
						cell.value += value;
					}
					else
					{
						if (dc == 1)
							cell.valuePrev = cell.value;
						else
							cell.valuePrev = 0;
						cell.cycleNum = cycleNum;
						cell.value = value;
					}

					cell.sig = sig;
					grid[index] = cell;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Get(IntVec3 cell)
		{
			return Get(cellIndices.CellToIndex(cell));
		}

		public float Get(int index)
		{
			if (index >= 0 && index < mapCellNum)
			{
				var cell = grid[index];
				var dc = cycleNum - cell.cycleNum;
				switch (dc)
				{
					case 0:
						return Maths.Max(cell.value, cell.valuePrev);
					case 1:
						return cell.value;
					default:
						return 0;
				}
			}

			return 0;
		}

		public void Next()
		{
			if (sig++ == short.MaxValue) sig = 13;
		}

		public void NextCycle()
		{
			Next();
			if (cycleNum++ == short.MaxValue) sig = 13;
		}
	}
}