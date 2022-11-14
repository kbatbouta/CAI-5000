using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using Verse.AI;
using System.Threading;
using System.Net.NetworkInformation;
using UnityEngine.Analytics;

namespace CombatAI.Comps
{
    public class ThingComp_CombatAI : ThingComp
    {
        private HashSet<Pawn> _visibleEnemies = new HashSet<Pawn>();
        private List<IntVec3> _path = new List<IntVec3>();
        private List<Color> _colors = new List<Color>();

        private int lastInterupted;
        private int lastSawEnemies;

        private bool scanning;

        private HashSet<Thing> visibleEnemies;

        public Pawn_CustomDutyTracker duties;
        public SightTracker.SightReader sightReader;

        public ThingComp_CombatAI()
        {
            this.visibleEnemies = new HashSet<Thing>();            
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent is Pawn pawn)
            {
                this.duties = new Pawn_CustomDutyTracker(pawn);
            }
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight && parent is Pawn pawn)
            {
                var verb = pawn.CurrentEffectiveVerb;
                var sightRange = Math.Min(SightUtility.GetSightRange(pawn), verb.EffectiveRange);
                var sightRangeSqr = sightRange * sightRange;
                if (sightRange != 0 && verb != null)
                {
                    Vector3 drawPos = pawn.DrawPos;
                    IntVec3 shiftedPos = GetMovingShiftedPosition(pawn, 30);
                    List<Pawn> nearbyVisiblePawns = GenClosest.ThingsInRange(pawn.Position, pawn.Map, Utilities.TrackedThingsRequestCategory.Pawns, sightRange)
                        .Select(t => t as Pawn)
                        .Where(p => !p.Dead && !p.Downed && GetMovingShiftedPosition(p, 60).DistanceToSquared(shiftedPos) < sightRangeSqr && verb.CanHitTargetFrom(shiftedPos, GetMovingShiftedPosition(p, 60)) && p.HostileTo(pawn))
                        .ToList();
                    CombatAI.Gui.GUIUtility.ExecuteSafeGUIAction(() =>
                    {
                        Vector2 drawPosUI = UI.MapToUIPosition(drawPos);
                        Text.Font = GameFont.Tiny;
                        string state = GenTicks.TicksGame - lastInterupted > 120 ? "<color=blue>O</color>" : "<color=yellow>X</color>";                        
                        Widgets.Label(new Rect(drawPosUI.x - 25, drawPosUI.y - 15, 50, 30), $"{state}/{_visibleEnemies.Count}");
                    });                    
                    bool bugged = nearbyVisiblePawns.Count != _visibleEnemies.Count;
                    if (bugged)
                    {
                        Rect rect; 
                        Vector2 a = UI.MapToUIPosition(drawPos);
                        Vector2 b;
                        Vector2 mid;                                             
                        foreach (var other in nearbyVisiblePawns.Where(p => !_visibleEnemies.Contains(p)))
                        {
                            b = UI.MapToUIPosition(other.DrawPos);
                            Widgets.DrawLine(a, b, Color.red, 1);

                            mid = (a + b) / 2;
                            rect = new Rect(mid.x - 25, mid.y - 15, 50, 30);
                            Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
                            Widgets.DrawBox(rect);
                            Widgets.Label(rect, $"<color=red>Errored</color>.  {Math.Round(other.Position.DistanceTo(pawn.Position), 1)}");
                        }
                    }
                    bool selected = Find.Selector.SelectedPawns.Contains(pawn);
                    if (bugged || selected)
                    {
                        GenDraw.DrawRadiusRing(pawn.Position, sightRange);
                    }
                    if (selected)
                    {
                        for (int i = 1; i < _path.Count; i++)
                        {
                            Widgets.DrawBoxSolid(new Rect(UI.MapToUIPosition(_path[i - 1].ToVector3().Yto0() + new Vector3(0.5f, 0, 0.5f)) - new Vector2(5, 5), new Vector2(10, 10)), _colors[i]);
                            Widgets.DrawLine(UI.MapToUIPosition(_path[i - 1].ToVector3().Yto0() + new Vector3(0.5f, 0, 0.5f)), UI.MapToUIPosition(_path[i].ToVector3().Yto0() + new Vector3(0.5f, 0, 0.5f)), Color.white, 1);
                        }
                        if(_path.Count > 0)
                        {
                            Vector2 v = UI.MapToUIPosition(pawn.DrawPos.Yto0());
                            Widgets.DrawLine(UI.MapToUIPosition(_path.Last().ToVector3().Yto0() + new Vector3(0.5f, 0, 0.5f)), v, _colors.Last(), 1);
                            Widgets.DrawBoxSolid(new Rect(v - new Vector2(5, 5), new Vector2(10, 10)), _colors.Last());
                        }
                        if (!_visibleEnemies.EnumerableNullOrEmpty())
                        {
                            Vector2 a = UI.MapToUIPosition(pawn.DrawPos);
                            Vector2 b;
                            Vector2 mid;
                            Rect rect;
                            int index = 0;
                            foreach (var other in _visibleEnemies)
                            {
                                b = UI.MapToUIPosition(other.DrawPos);
                                Widgets.DrawLine(a, b, Color.blue, 1);

                                mid = (a + b) / 2;
                                rect = new Rect(mid.x - 25, mid.y - 15, 50, 30);
                                Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
                                Widgets.DrawBox(rect);
                                Widgets.Label(rect, $"<color=gray>({index++}).</color> {Math.Round(other.Position.DistanceTo(pawn.Position), 1)}");
                            }
                        }
                    }
                }
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!parent.Spawned)
            {
                return;
            }
            if (duties != null)
            {
                duties.TickRare();
            }
        }      

        public void OnScanFinished()
        {            
            if (scanning == false)
            {
                Log.Warning($"ISMA: OnScanFinished called while not scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
                return;
            }
            scanning = false;
            if (Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight)
            {
                _visibleEnemies.Clear();
                _visibleEnemies.AddRange(visibleEnemies.Where(t => t is Pawn).Select(t => t as Pawn));
                if (_path.Count == 0 || _path.Last() != parent.Position)
                {
                    _path.Add(parent.Position);
                    if (GenTicks.TicksGame - lastInterupted < 120)
                    {
                        _colors.Add(Color.red);
                    }
                    else if (GenTicks.TicksGame - lastInterupted < 240)
                    {
                        _colors.Add(Color.yellow);
                    }
                    else
                    {
                        _colors.Add(Color.black);
                    }
                    if (_path.Count >= 30)
                    {
                        _path.RemoveAt(0);
                        _colors.RemoveAt(0);
                    }
                }
            }
            if (visibleEnemies.Count > 0)
            {
                if (GenTicks.TicksGame - lastInterupted < 120 && GenTicks.TicksGame - lastSawEnemies > 60)
                {
                    lastInterupted = -1;
                    if (Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight)
                    {
                        parent.Map.debugDrawer.FlashCell(parent.Position, 1.0f, "X", duration: 60);
                    }
                }
                lastSawEnemies = GenTicks.TicksGame;
            } 
            if (parent == null || parent.Destroyed || !parent.Spawned || GenTicks.TicksGame - lastInterupted < 120 || visibleEnemies.Count == 0 || (parent as Pawn)?.stances?.curStance is Stance_Warmup)
            {
                return;
            }            
            Verb verb = parent.TryGetAttackVerb();            
            if (verb == null || verb.IsMeleeAttack)
            {
                return;
            }                        
            if (parent is Pawn pawn)
            {                
                Thing bestEnemy = null;
                IntVec3 bestEnemyPositon = IntVec3.Invalid;
                float bestEnemyScore = 1e8f;
                bool bestEnemyVisibleNow = false;
                bool bestEnemyVisibleSoon = false;
                foreach (Thing enemy in visibleEnemies)
                {
                    if (enemy != null && enemy.Spawned && !enemy.Destroyed)
                    {
                        if (verb.CanHitTarget(bestEnemy))
                        {
                            if (!bestEnemyVisibleNow)
                            {                                
                                bestEnemyVisibleNow = true;                                
                                bestEnemy = enemy;
                                bestEnemyScore = pawn.Position.DistanceToSquared(enemy.Position);                                
                                bestEnemyPositon = enemy.Position;
                            }
                            else
                            {
                                float distSqr = pawn.Position.DistanceToSquared(enemy.Position);
                                if (bestEnemyScore > distSqr)
                                {
                                    bestEnemy = enemy;
                                    bestEnemyScore = distSqr;
                                    bestEnemyPositon = enemy.Position;
                                }
                            }
                        }
                        else if (!bestEnemyVisibleNow)
                        {
                            IntVec3 shiftedPos = enemy.Position;
                            if (enemy is Pawn enemyPawn)
                            {
                                shiftedPos = GetMovingShiftedPosition(enemyPawn, 60);
                            }
                            if (shiftedPos != enemy.Position && verb.CanHitTargetFrom(pawn.Position, shiftedPos))
                            {
                                float distSqr = pawn.Position.DistanceToSquared(shiftedPos);
                                if (bestEnemyScore > distSqr)
                                {
                                    bestEnemy = enemy;
                                    bestEnemyScore = distSqr;
                                    bestEnemyPositon = shiftedPos;
                                    bestEnemyVisibleSoon = true;
                                }
                            }
                            else if(!bestEnemyVisibleSoon)
                            {
                                float distSqr = pawn.Position.DistanceToSquared(shiftedPos) * 2f;
                                if (bestEnemyScore > distSqr)
                                {
                                    bestEnemy = enemy;
                                    bestEnemyScore = distSqr;
                                    bestEnemyPositon = shiftedPos;
                                }
                            }
                        }
                    }
                }
                if (bestEnemy == null)
                {
                    return;
                }
                // parent.Map.debugDrawer.FlashCell(pawn.Position, 0.9f, "s", duration: 100);
                // ------------------------------------------------------------
                float dist = bestEnemyPositon.DistanceToSquared(pawn.Position); ;
                if (dist > 36)
                {
                    if (bestEnemyVisibleNow)
                    {
                        pawn.mindState.enemyTarget = bestEnemy;
                        CastPositionRequest request = new CastPositionRequest();
                        request.caster = pawn;
                        request.target = bestEnemy;
                        request.verb = verb;
                        request.maxRangeFromLocus = Mathf.Min(pawn.Position.DistanceTo(bestEnemy.Position) / 2, 10);
                        request.wantCoverFromTarget = true;
                        if (CastPositionFinder.TryFindCastPosition(request, out IntVec3 cell))
                        {
                            Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
                            job_goto.locomotionUrgency = LocomotionUrgency.Sprint;                            
                            Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);
                            job_waitCombat.checkOverrideOnExpire = true;
                            pawn.jobs.StopAll();
                            pawn.jobs.StartJob(job_goto, JobCondition.InterruptForced);
                            pawn.jobs.jobQueue.EnqueueFirst(job_waitCombat);
                        }
                        else
                        {
                            Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);
                            pawn.jobs.StopAll();
                            pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
                        }
                    }
                    else
                    {
                        pawn.mindState.enemyTarget = bestEnemy;
                        CoverPositionRequest request = new CoverPositionRequest();
                        request.caster = pawn;
                        request.target = new LocalTargetInfo(bestEnemyPositon);
                        request.verb = verb;
                        request.maxRangeFromCaster = Mathf.Min(pawn.Position.DistanceTo(bestEnemy.Position) / 2, 10);
                        request.checkBlockChance = true;
                        if (CoverPositionFinder.TryFindCoverPosition(request, out IntVec3 cell))
                        {
                            Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
                            job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
                            Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);
                            pawn.jobs.StopAll();
                            pawn.jobs.StartJob(job_goto, JobCondition.InterruptForced);
                            pawn.jobs.jobQueue.EnqueueFirst(job_waitCombat);                            
                        }
                        else
                        {
                            Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);
                            pawn.jobs.StopAll();
                            pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
                        }
                    }
                }
                else
                {
                    pawn.mindState.enemyTarget = bestEnemy;
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
                return;
            }
            scanning = true;
            visibleEnemies.Clear();
        }

        public void Notify_EnemiesVisible(IEnumerable<Thing> things)
        {
            if (!scanning)
            {                
                Log.Warning($"ISMA: Notify_EnemiesVisible called while not scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
                return;
            }
            visibleEnemies.AddRange(things);            
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref duties, "duties");
            if(duties == null && parent is Pawn pawn)
            {
                duties = new Pawn_CustomDutyTracker(pawn);
            }
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

