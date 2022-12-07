using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace CombatAI
{
	public class WallGrid : MapComponent
	{
		private readonly CellIndices cellIndices;
		private readonly float[]     grid;

		public WallGrid(Map map) : base(map)
		{
			cellIndices = map.cellIndices;
			grid        = new float[cellIndices.NumGridCells];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FillCategory GetFillCategory(IntVec3 cell)
		{
			return GetFillCategory(cellIndices.CellToIndex(cell));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FillCategory GetFillCategory(int index)
		{
			float f = grid[index];
			if (f == 0)
			{
				return FillCategory.None;
			}
			else if (f < 1f)
			{
				return FillCategory.Partial;
			}
			else
			{
				return FillCategory.Full;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanBeSeenOver(IntVec3 cell)
		{
			return CanBeSeenOver(cellIndices.CellToIndex(cell));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CanBeSeenOver(int index)
		{
			return grid[index] < 0.998f;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RecalculateCell(IntVec3 cell, Thing t)
		{
			RecalculateCell(cellIndices.CellToIndex(cell), t);
		}
		public void RecalculateCell(int index, Thing t)
		{
			if (t != null)
			{
				if (t.def.plant != null)
				{
					if (t.def.plant.IsTree)
					{
						if (t is Plant plant)
						{
							grid[index] = plant.Growth * t.def.fillPercent / 4f;
						}
						else
						{
							grid[index] = t.def.fillPercent / 4f;
						}
					}
				}
				else if (t is Building_Door door)
				{
					grid[index] = 1 - door.OpenPct;
				}
				else if (t is Building ed && ed.def.Fillage == FillCategory.Full)
				{
					grid[index] = 1.0f;
				}
				else
				{
					grid[index] = t.def.fillPercent;
				}
			}
			else
			{
				grid[index] = 0;
			}
		}

		public float this[IntVec3 cell]
		{
			get => this[cellIndices.CellToIndex(cell)];
			set => this[cellIndices.CellToIndex(cell)] = value;
		}

		public float this[int index]
		{
			get => grid[index];
			set => grid[index] = value;
		}
	}
}
