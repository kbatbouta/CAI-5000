using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using Verse.AI;
using UnityEngine.UIElements;
using System.Threading;

namespace CombatAI.Comps
{
    public class ThingComp_CombatAI : ThingComp
    {
        private int lastInterupted;
        private bool scanning;                
        private HashSet<Thing> visibleEnemies;

        public SightTracker.SightReader sightReader;      

        public ThingComp_CombatAI()
        {
            this.visibleEnemies = new HashSet<Thing>();
        }      

        public void OnScanFinished()
        {
            if (scanning == false)
            {
                Log.Warning($"ISMA: OnScanFinished called while not scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
            }
            scanning = false;
            if (parent == null || parent.Destroyed || !parent.Spawned || visibleEnemies.Count == 0 || GenTicks.TicksGame - lastInterupted < 360 )
            {
                return;
            }
            Verb verb = parent.TryGetAttackVerb();
            if (verb == null || verb.IsMeleeAttack)
            {
                //parent.Map.debugDrawer.FlashCell(parent.Position, 0.1f, $"L{visibleEnemies.Count}");
                return;
            }
            //parent.Map.debugDrawer.FlashCell(parent.Position, 0.5f, $"W{visibleEnemies.Count}");
            if (parent is Pawn pawn)
            {
                if (!(pawn.pather?.moving ?? false))
                {
                    return;
                }
                Thing bestEnemy = null;
                float bestEnemyScore = 1e8f;
                bool bestEnemyVisibleNow = false;
                foreach (Thing enemy in visibleEnemies)
                {
                    if (enemy != null && enemy.Spawned && !enemy.Destroyed)
                    {
                        if (verb.CanHitTarget(bestEnemy))
                        {
                            if (!bestEnemyVisibleNow)
                            {
                                bestEnemy = enemy;
                                bestEnemyScore = pawn.Position.DistanceToSquared(enemy.Position);
                                bestEnemyVisibleNow = true;
                            }
                            else
                            {
                                float distSqr = pawn.Position.DistanceToSquared(enemy.Position);
                                if (bestEnemyScore > distSqr)
                                {
                                    bestEnemy = enemy;
                                    bestEnemyScore = distSqr;
                                }
                            }
                        }
                        else if (!bestEnemyVisibleNow && enemy is Pawn enemyPawn)
                        {
                            IntVec3 shiftedPos = GetMovingShiftedPosition(enemyPawn, 60);
                            if (shiftedPos != enemyPawn.Position && verb.CanHitTargetFrom(pawn.Position, shiftedPos))
                            {
                                float distSqr = pawn.Position.DistanceToSquared(shiftedPos);
                                if (bestEnemyScore > distSqr)
                                {
                                    bestEnemy = enemy;
                                    bestEnemyScore = distSqr;
                                }
                            }
                        }
                    }
                }
                if (bestEnemy == null)
                {
                    return;
                }
                //parent.Map.debugDrawer.FlashCell(pawn.Position, 0.9f, "s", duration: 100);
                if (bestEnemyVisibleNow)
                {
                    pawn.mindState.enemyTarget = bestEnemy;
                    CastPositionRequest request = new CastPositionRequest();
                    request.caster = pawn;
                    request.target = bestEnemy;
                    request.verb = verb;
                    request.maxRangeFromLocus = 5;
                    request.wantCoverFromTarget = true;
                    if (CastPositionFinder.TryFindCastPosition(request, out IntVec3 cell))
                    {
                        Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
                        Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);
                        pawn.jobs.StopAll();
                        pawn.jobs.StartJob(job_goto, JobCondition.InterruptForced);
                        pawn.jobs.jobQueue.EnqueueFirst(job_waitCombat);
                    }
                }
                else
                {
                    Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);
                    pawn.jobs.StopAll();
                    pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
                }
                lastInterupted = GenTicks.TicksGame;
            }            
        }

        public void OnScanStarted()
        {
            if(scanning == true)
            {
                Log.Warning($"ISMA: OnScanStarted called while scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
            }
            scanning = true;
            visibleEnemies.Clear();
        }

        public void Notify_EnemiesVisible(IEnumerable<Thing> things)
        {
            if (!scanning)
            {                
                Log.Warning($"ISMA: Notify_EnemiesVisible called while not scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
            }
            visibleEnemies.AddRange(things);            
        }

        public void Notify_SightReaderChanged(SightTracker.SightReader reader)
        {
            this.sightReader = reader;
        }

        private static IntVec3 GetMovingShiftedPosition(Pawn pawn, float ticksAhead)
        {
            PawnPath path;
            if (!(pawn.pather?.moving ?? false) || (path = pawn.pather.curPath) == null || path.NodesLeftCount <= 1)
            {
                return pawn.Position;
            }

            float distanceTraveled = Mathf.Min(pawn.GetStatValue(StatDefOf.MoveSpeed) * ticksAhead / 60f, path.NodesLeftCount - 1);
            return path.Peek(Mathf.FloorToInt(distanceTraveled));
        }
    }
}

