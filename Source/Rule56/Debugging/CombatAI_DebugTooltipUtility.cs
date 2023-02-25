using RimWorld.Planet;
using Verse;
namespace CombatAI
{
    /*
     * Taken from my original commit to CE where I introduced this debugging tool.	 
     * https://github.com/CombatExtended-Continued/CombatExtended/tree/31446370a92855de32c709bcd8ff09039db85452		 
     */

    public class CombatAI_DebugTooltipUtility
    {
        [CombatAI_DebugTooltip(CombatAI_DebugTooltipType.Map)]
        public static string CellPositionTip(Map map, IntVec3 cell)
        {
            return $"Cell: ({cell.x}, {cell.z})";
        }

        [CombatAI_DebugTooltip(CombatAI_DebugTooltipType.World)]
        public static string TileIndexTip(World world, int tile)
        {
            return $"Tile index:\t\t{tile}";
        }

        [CombatAI_DebugTooltip(CombatAI_DebugTooltipType.Map)]
        public static string CellTemp(Map map, IntVec3 cell)
        {
            return $"Temp: {GenTemperature.TryGetTemperature(cell, map)}";
        }
    }
}
