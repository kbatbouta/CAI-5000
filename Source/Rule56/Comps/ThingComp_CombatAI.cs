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
		private int lastChangedOrRetreated = 0;
		/// <summary>
		///     Set of visible enemies. A queue for visible enemies during scans.
		/// </summary>
		private readonly HashSet<Thing> visibleEnemies;
		/// <summary>
		///     Set of visible enemies. A queue for visible enemies during scans.
		/// </summary>
		private readonly HashSet<Thing> visibleEnemiesTargetingSelf = new HashSet<Thing>(32);
		/// <summary>
		///     Set of visible enemies. A queue for visible enemies during scans.
		/// </summary>
		private readonly HashSet<Thing> visibleEnemiesOutRangeSelf = new HashSet<Thing>(32);
		/// <summary>
		///     Set of visible enemies. A queue for visible enemies during scans.
		/// </summary>
		private readonly List<Thing> visibleEnemiesInRange = new List<Thing>(32);

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
		///		Number of hits parent was able to land on the enemy.
		/// </summary>
		public float hitsLanded;
		/// <summary>
		///		ML model.
		/// </summary>
		public Sequential sequential = SeqDefaults.reaction;

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
				visibleEnemiesTargetingSelf.Clear();
				visibleEnemiesInRange.Clear();
				visibleEnemiesOutRangeSelf.Clear();
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

			// if no enemies are visible skip.
			if (visibleEnemies.Count == 0)
			{
				return;
			}			
			if (parent is Pawn pawn && !(pawn.RaceProps?.Animal ?? true) && sequential != null)
			{
				if (pawn.GetAIType() == AIType.vanilla)
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
					return;
				}
				var verb = pawn.CurrentEffectiveVerb;
				if (verb.IsMeleeAttack)
				{
					return;
				}
				Thing nearestEnemy = null;
				float nearestEnemyDist = 1e4f;
				
				var position = pawn.Position;				
				foreach (Thing enemy in visibleEnemies)
				{
					var enemyPos = default(IntVec3);
					var enemyPawn = enemy as Pawn;
					if (enemyPawn != null)
					{
						DevelopmentalStage stage = enemyPawn.DevelopmentalStage;
						if (stage <= DevelopmentalStage.Child && stage != DevelopmentalStage.None)
						{
							continue;
						}
						enemyPos = PawnPathUtility.GetMovingShiftedPosition(pawn, 30);
					}
					else
					{
						enemyPos = enemy.Position;
					}
					float dist = Maths.Sqrt_Fast(enemyPos.DistanceToSquared(position), 4);
					if (nearestEnemyDist > dist)
					{
						nearestEnemyDist = dist;
						nearestEnemy = enemy;
					}
					bool clearLosTested = false;
					bool clearLos = false;
					if ((clearLosTested = dist < verb.EffectiveRange - 2) && (clearLos = verb.CanHitTarget(enemy)))
					{						
						visibleEnemiesInRange.Add(enemy);
					}
					Verb enemyVerb = enemy.TryGetAttackVerb();					
					if (enemyVerb != null && ((clearLosTested && clearLos) || (!clearLosTested && enemyVerb.CanHitTarget(parent))))
					{
						if (!clearLosTested)
						{
							visibleEnemiesOutRangeSelf.Add(enemy);
						}
						if (enemyVerb.currentTarget.IsValid && enemyVerb.currentTarget.Cell.DistanceToSquared(position) < 9)
						{
							visibleEnemiesTargetingSelf.Add(enemy);
						}
					}
				}
				Tensor2 dTensor = sequential.Evaluate(
					(float)visibleEnemiesTargetingSelf.Count / 3f,
					(float)visibleEnemiesInRange.Count / 3f,
					Maths.Min(GenTicks.TicksGame - lastChangedOrRetreated, 240f) / 240f,
					1f - Maths.Min(GenTicks.TicksGame - lastTookDamage, 240f) / 240f,					
					sightReader.GetThreat(position),
					sightReader.GetVisibilityToEnemies(position));
				var decision = -1;
				var dScore = -1f;
				for(int i = 0;i < 3; i++)
				{
					if (dTensor[0, i] > dScore)
					{
						dScore = dTensor[0, i];
						decision = i;
					}
				}
				switch (decision)
				{
					default:
						// TODO warmup stance	
						//
						WarmUp();
						parent.Map.debugDrawer.FlashCell(position, 0.01f, "0", 15);			
						return;	
					case 1:
						parent.Map.debugDrawer.FlashCell(position, 0.50f, "1", 15);
						ChangePos(nearestEnemy);
						lastChangedOrRetreated = GenTicks.TicksGame;
						return;
					case 2:
						parent.Map.debugDrawer.FlashCell(position, 0.99f, "2", 15);
						Retreat(nearestEnemy);
						lastChangedOrRetreated = GenTicks.TicksGame;
						return;
				}						
			}
		}

		public void WarmUp()
		{
			Pawn pawn = parent as Pawn;
			if (!(pawn.jobs.curJob?.def.Is(JobDefOf.Wait_Combat) ?? false))
			{
				Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 200 + 200);
				pawn.jobs.ClearQueuedJobs();
				pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
			}
		}

		/// <summary>
		/// TODO
		/// </summary>
		public void ChangePos(Thing nearestEnemy)
		{
			Pawn pawn = parent as Pawn;
			Verb verb = pawn.CurrentEffectiveVerb;			
			CoverPositionRequest request = new CoverPositionRequest();
			request.caster = pawn;			
			request.verb = pawn.CurrentEffectiveVerb;
			request.target = new LocalTargetInfo(nearestEnemy);
			request.maxRangeFromCaster = Rand.Chance(Finder.P50 - 0.1f) ? Mathf.Clamp(pawn.GetStatValue_Fast(StatDefOf.MoveSpeed, 900) * 2 / (pawn.BodySize + 0.01f), 4, 10) : 4;
			request.checkBlockChance = true;	
			if (CoverPositionFinder.TryFindCoverPosition(request, out IntVec3 cell))
			{				
				Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
				job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
				Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 200 + 200);
				job_waitCombat.checkOverrideOnExpire = true;
				pawn.jobs.ClearQueuedJobs();
				pawn.jobs.StartJob(moveJob = job_goto, JobCondition.InterruptForced);
				pawn.jobs.jobQueue.EnqueueFirst(waitJob = job_waitCombat);				
				prevEnemyDir = sightReader.GetEnemyDirection(cell).normalized;
			}
			else if (!(pawn.jobs.curJob?.def.Is(JobDefOf.Wait_Combat) ?? false))
			{
				Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 200 + 200);
				pawn.jobs.ClearQueuedJobs();
				pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
			}
		}

		/// <summary>
		/// TODO
		/// </summary>
		/// <param name="nearestEnemy"></param>
		public void Retreat(Thing nearestEnemy)
		{
			Pawn pawn = parent as Pawn;
			pawn.mindState.enemyTarget = nearestEnemy;
			CoverPositionRequest request = new CoverPositionRequest();
			request.caster = pawn;
			request.target = new LocalTargetInfo(nearestEnemy?.Position ?? pawn.Position);
			request.verb = pawn.CurrentEffectiveVerb;
			request.maxRangeFromCaster = 15;
			request.checkBlockChance = true;
			if (CoverPositionFinder.TryFindRetreatPosition(request, out IntVec3 cell) && cell != pawn.Position)
			{				
				Job job_goto = JobMaker.MakeJob(JobDefOf.Goto, cell);
				job_goto.locomotionUrgency = LocomotionUrgency.Sprint;
				pawn.jobs.ClearQueuedJobs();
				pawn.jobs.StopAll();
				pawn.jobs.StartJob(moveJob = job_goto, JobCondition.InterruptForced);				
			}
			else if (!(pawn.jobs.curJob?.def.Is(JobDefOf.Wait_Combat) ?? false))
			{
				Job job_waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat, Rand.Int % 200 + 200);
				pawn.jobs.ClearQueuedJobs();
				pawn.jobs.StartJob(job_waitCombat, JobCondition.InterruptForced);
			}
		}

		/// <summary>
		///     Called When the parent takes damage.
		/// </summary>
		/// <param name="dInfo">Damage info</param>
		public void Notify_TookDamage(DamageInfo dInfo)
		{
			// if the pawn is tanky enough skip.
			ThingComp_CombatAI comp = dInfo.Instigator?.GetComp_Fast<ThingComp_CombatAI>() ?? null;
			if (comp != null && (dInfo.Weapon?.IsRangedWeapon ?? false))
			{
				VerbProperties props = dInfo.Weapon.verbs.MaxBy(v => v.burstShotCount);				
				comp.hitsLanded += props.warmupTime / (props.burstShotCount + 0.01f);				
			}
			hitsLanded *= 0.97f;
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
