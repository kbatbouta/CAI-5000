using System;
using UnityEngine;
namespace CombatAI
{
    /*
     * Taken from my original commit to CE where I introduced this debugging tool. 
     * https://github.com/CombatExtended-Continued/CombatExtended/tree/31446370a92855de32c709bcd8ff09039db85452 
     */

    /// <summary>
    ///     Important: the return type should be string.
    ///     Used to allow for access to debug information anywhere via a tooltip.
    ///     This will be called when the left shift button is down or incase alternative key is not none, it will one will be
    ///     required.
    ///     A call to this function should return a string to be added to the tooltip.
    ///     Usage:
    ///     Requires that the function has (Map map, IntVec3 pos) as input parameters for usage within maps.
    ///     Requires that the function has (World map, int tile) as input parameters for usage within the world map.
    ///     Requires that the flaged function has a return type of string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CombatAI_DebugTooltip : Attribute
    {
        public readonly KeyCode                   altKey;
        public readonly CombatAI_DebugTooltipType tooltipType;

        /// <summary>
        ///     Important: the return type should be string.
        ///     Used to allow for access to debug information anywhere via a tooltip.
        ///     This will be called when the left shift button is down or incase alternative key is not none, it will one will be
        ///     required.
        ///     A call to this function should return a string to be added to the tooltip.
        ///     Usage:
        ///     Requires that the function has (Map map, IntVec3 pos) as input parameters for usage within maps.
        ///     Requires that the function has (World map, int tile) as input parameters for usage within the world map.
        ///     Requires that the flaged function has a return type of string.
        /// </summary>
        /// <param name="tooltipType">Where to call this function</param>
        /// <param name="altKey">alternative key instead of left shift</param>
        public CombatAI_DebugTooltip(CombatAI_DebugTooltipType tooltipType, KeyCode altKey = KeyCode.None)
        {
            this.tooltipType = tooltipType;
            this.altKey      = altKey;
        }
    }
}
