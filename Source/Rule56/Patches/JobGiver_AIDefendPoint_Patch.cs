namespace CombatAI.Patches
{
    public static class JobGiver_AIDefendPoint_Patch
    {
//        [HarmonyPatch(typeof(JobGiver_AIDefendPoint), nameof(JobGiver_AIDefendPoint.TryFindShootingPosition))]
//        private static class JobGiver_AIDefendPoint_TryFindShootingPosition_Patch
//        {
//            public static bool Prefix(Pawn pawn, out IntVec3 dest, ref bool __result, Verb verbToUse)
//            {
//                dest = IntVec3.Invalid;
//                if (verbToUse != null && pawn.CanReach(pawn.mindState.duty.focus.Cell, PathEndMode.OnCell, Danger.Unspecified) && pawn.TryGetSightReader(out SightTracker.SightReader reader) && reader.GetAbsVisibilityToEnemies(pawn.mindState.duty.focus.Cell) > 0)
//                {
//                    Map      map  = pawn.Map;
//                    WallGrid grid = map.GetComp_Fast<WallGrid>();
//                    PawnPath path = map.pathFinder.FindPath(pawn.Position, pawn.mindState.duty.focus.Cell, pawn, PathEndMode.OnCell, null);
//                    if (path is { Found: true } && path.nodes.Count > 0)
//                    {
//                        try
//                        {
//                            int     index = 0;
//                            int     limit = Maths.Min((int)verbToUse.EffectiveRange + 1, path.nodes.Count);
//                            IntVec3 cell;
//                            while (index < limit && reader.GetAbsVisibilityToEnemies(cell = path.nodes[index]) > 0 && grid.GetFillCategory(cell) != FillCategory.Full)
//                            {
//                                index++;
//                                map.debugDrawer.FlashCell(cell, 1, "x");
//                            }
//                            if (index >= path.nodes.Count || index == 0)
//                            {
//                                path.ReleaseToPool();
//                                return true;
//                            }
//                            dest = path.nodes[index - 1];
//                            path.ReleaseToPool();
//                            __result = true;
//                            return false;
//                        }
//                        catch (Exception er)
//                        {
//                            path.ReleaseToPool();
//                            throw er;
//                        }
//                    }
//                }
//                return true;
//            }
//        }
    }
}
