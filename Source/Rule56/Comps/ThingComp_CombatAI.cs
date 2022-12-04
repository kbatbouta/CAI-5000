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
using Unity.Baselib.LowLevel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CombatAI.Comps
{
    public class ThingComp_CombatAI : ThingComp
    {
		#region States

		/*
         * States 
         */

		private bool scanning;

		#endregion

		#region TimeStamps

		/*
         * Time stamps 
         */

		/// <summary>
		/// When the pawn was last order to move by CAI.
		/// </summary>
		private int lastMoved;
		/// <summary>
		/// When the last injury occured/damage. 
		/// </summary>
		private int lastTookDamage;
		/// <summary>
		/// When the last scan occured. SightGrid is responisble for these scan cycles.
		/// </summary>
		private int lastScanned;
		/// <summary>
		/// When did this comp last interupt the parent pawn. IE: reacted, retreated, etc.
		/// </summary>
		private int lastInterupted;
		/// <summary>
		/// When the pawn was last order to retreat by CAI.
		/// </summary>
		private int lastRetreated;
		/// <summary>
		/// Last tick any enemies where reported in a scan.
		/// </summary>
		private int lastSawEnemies;

		#endregion        
		
        /// <summary>
        /// Parent armor report.
        /// </summary>
		private ArmorReport armor;
        /// <summary>
        /// Set of visible enemies. A queue for visible enemies during scans.
        /// </summary>
		private HashSet<Thing> visibleEnemies;

        /// <summary>
        /// Move job started by this comp.
        /// </summary>
		public Job moveJob;		
        /// <summary>
        /// Wait job started/queued by this comp.
        /// </summary>
		public Job waitJob;
        /// <summary>
        /// Custom pawn duty tracker. Allows the execution of new duties then going back to the old one once the new one is finished.
        /// </summary>
        public Pawn_CustomDutyTracker duties;
        /// <summary>
        /// Parent sight reader.
        /// </summary>
        public SightTracker.SightReader sightReader;
       
        public ArmorReport CachedArmorReport
        {
            get => armor;
        }

		public ThingComp_CombatAI()
        {
            this.visibleEnemies = new HashSet<Thing>(32);            
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent is Pawn pawn)
            {
                this.armor = ArmorUtility.GetArmorReport(pawn);
				this.duties = new Pawn_CustomDutyTracker(pawn);				
			}
        } 

#if DEBUG_REACTION

        public override void DrawGUIOverlay()
        {
            if (Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight && parent is Pawn pawn)
            {
                base.DrawGUIOverlay();
                var verb = pawn.CurrentEffectiveVerb;
                var sightRange = Maths.Min(SightUtility.GetSightRange(pawn), verb.EffectiveRange);
                var sightRangeSqr = sightRange * sightRange;
                if (sightRange != 0 && verb != null)
                {
                    Vector3 drawPos = pawn.DrawPos;                    
                    IntVec3 shiftedPos = PawnPathUtility.GetMovingShiftedPosition(pawn, 30);
                    List<Pawn> nearbyVisiblePawns = GenClosest.ThingsInRange(pawn.Position, pawn.Map, Utilities.TrackedThingsRequestCategory.Pawns, sightRange)
                        .Select(t => t as Pawn)
                        .Where(p => !p.Dead && !p.Downed && PawnPathUtility.GetMovingShiftedPosition(p, 60).DistanceToSquared(shiftedPos) < sightRangeSqr && verb.CanHitTargetFrom(shiftedPos, PawnPathUtility.GetMovingShiftedPosition(p, 60)) && p.HostileTo(pawn))
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
                        if (_path.Count > 0)
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
#endif		
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

		public override void CompTickLong()
		{
			base.CompTickLong();
			if (parent is Pawn pawn)
			{
				this.armor = ArmorUtility.GetArmorReport(pawn);
			}
		}

        /// <summary>
        /// Returns whether the parent has retreated in the last number of ticks. 
        /// </summary>
        /// <param name="ticks">The number of ticks</param>
        /// <returns>Whether the pawn retreated in the last number of ticks</returns>
		public bool RetreatedRecently(int ticks)
		{
			return GenTicks.TicksGame - lastRetreated <= ticks;
		}
		/// <summary>
		/// Returns whether the parent has took damage in the last number of ticks. 
		/// </summary>
		/// <param name="ticks">The number of ticks</param>
		/// <returns>Whether the pawn took damage in the last number of ticks</returns>
		public bool TookDamageRecently(int ticks)
		{
			return GenTicks.TicksGame - lastTookDamage <= ticks;
		}
		/// <summary>
		/// Returns whether the parent has reacted in the last number of ticks. 
		/// </summary>
		/// <param name="ticks">The number of ticks</param>
		/// <returns>Whether the reacted in the last number of ticks</returns>
		public bool ReactedRecently(int ticks)
		{
			return GenTicks.TicksGame - lastInterupted <= ticks;
		}

		public void OnScanStarted()
		{            
            if (visibleEnemies.Count != 0)
            {
                if (scanning == true)
                {
                    Log.Warning($"ISMA: OnScanStarted called while scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
                    return;
                }                
                visibleEnemies.Clear();
            }
			scanning = true;
			lastScanned = GenTicks.TicksGame;
		}        

		public void OnScanFinished()
        {
            if (scanning == false)
            {
                Log.Warning($"ISMA: OnScanFinished called while not scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
                return;
            }
            scanning = false;
#if DEBUG_REACTION
            if (Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight)
            {
                _visibleEnemies.Clear();
                _visibleEnemies.AddRange(visibleEnemies.Where(t => t is Pawn).Select(t => t as Pawn));
                if (_path.Count == 0 || _path.Last() != parent.Position)
                {
                    _path.Add(parent.Position);
                    if (GenTicks.TicksGame - lastInterupted < 150)
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
#endif
			if (visibleEnemies.Count == 0)
			{
                return;
			}
			if (!Finder.Performance.TpsCriticallyLow)
            {
                if (GenTicks.TicksGame - lastInterupted < 100 && GenTicks.TicksGame - lastSawEnemies > 90)
                {
                    lastInterupted = -1;
                    if (Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight)
                    {
                        parent.Map.debugDrawer.FlashCell(parent.Position, 1.0f, "X", duration: 60);
                    }
                }
                lastSawEnemies = GenTicks.TicksGame;
            }             
            if (parent is Pawn pawn && !(pawn.RaceProps?.Animal ?? true))
            {
                float bodySize = pawn.RaceProps.baseBodySize;
				if (GenTicks.TicksGame - lastInterupted < 60 * bodySize || GenTicks.TicksGame - lastRetreated < 65 * bodySize)
				{
					return;
				}		
				if (Mod_CE.active && (pawn.CurJobDef.Is(Mod_CE.ReloadWeapon) || pawn.CurJobDef.Is(Mod_CE.HunkerDown)))
                {					
					return;
                }
                if (pawn.CurJobDef.Is(JobDefOf.Kidnap))
                {
                    return;
                }
                PawnDuty duty = pawn.mindState.duty;
				if (duty != null && (duty.def.Is(DutyDefOf.Build) || duty.def.Is(DutyDefOf.SleepForever) || duty.def.Is(DutyDefOf.TravelOrLeave)))
                {
                    lastInterupted = GenTicks.TicksGame + Rand.Int % 240;
                    return;
                }
                Stance_Warmup warmup = (pawn.stances?.curStance ?? null) as Stance_Warmup;
				if (warmup != null && bodySize > 2.5f)
				{
					return;
				}
				bool fastCheck = false;
				if (warmup != null && ((warmup.ticksLeft + GenTicks.TicksGame - warmup.startedTick) > 120 || warmup.ticksLeft < 30))
                { 
					fastCheck = true;
				}               
				Verb verb = parent.TryGetAttackVerb();
                if (verb == null)
                {
                    return;
                }
                if (verb.IsMeleeAttack)
                {
                    if (pawn.CurJobDef == JobDefOf.Mine)
                    {
                        pawn.jobs.StopAll();                        
                    }
					return;
				}
				if (!verb.Available() || (Mod_CE.active && Mod_CE.IsAimingCE(verb)))
				{
					return;
				}				
				Thing bestEnemy = pawn.mindState.enemyTarget;
                IntVec3 bestEnemyPositon = IntVec3.Invalid;
                IntVec3 pawnPosition = pawn.Position;
                float bestEnemyScore = verb.currentTarget.IsValid && verb.currentTarget.Cell.IsValid ? verb.currentTarget.Cell.DistanceToSquared(pawnPosition) : 1e6f;           
                bool bestEnemyVisibleNow = warmup != null;                
                bool retreat = false;
                bool canRetreat = pawn.RaceProps.baseHealthScale <= 2.0f && pawn.RaceProps.baseBodySize <= 2.2f;                
				float retreatDistSqr = Maths.Max(verb.EffectiveRange * verb.EffectiveRange / 9, 25);               
				foreach (Thing enemy in visibleEnemies)
                {                   
                    if (enemy != null && enemy.Spawned && !enemy.Destroyed)
                    {                        
                        IntVec3 shiftedPos = enemy.Position;
                        Pawn enemyPawn = enemy as Pawn;                        					
						if (enemyPawn != null)
						{
							DevelopmentalStage stage = enemyPawn.DevelopmentalStage;							
							if (stage <= DevelopmentalStage.Child && stage != DevelopmentalStage.None)
							{
								continue;
							}
							shiftedPos = PawnPathUtility.GetMovingShiftedPosition(enemyPawn, 60);
						}
						float distSqr = pawnPosition.DistanceToSquared(shiftedPos);
                        if (canRetreat && distSqr < retreatDistSqr)
                        {
                            if (enemyPawn != null && distSqr < 49)
                            {
                                bestEnemy = enemy;
                                retreat = true;
                                break;
                            }
                        }                        
                        if (!fastCheck)
                        {
                            if (verb.CanHitTarget(enemy.Position))
                            {
                                if (!bestEnemyVisibleNow)
                                {
                                    bestEnemyVisibleNow = true;
                                    bestEnemy = enemy;
                                    bestEnemyScore = distSqr;
                                    bestEnemyPositon = shiftedPos;
                                }
                                else
                                {
                                    if (bestEnemyScore > distSqr)
                                    {
                                        bestEnemy = enemy;
                                        bestEnemyScore = distSqr;
                                        bestEnemyPositon = shiftedPos;
                                    }
                                }                                
                            }
                            else if (!bestEnemyVisibleNow)
                            {								
								if (shiftedPos != enemy.Position && verb.CanHitTarget(shiftedPos))
                                {
                                    distSqr = pawnPosition.DistanceToSquared(shiftedPos);
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
                }
                if (bestEnemy == null)
				{
                    return;
                }
                if (retreat)
                {
                    waitJob = null;
					//pawn.Map.debugDrawer.FlashCell(pawn.Position, 1f, "FLEE", 200);
					pawn.mindState.enemyTarget = bestEnemy;
                    CoverPositionRequest request = new CoverPositionRequest();
                    request.caster = pawn;
                    request.target = new LocalTargetInfo(bestEnemyPositon);
                    request.verb = verb;
                    request.maxRangeFromCaster = Maths.Min(retreatDistSqr * 2 / (pawn.BodySize + 0.01f), 15);
                    request.checkBlockChance = true;
                    if (CoverPositionFinder.TryFindRetreatPosition(request, out IntVec3 cell) && cell != pawnPosition)
                    {
                        Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
                        job_goto.locomotionUrgency = LocomotionUrgency.Sprint;                        
                        pawn.jobs.StartJob(moveJob = job_goto, JobCondition.InterruptForced);                        
                    }
                    else if(warmup == null)
                    {
                        Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);                        
                        pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
                    }
                    lastRetreated = GenTicks.TicksGame - Rand.Int % 50;
                }
                else if(!fastCheck)
                {                    
                    bool changedPos = false;
					// parent.Map.debugDrawer.FlashCell(pawn.Position, 0.9f, "s", duration: 100);
					// ------------------------------------------------------------
					float moveSpeed = pawn.GetStatValue_Fast(StatDefOf.MoveSpeed, 450);
                    if (pawn.stances?.stagger?.Staggered ?? false)
                    {
                        moveSpeed = pawn.stances.stagger.StaggerMoveSpeedFactor;
					}
					float dist = bestEnemyScore;
                    if (dist > 25)
                    {
                        if (bestEnemyVisibleNow)
                        {
							waitJob = null;
							pawn.mindState.enemyTarget = bestEnemy;
                            CastPositionRequest request = new CastPositionRequest();
                            request.caster = pawn;
                            request.target = bestEnemy;
                            request.verb = verb;
                            request.maxRangeFromTarget = 9999;
							request.maxRangeFromCaster = Rand.Chance(Finder.P50 - 0.1f) ? Mathf.Clamp(moveSpeed * 2 / (pawn.BodySize + 0.01f), 3, 10) : 3;
                            request.wantCoverFromTarget = true;
                            if (CastPositionFinder.TryFindCastPosition(request, out IntVec3 cell) && (cell != pawnPosition || warmup == null))
                            {
                                Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
                                job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
                                Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);
                                job_waitCombat.checkOverrideOnExpire = true;                                
                                pawn.jobs.StartJob(moveJob = job_goto, JobCondition.InterruptForced);
                                pawn.jobs.jobQueue.EnqueueFirst(job_waitCombat);								
								changedPos = true;
                            }
                            else if (warmup == null)
                            {
                                Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);
                                pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
                            }
                        }
                        else
                        {
							pawn.mindState.enemyTarget = bestEnemy;
                            CoverPositionRequest request = new CoverPositionRequest();
                            request.caster = pawn;
                            request.target = new LocalTargetInfo(bestEnemy.Position);
                            request.verb = verb;
                            request.maxRangeFromCaster = Rand.Chance(Finder.P50 - 0.1f) ? Mathf.Clamp(moveSpeed * 2 / (pawn.BodySize + 0.01f), 3, 10) : 3;
							request.checkBlockChance = true;
                            if (CoverPositionFinder.TryFindCoverPosition(request, out IntVec3 cell) && cell != pawnPosition && warmup == null)
                            {
                                Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
                                job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
                                Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);                                
                                pawn.jobs.StartJob(moveJob = job_goto, JobCondition.InterruptForced);
                                job_waitCombat.verbToUse = verb;
                                job_waitCombat.targetC = bestEnemy;								
								pawn.jobs.jobQueue.EnqueueFirst(waitJob = job_waitCombat);								
                                changedPos = true;
							}                          
                        }
                    }
                    else
                    {
						waitJob = null;
						pawn.mindState.enemyTarget = bestEnemy;
                        Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);						
						pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
                    }
                    if (changedPos)
                    {
						lastInterupted = lastMoved = GenTicks.TicksGame;
                    }
                    else
                    {
                        lastInterupted = GenTicks.TicksGame - Rand.Int % 60;
					}
				}                
            }
        }       

		public void Notify_TookDamage(DamageInfo dInfo)
		{
			if (parent.Spawned && GenTicks.TicksGame - lastScanned < 90 && parent is Pawn pawn && !pawn.Dead && !pawn.Downed && armor.TankInt < 0.4f)
			{			
				if (dInfo.Def != null && dInfo.Instigator != null)
                {
                    if (pawn.CurJobDef.Is(JobDefOf.Mine))
                    {
                        pawn.jobs.StopAll();
                    }
                    else
                    {
                        Verb effectiveVerb = pawn.CurrentEffectiveVerb;
                        if (effectiveVerb != null && effectiveVerb.Available() && effectiveVerb.EffectiveRange > 5)
                        {
                            float enemyRange = SightUtility.GetSightRange(dInfo.Instigator);
                            float armorVal = armor.GetArmor(dInfo.Def);
                            if (armorVal == 0 || Rand.Chance(dInfo.ArmorPenetrationInt / armorVal) || (GenTicks.TicksGame - lastTookDamage < 30 && Rand.Chance(0.50f)))
                            {
                                IntVec3 pawnPosition = parent.Position;
                                waitJob = null;                                
                                pawn.mindState.enemyTarget = dInfo.Instigator;
                                CoverPositionRequest request = new CoverPositionRequest();
                                request.caster = pawn;
                                request.target = new LocalTargetInfo(dInfo.Instigator);
                                request.verb = effectiveVerb;
                                request.maxRangeFromCaster = Maths.Min(enemyRange * 2 / (pawn.BodySize + 0.01f), 15);
                                request.checkBlockChance = true;
                                if (CoverPositionFinder.TryFindRetreatPosition(request, out IntVec3 cell) && cell != pawnPosition)
                                {
                                    Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
                                    job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
                                    Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, expiryInterval: Rand.Int % 100 + 100);
                                    pawn.jobs.StartJob(moveJob = job_goto, JobCondition.InterruptForced);
                                    pawn.jobs.jobQueue.EnqueueFirst(job_waitCombat);
                                }								
								lastRetreated = GenTicks.TicksGame - Rand.Int % 50;
                            }
                        }
                    }
                }
            }
			lastTookDamage = GenTicks.TicksGame;
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

		public void Notify_EnemyVisible(Thing thing)
		{
			if (!scanning)
			{
				Log.Warning($"ISMA: Notify_EnemiesVisible called while not scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
				return;
			}
			visibleEnemies.Add(thing);
		}

        public void Notify_WaitJobEnded()
        {
            this.lastInterupted -= 30;
        }

		public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref duties, "duties");         
            if (parent is Pawn pawn)
            {
                if (duties == null)
                {
                    duties = new Pawn_CustomDutyTracker(pawn);
                }
                duties.pawn = pawn;
            }
        }        

        public void Notify_SightReaderChanged(SightTracker.SightReader reader)
        {
            this.sightReader = reader;
        }

#if DEBUG_REACTION

        /*
         * Debug only vars.
         */

		private HashSet<Pawn> _visibleEnemies = new HashSet<Pawn>();
		private List<IntVec3> _path = new List<IntVec3>();
		private List<Color> _colors = new List<Color>();
#endif
	}
}

