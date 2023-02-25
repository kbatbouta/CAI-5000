using UnityEngine;
using Verse;
namespace CombatAI
{
    public static class MortarUtility
    {
        private static Vector2 zero = new Vector2(0, 0);

        public static void TryCastSight(Map map, IntVec3 root, ITSignalGrid grid, int minRange, int maxRange, bool canPenetrateThick, MetaCombatAttribute attributes)
        {
            RoofGrid roofs   = map.GetComponent<RoofGrid>();
            int      minRSqr = minRange * minRange;
            int      maxRSqr = maxRange * maxRange;
            attributes |= MetaCombatAttribute.Mortar;
            for (int i = -maxRange; i <= maxRange; i++)
            {
                for (int j = -maxRange; j <= maxRange; j++)
                {
                    IntVec3 cell = root + new IntVec3(i, 0, j);
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }
                    int dist = cell.DistanceToSquared(root);
                    if (dist < minRSqr || dist > maxRSqr)
                    {
                        continue;
                    }
                    RoofType roof = roofs.GetRoofType(cell);
                    if (canPenetrateThick || (roof & RoofType.RockThick) != RoofType.None)
                    {
                        grid.Set(cell, attributes);
                    }
                }
            }
        }
    }
}
