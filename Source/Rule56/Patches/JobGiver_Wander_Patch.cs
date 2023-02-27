namespace CombatAI.Patches
{
    public static class JobGiver_Wander_Patch
    {
//        [HarmonyPatch(typeof(JobGiver_Wander), nameof(JobGiver_Wander.TryGiveJob))]
//        private static class JobGiver_Wander_TryGiveJob_Patch
//        {
//            public static bool Prefix(JobGiver_Wander __instance, Pawn pawn)
//            {
//                if (pawn.Faction.IsPlayerSafe())
//                {
//                    return true;
//                }
//                // skip if the pawn is firing or warming up
//                if (pawn.stances?.curStance is Stance_Warmup)
//                {
//                    return false;
//                }
//                // don't skip unless it's JobGiver_WanderNearDutyLocation
//                if (!(__instance is JobGiver_WanderNearDutyLocation) && !(__instance is JobGiver_WanderAnywhere))
//                {
//                    return true;
//                }
//                ThingComp_CombatAI comp = pawn.AI();
//                if (comp != null)
//                {
//                    if(comp.data.InterruptedRecently(600) || comp.data.RetreatedRecently(600))
//                    {
//                        return false;
//                    }
//                    if (comp.sightReader != null && comp.sightReader.GetVisibilityToEnemies(pawn.Position) > 0 && pawn.mindState.enemyTarget == null)
//                    {
//                        if (comp.data.NumAllies != 0)
//                        {
//                            IEnumerator<AIEnvAgentInfo> allies = comp.data.AlliesNearBy();
//                            while (allies.MoveNext())
//                            {
//                                AIEnvAgentInfo ally = allies.Current;
//                                if (ally.thing is Pawn { Destroyed: false, Spawned: true } other && other.mindState.enemyTarget != null)
//                                {
//                                    pawn.mindState.enemyTarget = other.mindState.enemyTarget;
//                                    return false;
//                                }
//                            }
//                        }
//                        if (comp.data.NumEnemies != 0)
//                        {
//                            float                       minDist  = float.MaxValue;
//                            Thing                       minEnemy = null;
//                            IEnumerator<AIEnvAgentInfo> enemies  = comp.data.Enemies();                            
//                            while (enemies.MoveNext())
//                            {
//                                AIEnvAgentInfo enemy = enemies.Current;                                
//                                if (enemy.thing is { Destroyed: false, Spawned: true })
//                                {
//                                    float dist = enemy.thing.DistanceTo_Fast(pawn);
//                                    if (dist < minDist)
//                                    {
//                                        minEnemy = enemy.thing;
//                                        minDist  = dist;
//                                    }
//                                }
//                            }
//                            if (minEnemy != null)
//                            {
//                                pawn.mindState.enemyTarget = minEnemy;
//                                return false;
//                            }
//                        }
//                    }
//                }
//                return true;
//            }
//        }
    }
}
