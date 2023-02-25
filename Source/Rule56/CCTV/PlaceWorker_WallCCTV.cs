using System.Collections.Generic;
using CombatAI.R;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public class PlaceWorker_WallCCTV : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;
            if (rot == Rot4.North || rot == Rot4.South)
            {
                GenDraw.DrawFieldEdges(new List<IntVec3>
                {
                    center
                }, !center.Impassable(map) ? Color.white : Color.red);
                IntVec3 wall = center + IntVec3.North.RotatedBy(rot);
                GenDraw.DrawFieldEdges(new List<IntVec3>
                {
                    wall
                }, wall.Impassable(map) ? Color.blue : Color.red);
            }
            else
            {
                GenDraw.DrawFieldEdges(new List<IntVec3>
                {
                    center
                }, !center.Impassable(map) ? Color.white : Color.red);
                IntVec3 wall = center + IntVec3.South.RotatedBy(rot);
                GenDraw.DrawFieldEdges(new List<IntVec3>
                {
                    wall
                }, wall.Impassable(map) ? Color.blue : Color.red);
            }
        }

        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (rot == Rot4.North || rot == Rot4.South)
            {
                if (center.Impassable(map) || !(center + IntVec3.North.RotatedBy(rot)).Impassable(map))
                {
                    return Keyed.CombatAI_PlaceWorker_WallMounted;
                }
            }
            else
            {
                if (center.Impassable(map) || !(center + IntVec3.South.RotatedBy(rot)).Impassable(map))
                {
                    return Keyed.CombatAI_PlaceWorker_WallMounted;
                }
            }
            return true;
        }
    }
}
