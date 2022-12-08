using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CombatAI.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using GUIUtility = CombatAI.Gui.GUIUtility;
namespace CombatAI.Comps
{
	public class ThingComp_CombatAI : ThingComp
	{

		/// <summary>
		///     Parent armor report.
		/// </summary>
		private ArmorReport armor;
		/// <summary>
		///     Custom pawn duty tracker. Allows the execution of new duties then going back to the old one once the new one is
		///     finished.
		/// </summary>
		public Pawn_CustomDutyTracker duties;

		/// <summary>
		///     Move job started by this comp.
		/// </summary>
		public Job moveJob;
		//Whether a scan is occuring.
		private bool scanning;
		/// <summary>
		///     Parent sight reader.
		/// </summary>
		public SightTracker.SightReader sightReader;
		/// <summary>
		///     Set of visible enemies. A queue for visible enemies during scans.
		/// </summary>
		private readonly HashSet<Thing> visibleEnemies;
		/// <summary>
		///     Wait job started/queued by this comp.
		/// </summary>
		public Job waitJob;

		public ThingComp_CombatAI()
		{
			visibleEnemies = new HashSet<Thing>(32);
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (parent is Pawn pawn)
			{
				armor  = pawn.GetArmorReport();
				duties = new Pawn_CustomDutyTracker(pawn);
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

		public override void CompTickLong()
		{
			base.CompTickLong();
			if (parent is Pawn pawn)
			{
				armor = pawn.GetArmorReport();
			}
		}

		/// <summary>
		///     Returns whether the parent has retreated in the last number of ticks.
		/// </summary>
		/// <param name="ticks">The number of ticks</param>
		/// <returns>Whether the pawn retreated in the last number of ticks</returns>
		public bool RetreatedRecently(int ticks)
		{
			return GenTicks.TicksGame - lastRetreated <= ticks;
		}
		/// <summary>
		///     Returns whether the parent has took damage in the last number of ticks.
		/// </summary>
		/// <param name="ticks">The number of ticks</param>
		/// <returns>Whether the pawn took damage in the last number of ticks</returns>
		public bool TookDamageRecently(int ticks)
		{
			return GenTicks.TicksGame - lastTookDamage <= ticks;
		}
		/// <summary>
		///     Returns whether the parent has reacted in the last number of ticks.
		/// </summary>
		/// <param name="ticks">The number of ticks</param>
		/// <returns>Whether the reacted in the last number of ticks</returns>
		public bool ReactedRecently(int ticks)
		{
			return GenTicks.TicksGame - lastInterupted <= ticks;
		}

		/// <summary>
		///     Called when a scan for enemies starts. Will clear the visible enemy queue. If not called, calling OnScanFinished or
		///     Notify_VisibleEnemy(s) will result in an error.
		///     Should only be called from the main thread.
		/// </summary>
		public void OnScanStarted()
		{
			if (visibleEnemies.Count != 0)
			{
				if (scanning)
				{
					Log.Warning($"ISMA: OnScanStarted called while scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
					return;
				}
				visibleEnemies.Clear();
			}
			scanning    = true;
			lastScanned = GenTicks.TicksGame;
		}

		/// <summary>
		///     Called a scan is finished. This will process enemies queued in visibleEnemies. Responsible for parent reacting.
		///     If OnScanStarted is not called before then this will result in an error.
		///     Should only be called from the main thread.
		/// </summary>
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
			// if no enemies are visible skip.
			if (visibleEnemies.Count == 0)
			{
				return;
			}
			// check if the TPS is good enough.
			if (!Finder.Performance.TpsCriticallyLow)
			{
				// if the pawn haven't seen enemies in a while and recently reacted then reset lastInterupted.
				// This is done to ensure fast reaction times when exiting then entering combat.
				if (GenTicks.TicksGame - lastInterupted < 100 && GenTicks.TicksGame - lastSawEnemies > 90)
				{
					lastInterupted = -1;
					if (Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight)
					{
						parent.Map.debugDrawer.FlashCell(parent.Position, 1.0f, "X", 60);
					}
				}
				lastSawEnemies = GenTicks.TicksGame;
			}
			if (parent is Pawn pawn && !(pawn.RaceProps?.Animal ?? true))
			{
				float bodySize = pawn.RaceProps.baseBodySize;
				// pawn reaction cooldown changes with their bodysize.
				if (GenTicks.TicksGame - lastInterupted < 60 * bodySize || GenTicks.TicksGame - lastRetreated < 65 * bodySize)
				{
					return;
				}
				// if CE is acrive skip reaction if the pawn is reloading or hunkering down.
				if (Mod_CE.active && (pawn.CurJobDef.Is(Mod_CE.ReloadWeapon) || pawn.CurJobDef.Is(Mod_CE.HunkerDown)))
				{
					return;
				}
				// if the pawn is kidnaping a pawn skip.
				if (pawn.CurJobDef.Is(JobDefOf.Kidnap))
				{
					return;
				}
				// Skip if some vanilla duties are active.
				PawnDuty duty = pawn.mindState.duty;
				if (duty != null && (duty.def.Is(DutyDefOf.Build) || duty.def.Is(DutyDefOf.SleepForever) || duty.def.Is(DutyDefOf.TravelOrLeave)))
				{
					lastInterupted = GenTicks.TicksGame + Rand.Int % 240;
					return;
				}
				// pawns above a certain bodysize who are worming up should be skiped.
				// This is mainly for large mech pawns.
				Stance_Warmup warmup = (pawn.stances?.curStance ?? null) as Stance_Warmup;
				if (warmup != null && bodySize > 2.5f)
				{
					return;
				}
				// A not fast check will check for retreat and for reactions to enemies that are visible or soon to be visible.
				// A fast check will check only for retreat.
				bool fastCheck = false;
				if (warmup != null && (warmup.ticksLeft + GenTicks.TicksGame - warmup.startedTick > 120 || warmup.ticksLeft < 30))
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
				if (!verb.Available() || Mod_CE.active && Mod_CE.IsAimingCE(verb))
				{
					return;
				}
				Thing   bestEnemy           = pawn.mindState.enemyTarget;
				IntVec3 bestEnemyPositon    = IntVec3.Invalid;
				IntVec3 pawnPosition        = pawn.Position;
				float   bestEnemyScore      = verb.currentTarget.IsValid && verb.currentTarget.Cell.IsValid ? verb.currentTarget.Cell.DistanceToSquared(pawnPosition) : 1e6f;
				bool    bestEnemyVisibleNow = warmup != null;
				bool    retreat             = false;
				bool    canRetreat          = pawn.RaceProps.baseHealthScale <= 2.0f && pawn.RaceProps.baseBodySize <= 2.2f;
				float   retreatDistSqr      = Maths.Max(verb.EffectiveRange * verb.EffectiveRange / 9, 36);
				foreach (Thing enemy in visibleEnemies)
				{
					if (enemy != null && enemy.Spawned && !enemy.Destroyed)
					{
						IntVec3 shiftedPos = enemy.Position;
						Pawn    enemyPawn  = enemy as Pawn;
						if (enemyPawn != null)
						{
							// skip for children
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
								retreat   = true;
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
									bestEnemy           = enemy;
									bestEnemyScore      = distSqr;
									bestEnemyPositon    = shiftedPos;
								}
								else
								{
									if (bestEnemyScore > distSqr)
									{
										bestEnemy        = enemy;
										bestEnemyScore   = distSqr;
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
										bestEnemy        = enemy;
										bestEnemyScore   = distSqr;
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
					request.caster             = pawn;
					request.target             = new LocalTargetInfo(bestEnemyPositon);
					request.verb               = verb;
					request.maxRangeFromCaster = Maths.Min(retreatDistSqr * 2 / (pawn.BodySize + 0.01f), 15);
					request.checkBlockChance   = true;
					if (CoverPositionFinder.TryFindRetreatPosition(request, out IntVec3 cell) && cell != pawnPosition)
					{
						Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
						job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
						pawn.jobs.ClearQueuedJobs();
						pawn.jobs.StartJob(moveJob = job_goto, JobCondition.InterruptForced);
					}
					else if (warmup == null)
					{
						Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 100 + 100);
						pawn.jobs.ClearQueuedJobs();
						pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
					}
					lastRetreated = GenTicks.TicksGame - Rand.Int % 50;
				}
				else if (!fastCheck)
				{
					bool changedPos = false;
					// 
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
							waitJob                    = null;
							pawn.mindState.enemyTarget = bestEnemy;
							CastPositionRequest request = new CastPositionRequest();
							request.caster              = pawn;
							request.target              = bestEnemy;
							request.verb                = verb;
							request.maxRangeFromTarget  = 9999;
							request.maxRangeFromCaster  = Rand.Chance(Finder.P50 - 0.1f) ? Mathf.Clamp(moveSpeed * 2 / (pawn.BodySize + 0.01f), 4, 10) : 4;
							request.wantCoverFromTarget = true;
							if (CastPositionFinder.TryFindCastPosition(request, out IntVec3 cell))
							{
								if (cell != pawnPosition && (prevEnemyDir == Vector2.zero || Rand.Chance(Mathf.Abs(1 - Vector2.Dot(prevEnemyDir, sightReader.GetEnemyDirection(cell).normalized))) || Rand.Chance(sightReader.GetVisibilityToEnemies(pawn.Position) - sightReader.GetVisibilityToEnemies(cell))))
								{
									Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
									job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
									Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 100 + 100);
									job_waitCombat.checkOverrideOnExpire = true;
									pawn.jobs.ClearQueuedJobs();
									pawn.jobs.StartJob(moveJob              = job_goto, JobCondition.InterruptForced);
									pawn.jobs.jobQueue.EnqueueFirst(waitJob = job_waitCombat);
									changedPos   = true;
									prevEnemyDir = sightReader.GetEnemyDirection(cell).normalized;
								}
								else if (warmup == null)
								{
									pawn.mindState.enemyTarget = bestEnemy;
									Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 100 + 100);
									job_waitCombat.checkOverrideOnExpire = true;
									pawn.jobs.ClearQueuedJobs();
									pawn.jobs.StartJob(waitJob = job_waitCombat, JobCondition.InterruptForced);
								}
							}
						}
						else
						{
							pawn.mindState.enemyTarget = bestEnemy;
							CoverPositionRequest request = new CoverPositionRequest();
							request.caster             = pawn;
							request.target             = new LocalTargetInfo(bestEnemy.Position);
							request.verb               = verb;
							request.maxRangeFromCaster = Rand.Chance(Finder.P50 - 0.1f) ? Mathf.Clamp(moveSpeed * 2 / (pawn.BodySize + 0.01f), 4, 10) : 4;
							request.checkBlockChance   = true;
							if (CoverPositionFinder.TryFindCoverPosition(request, out IntVec3 cell) && cell != pawnPosition)
							{
								if (prevEnemyDir == Vector2.zero || Rand.Chance(Mathf.Abs(1 - Vector2.Dot(prevEnemyDir, sightReader.GetEnemyDirection(cell).normalized))) || Rand.Chance(sightReader.GetVisibilityToEnemies(pawn.Position) - sightReader.GetVisibilityToEnemies(cell)))
								{
									Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
									job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
									Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 100 + 100);
									job_waitCombat.checkOverrideOnExpire = true;
									pawn.jobs.ClearQueuedJobs();
									pawn.jobs.StartJob(moveJob              = job_goto, JobCondition.InterruptForced);
									pawn.jobs.jobQueue.EnqueueFirst(waitJob = job_waitCombat);
									changedPos   = true;
									prevEnemyDir = sightReader.GetEnemyDirection(cell).normalized;
								}
							}
						}
					}
					else if (warmup == null)
					{
						waitJob                    = null;
						pawn.mindState.enemyTarget = bestEnemy;
						Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 100 + 100);
						pawn.jobs.ClearQueuedJobs();
						pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
					}
					if (changedPos)
					{
						lastInterupted = lastMoved = GenTicks.TicksGame;
					}
					else
					{
						lastInterupted = GenTicks.TicksGame - Rand.Int % 30;
					}
				}
			}
		}

		/// <summary>
		///     Called When the parent takes damage.
		/// </summary>
		/// <param name="dInfo">Damage info</param>
		public void Notify_TookDamage(DamageInfo dInfo)
		{
			// if the pawn is tanky enough skip.
			if (parent.Spawned && GenTicks.TicksGame - lastScanned < 90 && parent is Pawn pawn && !pawn.Dead && !pawn.Downed && armor.TankInt < 0.4f)
			{
				if (!pawn.RaceProps.IsMechanoid && dInfo.Def != null && dInfo.Instigator != null)
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
							float enemyRange = dInfo.Instigator.TryGetAttackVerb()?.EffectiveRange ?? 5f;
							float armorVal   = armor.GetArmor(dInfo.Def);
							if (armorVal == 0 || Rand.Chance(dInfo.ArmorPenetrationInt / armorVal) || GenTicks.TicksGame - lastTookDamage < 30 && Rand.Chance(0.50f))
							{
								IntVec3 pawnPosition = parent.Position;
								waitJob                    = null;
								pawn.mindState.enemyTarget = dInfo.Instigator;
								CoverPositionRequest request = new CoverPositionRequest();
								request.caster             = pawn;
								request.target             = new LocalTargetInfo(dInfo.Instigator);
								request.verb               = effectiveVerb;
								request.maxRangeFromCaster = Maths.Min(enemyRange * 2 / (pawn.BodySize + 0.01f), 15);
								request.checkBlockChance   = true;
								if (CoverPositionFinder.TryFindRetreatPosition(request, out IntVec3 cell) && cell != pawnPosition)
								{
									Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
									job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
									Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 100 + 100);
									pawn.jobs.StartJob(moveJob = job_goto, JobCondition.InterruptForced);
									pawn.jobs.ClearQueuedJobs();
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

		/// <summary>
		///     Enqueue enemies for reaction processing.
		/// </summary>
		/// <param name="things">Spotted enemies</param>
		public void Notify_EnemiesVisible(IEnumerable<Thing> things)
		{
			if (!scanning)
			{
				Log.Warning($"ISMA: Notify_EnemiesVisible called while not scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
				return;
			}
			visibleEnemies.AddRange(things);
		}

		/// <summary>
		///     Enqueue enemy for reaction processing.
		/// </summary>
		/// <param name="things">Spotted enemy</param>
		public void Notify_EnemyVisible(Thing thing)
		{
			if (!scanning)
			{
				Log.Warning($"ISMA: Notify_EnemiesVisible called while not scanning. ({visibleEnemies.Count}, {Thread.CurrentThread.ManagedThreadId})");
				return;
			}
			visibleEnemies.Add(thing);
		}

		/// <summary>
		///     Called to notify a wait job started by reaction has ended. Will reduce the reaction cooldown.
		/// </summary>
		public void Notify_WaitJobEnded()
		{
			lastInterupted -= 30;
		}

		/// <summary>
		///     Called when the parent sightreader group has changed.
		///     Should only be called from SighTracker/SightGrid.
		/// </summary>
		/// <param name="reader">The new sightReader</param>
		public void Notify_SightReaderChanged(SightTracker.SightReader reader)
		{
			sightReader = reader;
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

		#region TimeStamps

		/// <summary>
		///     When the pawn was last order to move by CAI.
		/// </summary>
		private int lastMoved;
		/// <summary>
		///     When the last injury occured/damage.
		/// </summary>
		private int lastTookDamage;
		/// <summary>
		///     When the last scan occured. SightGrid is responisble for these scan cycles.
		/// </summary>
		private int lastScanned;
		/// <summary>
		///     When did this comp last interupt the parent pawn. IE: reacted, retreated, etc.
		/// </summary>
		private int lastInterupted;
		/// <summary>
		///     When the pawn was last order to retreat by CAI.
		/// </summary>
		private int lastRetreated;
		/// <summary>
		///     Last tick any enemies where reported in a scan.
		/// </summary>
		private int lastSawEnemies;
		/// <summary>
		///     The general direction of enemies last time the pawn reacted.
		/// </summary>
		private Vector2 prevEnemyDir = Vector2.zero;

		#endregion

#if DEBUG_REACTION

		/*
         * Debug only vars.
         */

		public override void DrawGUIOverlay()
		{
			if (Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight && parent is Pawn pawn)
			{
				base.DrawGUIOverlay();
				Verb  verb          = pawn.CurrentEffectiveVerb;
				float sightRange    = Maths.Min(SightUtility.GetSightRadius(pawn).scan, verb.EffectiveRange);
				float sightRangeSqr = sightRange * sightRange;
				if (sightRange != 0 && verb != null)
				{
					Vector3 drawPos    = pawn.DrawPos;
					IntVec3 shiftedPos = PawnPathUtility.GetMovingShiftedPosition(pawn, 30);
					List<Pawn> nearbyVisiblePawns = pawn.Position.ThingsInRange(pawn.Map, TrackedThingsRequestCategory.Pawns, sightRange)
						.Select(t => t as Pawn)
						.Where(p => !p.Dead && !p.Downed && PawnPathUtility.GetMovingShiftedPosition(p, 60).DistanceToSquared(shiftedPos) < sightRangeSqr && verb.CanHitTargetFrom(shiftedPos, PawnPathUtility.GetMovingShiftedPosition(p, 60)) && p.HostileTo(pawn))
						.ToList();
					GUIUtility.ExecuteSafeGUIAction(() =>
					{
						Vector2 drawPosUI = drawPos.MapToUIPosition();
						Text.Font = GameFont.Tiny;
						string state = GenTicks.TicksGame - lastInterupted > 120 ? "<color=blue>O</color>" : "<color=yellow>X</color>";
						Widgets.Label(new Rect(drawPosUI.x - 25, drawPosUI.y - 15, 50, 30), $"{state}/{_visibleEnemies.Count}");
					});
					bool bugged = nearbyVisiblePawns.Count != _visibleEnemies.Count;
					if (bugged)
					{
						Rect    rect;
						Vector2 a = drawPos.MapToUIPosition();
						Vector2 b;
						Vector2 mid;
						foreach (Pawn other in nearbyVisiblePawns.Where(p => !_visibleEnemies.Contains(p)))
						{
							b = other.DrawPos.MapToUIPosition();
							Widgets.DrawLine(a, b, Color.red, 1);

							mid  = (a + b) / 2;
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
							Widgets.DrawBoxSolid(new Rect((_path[i - 1].ToVector3().Yto0() + new Vector3(0.5f, 0, 0.5f)).MapToUIPosition() - new Vector2(5, 5), new Vector2(10, 10)), _colors[i]);
							Widgets.DrawLine((_path[i - 1].ToVector3().Yto0() + new Vector3(0.5f, 0, 0.5f)).MapToUIPosition(), (_path[i].ToVector3().Yto0() + new Vector3(0.5f, 0, 0.5f)).MapToUIPosition(), Color.white, 1);
						}
						if (_path.Count > 0)
						{
							Vector2 v = pawn.DrawPos.Yto0().MapToUIPosition();
							Widgets.DrawLine((_path.Last().ToVector3().Yto0() + new Vector3(0.5f, 0, 0.5f)).MapToUIPosition(), v, _colors.Last(), 1);
							Widgets.DrawBoxSolid(new Rect(v - new Vector2(5, 5), new Vector2(10, 10)), _colors.Last());
						}
						if (!_visibleEnemies.EnumerableNullOrEmpty())
						{
							Vector2 a = pawn.DrawPos.MapToUIPosition();
							Vector2 b;
							Vector2 mid;
							Rect    rect;
							int     index = 0;
							foreach (Pawn other in _visibleEnemies)
							{
								b = other.DrawPos.MapToUIPosition();
								Widgets.DrawLine(a, b, Color.blue, 1);

								mid  = (a + b) / 2;
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

		private readonly HashSet<Pawn> _visibleEnemies = new HashSet<Pawn>();
		private readonly List<IntVec3> _path           = new List<IntVec3>();
		private readonly List<Color>   _colors         = new List<Color>();
#endif
	}
}
