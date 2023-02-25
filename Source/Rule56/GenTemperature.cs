using System.Runtime.CompilerServices;
using Verse;
namespace CombatAI
{
    public static class GenTemperature
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TryGetTemperature(IntVec3 cell, Map map)
        {
            return TryGetTemperature(map.cellIndices.CellToIndex(cell), map);
        }

        public static float TryGetTemperature(int index, Map map)
        {
            if (index >= 0 && index < map.cellIndices.NumGridCells)
            {
                Region region = map.regionGrid.regionGrid[index];
                Room   room;
                if (region is { valid: true } && (room = region.District?.Room) != null)
                {
                    return room.Temperature;
                }
            }
            return 21f;
        }
    }
}
