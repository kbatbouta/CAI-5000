using System;
using System.Collections.Generic;
using CombatAI.Comps;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace CombatAI
{
	public class SightGrid
	{
		private const    int                        COVERCARRYLIMIT = 6;
		private readonly AsyncActions               asyncActions;
		private readonly IBuckets<IBucketableThing> buckets;
		private readonly List<Vector3>              buffer = new List<Vector3>(1024);
		private readonly CellFlooder                flooder;
		/// <summary>
		///     Sight grid contains all sight data.
		/// </summary>
		public readonly ITSignalGrid grid;
		/// <summary>
		///     Region grid contains sight data for regions.
		/// </summary>
		public readonly ITRegionGrid grid_regions;
		/// <summary>
		///     SightGrid Id.
		/// </summary>
		public readonly int gridId;
		/// <summary>
		///     Parent map.
		/// </summary>
		public readonly Map map;
		private readonly Dictionary<Faction, int> numsByFaction = new Dictionary<Faction, int>();
		/// <summary>
		///     Performance settings.
		/// </summary>
		public readonly Settings.SightPerformanceSettings settings;
		/// <summary>
		///     Parent map sight tracker.
		/// </summary>
		public readonly SightTracker sightTracker;
		private readonly List<Thing> thingBuffer1 = new List<Thing>(256);
		private readonly List<Thing> thingBuffer2 = new List<Thing>(256);
		/// <summary>
		///     Things UInt64 map.
		/// </summary>
		public readonly IThingsUInt64Map thingsUInt64Map;
		private readonly List<IBucketableThing> tmpDeRegisterList      = new List<IBucketableThing>(64);
		private readonly List<IBucketableThing> tmpInconsistentRecords = new List<IBucketableThing>(64);
		private readonly List<IBucketableThing> tmpInvalidRecords      = new List<IBucketableThing>(64);

		private readonly List<Thing> _getThings = new List<Thing>();
		private          WallGrid    _walls;
		/// <summary>
		///     Fog of war grid. Can be null.
		/// </summary>
		public ITFloatGrid gridFog;
		private int ops;
		/// <summary>
		///     Whether this is the player grid
		/// </summary>
		public bool playerAlliance = false;

		private int regionUpdateIndex;

		private IntVec3 suCentroid;
		private IntVec3 suCentroidPrev;
		/// <summary>
		///     Super rect containing all none sighter things casting.
		/// </summary>
		private CellRect suRect_Combatant = CellRect.Empty;
		/// <summary>
		///     Super rect containing all none sighter things casting from the prev cycle.
		/// </summary>
		private CellRect suRectPrev_Combatant = CellRect.Empty;
		/// <summary>
		///     Ticks until update.
		/// </summary>
		private int ticksUntilUpdate;
		/// <summary>
		///     Whether this is the player grid
		/// </summary>
		public bool trackFactions = false;
		private bool wait;

		public SightGrid(SightTracker sightTracker, Settings.SightPerformanceSettings settings, int gridId)
		{
			this.gridId       = gridId;
			this.sightTracker = sightTracker;
			thingsUInt64Map   = new IThingsUInt64Map();
			map               = sightTracker.map;
			this.settings     = settings;
			grid              = new ITSignalGrid(map);
			if (!Extern.active)
			{
				grid_regions = new ITRegionGridLegacy(map);
			}
			else
			{
				grid_regions = new ITRegionGridPrepatched(map, gridId);
			}
			asyncActions          = new AsyncActions(1);
			ticksUntilUpdate      = Rand.Int % this.settings.interval;
			buckets               = new IBuckets<IBucketableThing>(settings.buckets);
			suRect_Combatant      = new CellRect();
			suRect_Combatant.minX = map.cellIndices.mapSizeX;
			suRect_Combatant.maxX = 0;
			suRect_Combatant.minZ = map.cellIndices.mapSizeZ;
			suRect_Combatant.maxZ = 0;
			suRectPrev_Combatant  = CellRect.Empty;
			flooder               = new CellFlooder(map);
		}
		/// <summary>
		///     Tracks the number of factions tracked.
		/// </summary>
		public int FactionNum
		{
			get => numsByFaction.Count;
		}
		/// <summary>
		///     CellRect containing all combatant pawns.
		/// </summary>
		public CellRect SuRect_Combatant
		{
			get => suRectPrev_Combatant;
		}
		/// <summary>
		///     Avg position of combatant pawns.
		/// </summary>
		public IntVec3 SuCentroid
		{
			get => suCentroidPrev;
		}
		/// <summary>
		///     The map's wallgrid.
		/// </summary>
		public WallGrid Walls
		{
			get => _walls != null ? _walls : _walls = sightTracker.map.GetComp_Fast<WallGrid>();
		}

		public void FinalizeInit()
		{
			// initialize the region grid.
			for (int i = 0; i < map.cellIndices.NumGridCells; i++)
			{
				grid_regions.SetRegionAt(i, map.regionGrid.regionGrid[i]);
			}
			// start async actions.
			asyncActions.Start();
		}

		public virtual void SightGridOptionalUpdate(bool gamePaused, bool performanceOkay)
		{
			if (gamePaused || performanceOkay)
			{
				int limit        = gamePaused ? 32 : 8;
				int numGridCells = map.cellIndices.NumGridCells;
				for (int i = 0; i < limit; i++)
				{
					Region region = map.regionGrid.regionGrid[regionUpdateIndex];
					if (region?.valid ?? false)
					{
						grid_regions.SetRegionAt(regionUpdateIndex, region);
						regionUpdateIndex++;
						if (regionUpdateIndex >= numGridCells)
						{
							regionUpdateIndex = 0;
						}
					}
				}
			}
		}

		public void SightGridUpdate()
		{
			asyncActions.ExecuteMainThreadActions();
			if (ticksUntilUpdate-- > 0 || wait)
			{
				return;
			}
			tmpInvalidRecords.Clear();
			tmpInconsistentRecords.Clear();
			List<IBucketableThing> bucket = buckets.Current;
			for (int i = 0; i < bucket.Count; i++)
			{
				try
				{
					IBucketableThing item = bucket[i];
					if (!Valid(item))
					{
						tmpInvalidRecords.Add(item);
						continue;
					}
					if (!Consistent(item))
					{
						tmpInconsistentRecords.Add(item);
						continue;
					}
					TryCastSight(item);
				}
				catch (Exception er)
				{
					er.ShowExceptionGui();
				}
			}
			if (tmpInvalidRecords.Count != 0)
			{
				for (int i = 0; i < tmpInvalidRecords.Count; i++)
				{
					TryDeRegister(tmpInvalidRecords[i].thing);
				}
				tmpInvalidRecords.Clear();
			}
			if (tmpInconsistentRecords.Count != 0)
			{
				for (int i = 0; i < tmpInconsistentRecords.Count; i++)
				{
					TryDeRegister(tmpInconsistentRecords[i].thing);
					sightTracker.Register(tmpInconsistentRecords[i].thing);
				}
				tmpInconsistentRecords.Clear();
			}
			ticksUntilUpdate = settings.interval + Mathf.CeilToInt(settings.interval * (1.0f - Finder.P50));
			buckets.Next();
			if (buckets.Index == 0)
			{
				wait = true;
				asyncActions.EnqueueOffThreadAction(delegate
				{
					asyncActions.EnqueueMainThreadAction(Continue);
				});
			}
		}

		public void GetThings(ulong flags, IntVec3 cell, List<Thing> buffer, bool validateLoS = false)
		{
			_getThings.Clear();
			thingsUInt64Map.GetThings(flags, _getThings);
			if (_getThings.Count > 0)
			{
				for (int i = 0; i < _getThings.Count; i++)
				{
					Thing thing = _getThings[i];
					if (thing != null && thing.Spawned)
					{
						if (thing.Position.DistanceToSquared(cell) < Maths.Sqr(SightUtility.GetSightRadius_Fast(thing)))
						{
							if (validateLoS)
							{
								Verb verb = thing.TryGetAttackVerb();
								if (verb != null)
								{
									if ((verb.IsMeleeAttack && !GenSight.LineOfSight(cell, thing.Position, map, true)) || !verb.CanHitCellFromCellIgnoringRange(thing.Position, cell))
									{
										continue;
									}
								}
							}
							buffer.Add(thing);
						}
					}
				}
				// cleanup.
				_getThings.Clear();
			}
		}

		public virtual void Register(Thing thing)
		{
			buckets.RemoveId(thing.thingIDNumber);
			if (Valid(thing))
			{
				IBucketableThing item;
				thingsUInt64Map.Add(thing);
				buckets.Add(item = new IBucketableThing(this, thing, (thing.thingIDNumber + 19) % settings.buckets));
				if (trackFactions)
				{
					numsByFaction.TryGetValue(thing.Faction, out int num);
					numsByFaction[thing.Faction] = num + 1;
				}
			}
		}

		public virtual void TryDeRegister(Thing thing)
		{
			if (trackFactions)
			{
				IBucketableThing bucketable = buckets.GetById(thing.thingIDNumber);
				thingsUInt64Map.Remove(thing);
				if (bucketable != null && numsByFaction.TryGetValue(bucketable.registeredFaction, out int num))
				{
					if (num > 1)
					{
						numsByFaction[bucketable.registeredFaction] = num - 1;
					}
					else
					{
						numsByFaction.Remove(bucketable.registeredFaction);
					}
				}
			}
			buckets.RemoveId(thing.thingIDNumber);
		}

		public virtual void Destroy()
		{
			try
			{
				buckets.Release();
				asyncActions.Kill();
			}
			catch (Exception er)
			{
				Log.Error($"CAI: SightGridManager Notify_MapRemoved failed to stop thread with {er}");
			}
		}

		private void Continue()
		{
			suRectPrev_Combatant      =  suRect_Combatant;
			suRect_Combatant.minX     =  map.cellIndices.mapSizeX;
			suRect_Combatant.maxX     =  0;
			suRect_Combatant.minZ     =  map.cellIndices.mapSizeZ;
			suRect_Combatant.maxZ     =  0;
			suRectPrev_Combatant.minX -= 5;
			suRectPrev_Combatant.minZ -= 5;
			suRectPrev_Combatant.maxX += 5;
			suRectPrev_Combatant.maxZ += 5;
			suCentroidPrev            =  suCentroid;
			gridFog?.NextCycle();
			grid.NextCycle();
			grid_regions.NextCycle();
			wait             = false;
			suCentroidPrev   = suCentroid;
			suCentroidPrev.x = Mathf.CeilToInt(suCentroidPrev.x / (ops + 1e-3f));
			suCentroidPrev.z = Mathf.CeilToInt(suCentroidPrev.z / (ops + 1e-3f));
			suCentroid       = IntVec3.Zero;
			ops              = 0;
		}

		private bool Consistent(IBucketableThing item)
		{
			if (item.registeredFaction != item.thing.Faction)
			{
				return false;
			}
			return true;
		}

		private bool Valid(Thing thing)
		{
			if (thing == null)
			{
				return false;
			}
			if (thing.Destroyed || !thing.Spawned)
			{
				return false;
			}
			return thing is Pawn pawn && !pawn.Dead || thing is Building_Turret || thing.def.HasComp(typeof(ThingComp_Sighter)) || thing.def.HasComp(typeof(ThingComp_CCTVTop));
		}

		private bool Valid(IBucketableThing item)
		{
			return !item.thing.Destroyed && item.thing.Spawned && (item.Pawn == null || !item.Pawn.Dead);
		}

		private bool Skip(IBucketableThing item)
		{
			if (!playerAlliance && item.dormant != null)
			{
				return !item.dormant.Awake || item.dormant.WaitingToWakeUp;
			}
			if (item.Pawn != null)
			{
				return !playerAlliance && (GenTicks.TicksGame - item.Pawn.needs?.rest?.lastRestTick <= 30 || item.Pawn.Downed);
			}
			if (item.sighter != null)
			{
				return playerAlliance && !item.sighter.Active;
			}
			if (item.TurretGun != null)
			{
				return playerAlliance && (!item.TurretGun.Active || item.TurretGun.IsMannable && !(item.TurretGun.mannableComp?.MannedNow ?? false));
			}
			if (Mod_CE.active && item.thing is Building_Turret turret)
			{
				return !Mod_CE.IsTurretActiveCE(turret);
			}
			return false;
		}

		private ulong GetFlags(IBucketableThing item)
		{
			return item.thing.GetThingFlags();
		}

		private bool TryCastSight(IBucketableThing item)
		{
			if (grid.CycleNum == item.lastCycle || Skip(item))
			{
				return false;
			}
			if (!item.cachedSightRadius.IsValid)
			{
				item.cachedSightRadius = SightUtility.GetSightRadius(item.thing);
			}
			if (item.cachedSightRadius.sight == 0)
			{
				return false;
			}
			if (!!item.cachedDamage.IsValid)
			{
				item.cachedDamage = DamageUtility.GetDamageReport(item.thing);
			}
			int     ticks  = GenTicks.TicksGame;
			IntVec3 origin = item.thing.Position;
			IntVec3 pos    = GetShiftedPosition(item.thing, (int)Maths.Min(settings.interval * settings.buckets * Find.TickManager.TickRateMultiplier, 90), item.path);
			if (!pos.InBounds(map))
			{
				Log.Error($"ISMA: SighGridUpdater {item.thing} position is outside the map's bounds!");
				return false;
			}
			IntVec3 flagPos = pos;
			if (item.Pawn != null)
			{
				flagPos = GetShiftedPosition(item.Pawn, (int)Maths.Min(60 * Find.TickManager.TickRateMultiplier, 80), null);
			}
			SightTracker.SightReader reader = item.ai?.sightReader ?? null;
			bool                     scanForEnemies;
			if (scanForEnemies = Finder.Settings.React_Enabled && item.sighter == null && reader != null && item.ai != null && !item.ai.ReactedRecently(45) && ticks - item.lastScannedForEnemies >= (!Finder.Performance.TpsCriticallyLow ? 10 : 15))
			{
				if (!item.registeredFaction.IsPlayerSafe() || (item.ai?.forcedTarget.IsValid ?? false))
				{
					if (item.dormant != null && !item.dormant.Awake)
					{
						scanForEnemies = false;
					}
					else if (item.Pawn != null && item.Pawn.mindState?.duty?.def == DutyDefOf.SleepForever)
					{
						scanForEnemies = false;
					}
				}
			}
			bool defenseMode = false;
			if (scanForEnemies)
			{
				if (item.Pawn != null && (item.Pawn.mindState.duty.Is(DutyDefOf.Defend) || item.Pawn.mindState.duty.Is(CombatAI_DutyDefOf.CombatAI_AssaultPoint)) && item.Pawn.CurJob.Is(JobDefOf.Wait_Wander))
				{
					defenseMode = true;
				}
				item.lastScannedForEnemies = ticks;
				try
				{
					item.ai.OnScanStarted();
				}
				catch (Exception er)
				{
					er.ShowExceptionGui();
				}
				item.spottings.Clear();
			}
			if (scanForEnemies || item.sighter == null && item.CctvTop == null)
			{
				suRect_Combatant.minX =  Maths.Min(suRect_Combatant.minX, pos.x);
				suRect_Combatant.maxX =  Maths.Max(suRect_Combatant.maxX, pos.x);
				suRect_Combatant.minZ =  Maths.Min(suRect_Combatant.minZ, pos.z);
				suRect_Combatant.maxZ =  Maths.Max(suRect_Combatant.maxZ, pos.z);
				ops                   += 1;
				suCentroid            += pos;
			}
			MetaCombatAttribute availability   = 0;
			bool                engagedInMelee = false;
			if (item.Pawn != null)
			{
				if ((engagedInMelee = item.Pawn.mindState.MeleeThreatStillThreat) || item.Pawn.stances?.curStance is Stance_Warmup)
				{
					availability = MetaCombatAttribute.Occupied;
				}
				else
				{
					availability = MetaCombatAttribute.Free;
				}
			}
			Vector3 drawPos = item.thing.DrawPos;
			scanForEnemies &= !engagedInMelee;
			ISightRadius sightRadius = item.cachedSightRadius;
			Action action = () =>
			{
				float r_fade     = sightRadius.fog * Finder.Settings.FogOfWar_RangeFadeMultiplier;
				float d_fade     = sightRadius.fog - r_fade;
				float rSqr_sight = Maths.Sqr(sightRadius.sight);
				float rSqr_scan  = Maths.Sqr(sightRadius.scan);
				float rSqr_fog   = Maths.Sqr(sightRadius.fog);
				float rSqr_fade  = Maths.Sqr(r_fade);
				if (playerAlliance && sightRadius.fog > 0)
				{
					gridFog.Next();
					gridFog.Set(origin, 1.0f);
					for (int i = 0; i < item.path.Count; i++)
					{
						IntVec3 cell = item.path[i];
						float   d3   = Maths.Sqr(drawPos.x - cell.x) + Maths.Sqr(drawPos.z - cell.z);
						float   val;
						if (d3 < rSqr_fade)
						{
							val = 1f;
						}
						else
						{
							val = 1f - Mathf.Clamp01((Maths.Sqrt_Fast(d3, 5) - r_fade) / d_fade);
						}
						gridFog.Set(cell, val);
					}
				}
				MetaCombatAttribute attr = item.cachedDamage.attributes | availability;
				grid.Next(pos, GetFlags(item), item.cachedDamage.adjustedSharp, item.cachedDamage.adjustedBlunt, attr);
				grid_regions.Next();
				Action<IntVec3, int, int, float> setAction = (cell, carry, dist, coverRating) =>
				{
					float d2         = pos.DistanceToSquared(cell);
					float visibility = 0f;
					if (!engagedInMelee)
					{
						if (d2 < rSqr_sight)
						{
							visibility = Maths.Max(1f - coverRating, 0.20f);
							if (visibility > 0f)
							{
								grid.Set(cell, visibility, new Vector2(cell.x - pos.x, cell.z - pos.z));
								grid_regions.Set(cell);
							}
						}
						else if(d2 < 360)
						{
							grid.Set(cell, 0, new Vector2(cell.x - pos.x, cell.z - pos.z));
							grid_regions.Set(cell);
						}
					}
					if (playerAlliance && d2 < rSqr_fog)
					{
						float d3 = Maths.Sqr(drawPos.x - cell.x) + Maths.Sqr(drawPos.z - cell.z);
						float val;
						if (d3 < rSqr_fade)
						{
							val = 1f;
						}
						else
						{
							val = 1f - Mathf.Clamp01((Maths.Sqrt_Fast(d3, 5) - r_fade) / d_fade);
						}
						gridFog?.Set(cell, val);
					}
					if (scanForEnemies && d2 < rSqr_scan)
					{
						ulong flag = reader.GetStaticEnemyFlags(cell);
						if (flag != 0)
						{
							ISpotRecord spotting = new ISpotRecord();
							spotting.cell  = cell;
							spotting.flag  = flag;
							spotting.state = (int)AIEnvAgentState.visible;
							spotting.score = visibility;
							item.spottings.Add(spotting);
						}
					}
				};
				if (item.CctvTop == null)
				{
					ShadowCastingUtility.CastWeighted(map, pos, setAction, Maths.Max(sightRadius.scan, sightRadius.fog, sightRadius.sight), settings.carryLimit, buffer);
				}
				else
				{
					ShadowCastingUtility.CastWeighted(map, pos, item.CctvTop.LookDirection, setAction, Maths.Max(sightRadius.scan, sightRadius.fog, sightRadius.sight), item.CctvTop.BaseWidth, settings.carryLimit, buffer);
				}
				grid.curRoot = IntVec3.Invalid;
				flooder.Flood(origin, node =>
				{
					if (!grid.IsSet(node.cell))
					{
						grid.Set(node.cell, 0.198f, new Vector2(node.cell.x - node.parent.x, node.cell.z - node.parent.z));
						grid_regions.Set(node.cell);
					}
					if (scanForEnemies)
					{
						ulong flag = reader.GetStaticEnemyFlags(node.cell) | reader.GetStaticFriendlyFlags(node.cell);
						if (flag != 0)
						{
							ISpotRecord spotting = new ISpotRecord();
							spotting.cell  = node.cell;
							spotting.flag  = flag;
							spotting.state = (int)AIEnvAgentState.nearby;
							spotting.score = node.distAbs;
							item.spottings.Add(spotting);
						}
					}
				}, maxDist: defenseMode ? 32 : 12, maxCellNum: defenseMode ? 325 : 225, passThroughDoors: true);
				grid.Set(origin, 1.0f, new Vector2(origin.x - pos.x, origin.z - pos.z));
				grid.Set(pos, 1.0f, new Vector2(origin.x - pos.x, origin.z - pos.z));
				grid.Next(pos, GetFlags(item), 0, 0, item.cachedDamage.attributes);
				grid.Set(flagPos, item.Pawn == null || !item.Pawn.Downed ? GetFlags(item) : 0);
				if (scanForEnemies)
				{
					if (item.ai.data.NumEnemies > 0 || item.ai.data.NumAllies > 0 || item.spottings.Count > 0 || Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight)
					{
						// on the main thread check for enemies on or near this cell.
						asyncActions.EnqueueMainThreadAction(delegate
						{
							if (!item.thing.Destroyed && item.thing.Spawned)
							{
								for (int i = 0; i < item.spottings.Count; i++)
								{
									ISpotRecord record = item.spottings[i];
									thingBuffer1.Clear();
									sightTracker.factionedUInt64Map.GetThings(record.flag, thingBuffer1);
									for (int j = 0; j < thingBuffer1.Count; j++)
									{
										Thing agent = thingBuffer1[j];
										if (agent != item.thing
										    && agent.Spawned
										    && (agent.Position.DistanceToSquared(record.cell) < 225 || agent is Pawn enemyPawn && PawnPathUtility.GetMovingShiftedPosition(enemyPawn, 70).DistanceToSquared(record.cell) < 255)
										    && !agent.IsDormant())
										{
											if (agent.HostileTo(item.thing))
											{
												item.ai.Notify_Enemy(new AIEnvAgentInfo(agent, (AIEnvAgentState)record.state));
											}
											else
											{
												item.ai.Notify_Ally(new AIEnvAgentInfo(agent, (AIEnvAgentState)record.state));
											}
										}
									}
								}
								//
								// notify the pawn so they can start processing targets.   
								try
								{
									item.ai.OnScanFinished();
								}
								catch (Exception er)
								{
									er.ShowExceptionGui();
								}
								item.spottings.Clear();
							}
						});
					}
				}
			};
			asyncActions.EnqueueOffThreadAction(action);
			item.lastCycle = grid.CycleNum;
			return true;
		}

		private IntVec3 GetShiftedPosition(Thing thing, int ticksAhead, List<IntVec3> subPath)
		{
			if (thing is Pawn pawn)
			{
				WallGrid walls = Walls;
				if (subPath != null)
				{
					subPath.Clear();
				}
				if (walls != null && PawnPathUtility.TryGetCellIndexAhead(pawn, ticksAhead, out int index))
				{
					PawnPath path = pawn.pather.curPath;
					IntVec3  cell = pawn.Position;
					IntVec3  temp;
					for (int i = 0; i < index; i++)
					{
						if (!walls.CanBeSeenOver(temp = path.Peek(i)))
						{
							return cell;
						}
						cell = temp;
						if (subPath != null)
						{
							subPath.Add(cell);
						}
					}
					return cell;
				}
				return thing.Position;
			}
			return thing.Position;
		}

		public struct ISightRadius
		{
			/// <summary>
			///     Scan radius. Used while scanning for enemies.
			/// </summary>
			public int scan;
			/// <summary>
			///     Sight radius. Determine the radius of the thing's influence map.
			/// </summary>
			public int sight;
			/// <summary>
			///     Fog radius. Determine how far this thing will reveal fog of war.
			/// </summary>
			public int fog;
			/// <summary>
			///     Creation tick timestamp.
			/// </summary>
			public int createdAt;
			/// <summary>
			///     Whether this is a valid report.
			/// </summary>
			public bool IsValid
			{
				get => createdAt != 0 && GenTicks.TicksGame - createdAt < 600;
			}
		}

		private struct ISpotRecord
		{
			/// <summary>
			///     Spotted flag.
			/// </summary>
			public ulong flag;
			/// <summary>
			///     Cell at which the spotting occured.
			/// </summary>
			public IntVec3 cell;
			/// <summary>
			///     state
			/// </summary>
			public int state;
			/// <summary>
			///     Cell visibility.
			/// </summary>
			public float score;
		}

		private class IBucketableThing : IBucketable
		{
			/// <summary>
			///     Dormant comp.
			/// </summary>
			public readonly ThingComp_CombatAI ai;
			/// <summary>
			///     Sighting component.
			/// </summary>
			public readonly ThingComp_CCTVTop CctvTop;
			/// <summary>
			///     Dormant comp.
			/// </summary>
			public readonly CompCanBeDormant dormant;
			/// <summary>
			///     Current sight grid.
			/// </summary>
			public readonly SightGrid grid;
			/// <summary>
			///     Pawn pawn
			/// </summary>
			public readonly List<IntVec3> path = new List<IntVec3>(16);
			/// <summary>
			///     Thing's faction on IBucketableThing instance creation.
			/// </summary>
			public readonly Faction registeredFaction;
			/// <summary>
			///     Sighting component.
			/// </summary>
			public readonly ThingComp_Sighter sighter;
			/// <summary>
			///     Contains spotting records that are to be processed on the main thread once the scan is finished.
			/// </summary>
			public readonly List<ISpotRecord> spottings = new List<ISpotRecord>(16);
			/// <summary>
			///     Thing.
			/// </summary>
			public readonly Thing thing;
			/// <summary>
			///     Thing potential damage report.
			/// </summary>
			public DamageReport cachedDamage;
			/// <summary>
			///     Cached sight radius report.
			/// </summary>
			public ISightRadius cachedSightRadius;
			/// <summary>
			///     Last cycle.
			/// </summary>
			public int lastCycle;
			/// <summary>
			///     Last tick this pawn scanned for enemies
			/// </summary>
			public int lastScannedForEnemies;

			public IBucketableThing(SightGrid grid, Thing thing, int bucketIndex)
			{
				this.grid         = grid;
				this.thing        = thing;
				dormant           = thing.GetComp_Fast<CompCanBeDormant>();
				ai                = thing.GetComp_Fast<ThingComp_CombatAI>();
				sighter           = thing.GetComp_Fast<ThingComp_Sighter>();
				registeredFaction = thing.Faction;
				BucketIndex       = bucketIndex;
				cachedDamage      = DamageUtility.GetDamageReport(thing);
				cachedSightRadius = SightUtility.GetSightRadius(thing);
				CctvTop           = thing.GetComp_Fast<ThingComp_CCTVTop>();
			}
			/// <summary>
			///     Thing.
			/// </summary>
			public Pawn Pawn
			{
				get => thing as Pawn;
			}
			/// <summary>
			///     Thing.
			/// </summary>
			public Building_TurretGun TurretGun
			{
				get => thing as Building_TurretGun;
			}
			/// <summary>
			///     Bucket index.
			/// </summary>
			public int BucketIndex
			{
				get;
			}
			/// <summary>
			///     Thing id number.
			/// </summary>
			public int UniqueIdNumber
			{
				get => thing.thingIDNumber;
			}
		}
	}
}
