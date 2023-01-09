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
		/// <summary>
		///     Sight grid contains all sight data.
		/// </summary>
		public readonly ITSignalGrid grid;
		/// <summary>
		///		Region grid contains sight data for regions.
		/// </summary>
		public readonly ITRegionGrid grid_regions;

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
		private readonly List<Thing>            thingBuffer1           = new List<Thing>(256);
		private readonly List<Thing>            thingBuffer2           = new List<Thing>(256);
		private readonly List<IBucketableThing> tmpDeRegisterList      = new List<IBucketableThing>(64);
		private readonly List<IBucketableThing> tmpInconsistentRecords = new List<IBucketableThing>(64);
		private readonly List<IBucketableThing> tmpInvalidRecords      = new List<IBucketableThing>(64);

		private int      ops;
		private WallGrid _walls;
		/// <summary>
		///     Fog of war grid. Can be null.
		/// </summary>
		public ITFloatGrid gridFog;
		/// <summary>
		///     Whether this is the player grid
		/// </summary>
		public bool playerAlliance = false;
		/// <summary>
		/// Super rect containing all none sighter things casting.
		/// </summary>
		private CellRect suRect_Combatant = CellRect.Empty;
		/// <summary>
		/// Super rect containing all none sighter things casting from the prev cycle.
		/// </summary>
		private CellRect suRectPrev_Combatant = CellRect.Empty;

		private IntVec3 suCentroid;
		private IntVec3 suCentroidPrev;
		/// <summary>
		/// Ticks until update.
		/// </summary>
		private int ticksUntilUpdate;
		/// <summary>
		///     Whether this is the player grid
		/// </summary>
		public bool trackFactions = false;
		private bool wait;

		public SightGrid(SightTracker sightTracker, Settings.SightPerformanceSettings settings)
		{
			this.sightTracker     = sightTracker;
			map                   = sightTracker.map;
			this.settings         = settings;
			grid                  = new ITSignalGrid(map);
			grid_regions          = new ITRegionGrid(map);
			asyncActions          = new AsyncActions(1);
			ticksUntilUpdate      = Rand.Int % this.settings.interval;
			buckets               = new IBuckets<IBucketableThing>(settings.buckets);
			suRect_Combatant      = new CellRect();
			suRect_Combatant.minX = map.cellIndices.mapSizeX;
			suRect_Combatant.maxX = 0;
			suRect_Combatant.minZ = map.cellIndices.mapSizeZ;
			suRect_Combatant.maxZ = 0;
			suRectPrev_Combatant  = CellRect.Empty;
		}
		/// <summary>
		///     Tracks the number of factions tracked.
		/// </summary>
		public int FactionNum
		{
			get => numsByFaction.Count;
		}
		/// <summary>
		/// CellRect containing all combatant pawns.
		/// </summary>
		public CellRect SuRect_Combatant
		{
			get => suRectPrev_Combatant;
		}
		/// <summary>
		/// Avg position of combatant pawns.
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
			asyncActions.Start();
		}

		public virtual void SightGridTick()
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

		public virtual void Register(Thing thing)
		{
			buckets.RemoveId(thing.thingIDNumber);
			if (Valid(thing))
			{
				IBucketableThing item;
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
				if (bucketable != null && numsByFaction.TryGetValue(bucketable.faction, out int num))
				{
					if (num > 1)
					{
						numsByFaction[bucketable.faction] = num - 1;
					}
					else
					{
						numsByFaction.Remove(bucketable.faction);
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
			if (item.faction != item.thing.Faction)
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
			return !item.thing.Destroyed && item.thing.Spawned && (item.pawn == null || !item.pawn.Dead);
		}

		private bool Skip(IBucketableThing item)
		{
			if (!playerAlliance && item.dormant != null)
			{
				return !item.dormant.Awake || item.dormant.WaitingToWakeUp;
			}
			if (item.pawn != null)
			{
				return !playerAlliance && (GenTicks.TicksGame - item.pawn.needs?.rest?.lastRestTick <= 30 || item.pawn.Downed);
			}
			if (item.sighter != null)
			{
				return playerAlliance && !item.sighter.Active;
			}
			if (item.turretGun != null)
			{
				return playerAlliance && (!item.turretGun.Active || item.turretGun.IsMannable && !(item.turretGun.mannableComp?.MannedNow ?? false));
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
			IntVec3 pos    = GetShiftedPosition(item.thing, 30, item.path);
			if (!pos.InBounds(map))
			{
				Log.Error($"ISMA: SighGridUpdater {item.thing} position is outside the map's bounds!");
				return false;
			}
			IntVec3 flagPos = pos;
			if (item.pawn != null)
			{
				flagPos = GetShiftedPosition(item.pawn, 60, null);
			}
			SightTracker.SightReader reader = item.ai?.sightReader ?? null;
			bool                     scanForEnemies;
			if (scanForEnemies = Finder.Settings.React_Enabled && !item.isPlayer && item.sighter == null && reader != null && item.ai != null && !item.ai.ReactedRecently(45) && ticks - item.lastScannedForEnemies >= (!Finder.Performance.TpsCriticallyLow ? 10 : 15))
			{
				if (item.dormant != null && !item.dormant.Awake)
				{
					scanForEnemies = false;
				}
				else if (item.pawn != null && item.pawn.mindState?.duty?.def == DutyDefOf.SleepForever)
				{
					scanForEnemies = false;
				}
			}			
			if (scanForEnemies)
			{
				item.lastScannedForEnemies = ticks;
				item.ai.OnScanStarted();
				item.spottings.Clear();
			}
			if (scanForEnemies || (item.sighter == null && item.CctvTop == null))
			{
				suRect_Combatant.minX = Maths.Min(suRect_Combatant.minX, pos.x);
				suRect_Combatant.maxX = Maths.Max(suRect_Combatant.maxX, pos.x);
				suRect_Combatant.minZ = Maths.Min(suRect_Combatant.minZ, pos.z);
				suRect_Combatant.maxZ = Maths.Max(suRect_Combatant.maxZ, pos.z);
				ops += 1;
				suCentroid += pos;
			}
			ISightRadius sightRadius = item.cachedSightRadius;
			Action action = () =>
			{
				if (playerAlliance)
				{
					gridFog.Next();
					gridFog.Set(origin, 1.0f);
					for (int i = 0; i < item.path.Count; i++)
					{
						gridFog.Set(item.path[i], 1.0f);
					}
				}
				grid.Next(item.cachedDamage.adjustedSharp, item.cachedDamage.adjustedBlunt, item.cachedDamage.attributes);
				grid_regions.Next( GetFlags(item), item.cachedDamage.adjustedSharp, item.cachedDamage.adjustedBlunt, item.cachedDamage.attributes);
				float r_fade     = sightRadius.fog * Finder.Settings.FogOfWar_RangeFadeMultiplier;
				float d_fade     = sightRadius.fog - r_fade;
				float rSqr_sight = Maths.Sqr(sightRadius.sight);
				float rSqr_scan  = Maths.Sqr(sightRadius.scan);
				float rSqr_fog   = Maths.Sqr(sightRadius.fog);
				float rSqr_fade  = Maths.Sqr(r_fade);
				Action<IntVec3, int, int, float> setAction = (cell, carry, dist, coverRating) =>
				{
					float d2         = pos.DistanceToSquared(cell);
					float visibility = 0f;
					if (d2 < rSqr_sight)
					{
						visibility = (float)(sightRadius.sight - dist) / sightRadius.sight * (1 - coverRating);
						if (visibility > 0f)
						{
							grid.Set(cell, visibility, new Vector2(cell.x - pos.x, cell.z - pos.z));
							grid_regions.Set(cell);
						}
					}
					if (playerAlliance && d2 < rSqr_fog)
					{
						float val;
						if (d2 < rSqr_fade)
						{
							val = 1f;
						}
						else
						{
							val = 1f - Mathf.Clamp01((Maths.Sqrt_Fast(d2, 5) - r_fade) / d_fade);
						}
						gridFog?.Set(cell, val);
					}
					if (scanForEnemies && d2 < rSqr_scan)
					{
						ulong flag = reader.GetEnemyFlags(cell);
						if (flag != 0)
						{
							ISpotRecord spotting = new ISpotRecord();
							spotting.cell       = cell;
							spotting.flag       = flag;
							spotting.visibility = visibility;
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
				grid.Set(origin, 1.0f, new Vector2(origin.x - pos.x, origin.z - pos.z));
				grid.Set(pos, 1.0f, new Vector2(origin.x - pos.x, origin.z - pos.z));
				grid.Next(0, 0, item.cachedDamage.attributes);
				grid.Set(flagPos, item.pawn == null || !item.pawn.Downed ? GetFlags(item) : 0);
				if (scanForEnemies)
				{
					if (item.spottings.Count > 0 || Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight)
					{
						// on the main thread check for enemies on or near this cell.
						asyncActions.EnqueueMainThreadAction(delegate
						{
							if (!item.thing.Destroyed && item.thing.Spawned)
							{
								for (int i = 0; i < item.spottings.Count; i++)
								{
									ISpotRecord record = item.spottings[i];
									Thing       enemy;
									thingBuffer1.Clear();
									sightTracker.factionedUInt64Map.GetThings(record.flag, thingBuffer1);
									for (int j = 0; j < thingBuffer1.Count; j++)
									{
										enemy = thingBuffer1[j];
										if (enemy.Spawned
										    && !enemy.Destroyed
										    && (enemy.Position.DistanceToSquared(record.cell) < 225 || enemy is Pawn enemyPawn && PawnPathUtility.GetMovingShiftedPosition(enemyPawn, 70).DistanceToSquared(record.cell) < 255)
										    && enemy.HostileTo(item.thing)
										    && !enemy.IsDormant())
										{
											item.ai.Notify_EnemyVisible(enemy);
										}
									}
								}
								// notify the pawn so they can start processing targets.                                
								item.ai.OnScanFinished();
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
			///     Cell visibility.
			/// </summary>
			public float visibility;
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
			///     Thing's faction on IBucketableThing instance creation.
			/// </summary>
			public readonly Faction faction;
			/// <summary>
			///     Current sight grid.
			/// </summary>
			public readonly SightGrid grid;
			/// <summary>
			///     Thing.
			/// </summary>
			public readonly bool isPlayer;
			/// <summary>
			///     Pawn pawn
			/// </summary>
			public readonly List<IntVec3> path = new List<IntVec3>(16);
			/// <summary>
			///     Thing.
			/// </summary>
			public readonly Pawn pawn;
			/// <summary>
			///     Sighting component.
			/// </summary>
			public readonly ThingComp_Sighter sighter;
			/// <summary>
			///     Contains spotting records that are to be processed on the main thread once the scan is finished.
			/// </summary>
			public readonly List<ISpotRecord> spottings = new List<ISpotRecord>(64);
			/// <summary>
			///     Thing.
			/// </summary>
			public readonly Thing thing;
			/// <summary>
			///     Thing.
			/// </summary>
			public readonly Building_TurretGun turretGun;
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
				pawn              = thing as Pawn;
				turretGun         = thing as Building_TurretGun;
				isPlayer          = thing.Faction.IsPlayerSafe();
				dormant           = thing.GetComp_Fast<CompCanBeDormant>();
				ai                = thing.GetComp_Fast<ThingComp_CombatAI>();
				sighter           = thing.GetComp_Fast<ThingComp_Sighter>();
				faction           = thing.Faction;
				BucketIndex       = bucketIndex;
				cachedDamage      = DamageUtility.GetDamageReport(thing);
				cachedSightRadius = SightUtility.GetSightRadius(thing);
				CctvTop           = thing.GetComp_Fast<ThingComp_CCTVTop>();
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
