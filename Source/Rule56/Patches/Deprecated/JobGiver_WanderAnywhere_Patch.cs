namespace CombatAI.Patches
{
    public class JobGiver_WanderAnywhere_Patch
    {
        //[HarmonyPatch(typeof(JobGiver_WanderAnywhere), nameof(JobGiver_WanderAnywhere.GetWanderRoot))]
        //static class JobGiver_WanderAnywhere_GetWanderRoot_Patch
        //{
        //    public static bool Prefix(Pawn pawn, ref IntVec3 __result)
        //    {                
        //        if (Finder.Performance.Tps > 55 && !Finder.Performance.TpsCriticallyLow && pawn.Faction == null && pawn.GetSightReader(out SightTracker.SightReader reader))                    
        //        {
        //            IntVec3 minCell = pawn.Position;
        //            IntVec3 root = pawn.Position;
        //            float rootVisibility = reader.GetVisibilityToEnemies(pawn.Position);
        //            float minCost = rootVisibility + 1;                    
        //            pawn.Map.GetCellFlooder().Flood(pawn.Position, (cell, parent, dist) =>
        //            {
        //                //
        //                // pawn.Map.debugDrawer.FlashCell(cell, Mathf.Clamp(dist + 75f, 0, 150f) / 150f, $"{dist}");
        //                if((dist < minCost || Mathf.Abs(dist - minCost) < 1e-1 && Rand.Chance(0.5f)) && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Unspecified))
        //                {
        //                    minCost = dist;
        //                    minCell = cell;
        //                }
        //            }, (cell) =>
        //            {                       
        //                return (reader.GetVisibilityToNeutrals(cell) - rootVisibility) * 1.3f;
        //            }, maxDist: 15);
        //            if (minCell != root)
        //            {
        //                __result = minCell;
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //}
    }
}
