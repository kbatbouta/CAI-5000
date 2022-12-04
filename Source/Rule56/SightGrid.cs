using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using CombatAI.Comps;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatAI {
	public class SightGrid
    {		
		private readonly List<Vector3> buffer = new List<Vector3>(1024);
        private readonly List<Thing> thingBuffer1 = new List<Thing>(256);
		private readonly List<Thing> thingBuffer2 = new List<Thing>(256);        

		private const int COVERCARRYLIMIT = 6;        

        private struct ISpotRecord
        {
            /// <summary>
            /// Spotted flag.
            /// </summary>
            public UInt64 flag;
            /// <summary>
            /// Cell at which the spotting occured.
            /// </summary>
            public IntVec3 cell;
            /// <summary>
            /// Cell visibility.
            /// </summary>
            public float visibility;
        }

        private class IBucketableThing : IBucketable
        {
            private int bucketIndex;

            /// <summary>
            /// Thing.
            /// </summary>
            public readonly Thing thing;
			/// <summary>
			/// Thing.
			/// </summary>
			public readonly Pawn pawn;
			/// <summary>
			/// Thing.
			/// </summary>
			public readonly Building_TurretGun turretGun;            
			/// <summary>
			/// Thing.
			/// </summary>
			public readonly bool isPlayer;
			/// <summary>
			/// Thing's faction on IBucketableThing instance creation.
			/// </summary>
			public readonly Faction faction;
            /// <summary>
            /// Sighting component.
            /// </summary>
            public readonly ThingComp_Sighter sighter;
            /// <summary>
            /// Dormant comp.
            /// </summary>
            public readonly CompCanBeDormant dormant;
			/// <summary>
			/// Dormant comp.
			/// </summary>
			public readonly ThingComp_CombatAI ai;
			/// <summary>
			/// Last cycle.
			/// </summary>
			public int lastCycle;
            /// <summary>
            /// Thing potential damage report.
            /// </summary>
            public DamageReport damage;
            /// <summary>
            /// Pawn pawn
            /// </summary>
            public readonly List<IntVec3> path = new List<IntVec3>(16);
            /// <summary>
            /// Contains spotting records that are to be processed on the main thread once the scan is finished.
            /// </summary>
            public readonly List<ISpotRecord> spottings = new List<ISpotRecord>(64);
            /// <summary>
            /// Last tick this pawn scanned for enemies
            /// </summary>
            public int lastScannedForEnemies;
            /// <summary>
            /// Bucket index.
            /// </summary>
            public int BucketIndex =>
                bucketIndex;
            /// <summary>
            /// Thing id number.
            /// </summary>
            public int UniqueIdNumber =>
                thing.thingIDNumber;

            public IBucketableThing(Thing thing, int bucketIndex)
            {
				this.thing = thing;
				this.pawn = thing as Pawn;
                this.turretGun = thing as Building_TurretGun;
				this.isPlayer = thing.Faction.IsPlayerSafe();
                this.damage = DamageUtility.GetDamageReport(thing);
				this.dormant = thing.GetComp_Fast<CompCanBeDormant>();
                this.ai = thing.GetComp_Fast<ThingComp_CombatAI>();
                this.sighter = thing.GetComp_Fast<ThingComp_Sighter>();                
                this.faction = thing.Faction;
                this.bucketIndex = bucketIndex;                
            }
        }
        
        private WallGrid _walls;
        private int ticksUntilUpdate;
        private bool wait = false;        
		private AsyncActions asyncActions;
        private IBuckets<IBucketableThing> buckets;
		private readonly Dictionary<Faction, int> numsByFaction = new Dictionary<Faction, int>();
		private readonly List<IBucketableThing> tmpDeRegisterList = new List<IBucketableThing>(64);
        private readonly List<IBucketableThing> tmpInvalidRecords = new List<IBucketableThing>(64);
        private readonly List<IBucketableThing> tmpInconsistentRecords = new List<IBucketableThing>(64);

        /// <summary>
        /// Parent map.
        /// </summary>
        public readonly Map map;
        /// <summary>
        /// Sight grid contains all sight data.
        /// </summary>
        public readonly ITSignalGrid grid;                
        /// <summary>
        /// Performance settings.
        /// </summary>
        public readonly Settings.SightPerformanceSettings settings;
        /// <summary>
        /// Parent map sight tracker.
        /// </summary>
        public readonly SightTracker sightTracker;
        /// <summary>
        /// Whether this is the player grid
        /// </summary>
        public bool playerAlliance = false;
		/// <summary>
		/// Whether this is the player grid
		/// </summary>
		public bool trackFactions = false;
        /// <summary>
        /// Tracks the number of factions tracked.
        /// </summary>
        public int FactionNum
        {
            get
            {
                return numsByFaction.Count;
            }
        }
		/// <summary>
		/// The map's wallgrid.
		/// </summary>                
		public WallGrid Walls
        {
            get
            {
                return _walls != null ? _walls : _walls = sightTracker.map.GetComp_Fast<WallGrid>();
			}
        }

        public SightGrid(SightTracker sightTracker, Settings.SightPerformanceSettings settings)
        {            
            this.sightTracker = sightTracker;
            this.map = sightTracker.map;
            this.settings = settings;                    
            this.grid = new ITSignalGrid(map);            
            this.asyncActions = new AsyncActions(1);            
            this.ticksUntilUpdate = Rand.Int % this.settings.interval;
            this.buckets = new IBuckets<IBucketableThing>(settings.buckets);
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
                if(!Valid(item))
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
            if(tmpInvalidRecords.Count != 0)
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
            ticksUntilUpdate = (int)settings.interval + Mathf.CeilToInt(settings.interval * (1.0f - Finder.P50));
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
				buckets.Add(new IBucketableThing(thing, (thing.thingIDNumber + 19) % settings.buckets));                
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
                    if(num > 1)
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
            catch(Exception er)
            {
                Log.Error($"CAI: SightGridManager Notify_MapRemoved failed to stop thread with {er}");
            }
        }

        private void Continue()
        {
			grid.NextCycle();
			wait = false;
		}

        private bool Consistent(IBucketableThing item)
        {
            if(item.faction != item.thing.Faction)
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
			return (thing is Pawn pawn && !pawn.Dead) || thing is Building_Turret || thing.def.HasComp(typeof(ThingComp_Sighter));
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
                return !playerAlliance && ((GenTicks.TicksGame - item.pawn.needs?.rest?.lastRestTick <= 30) || item.pawn.Downed);
            }
            if (item.sighter != null)
            {
                return playerAlliance && !item.sighter.Active;
            }
            if (item.turretGun != null)
            {
                return playerAlliance && (!item.turretGun.Active || (item.turretGun.IsMannable && !(item.turretGun.mannableComp?.MannedNow ?? false)));
            }
            if (Mod_CE.active && item.thing is Building_Turret turret)
            {                
                return !Mod_CE.IsTurretActiveCE(turret);
            }
            return false;
        }

        private UInt64 GetFlags(IBucketableThing item)
        {
            return item.thing.GetThingFlags();
        }

        private bool TryCastSight(IBucketableThing item)
        {
            if (grid.CycleNum == item.lastCycle || Skip(item))
            {
                return false;
            }
            int range;            
            if (item.sighter != null)
            {
				range = SightUtility.GetSightRange(item.sighter, playerAlliance);
			}
            else
            {
				range = SightUtility.GetSightRange(item.thing, playerAlliance);
            }            
            if (range == 0)
            {
				return false;
			}			
			item.damage = DamageUtility.GetDamageReport(item.thing);         
			int ticks = GenTicks.TicksGame;
            IntVec3 origin = item.thing.Position;
            IntVec3 pos = GetShiftedPosition(item.thing, 30, item.path);            
            if (!pos.InBounds(map))
            {
                Log.Error($"ISMA: SighGridUpdater {item.thing} position is outside the map's bounds!");
                return false;
            }
            IntVec3 flagPos = pos;
            if(item.pawn != null)
            {
                flagPos = GetShiftedPosition(item.pawn, 60, null);
			}
            SightTracker.SightReader reader = item.ai?.sightReader ?? null;
            bool scanForEnemies;
            if (scanForEnemies = (!item.isPlayer && item.sighter == null && reader != null && item.ai != null && ticks - item.ai.lastInterupted >= 45 && ticks - item.lastScannedForEnemies >= (!Finder.Performance.TpsCriticallyLow ? 10 : 15)))
            {
                if (item.dormant != null && !item.dormant.Awake)
                {
                    scanForEnemies = false;
                }
                else if(item.pawn != null && item.pawn.mindState?.duty?.def == DutyDefOf.SleepForever)
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
			Action action = () =>
            {
				grid.Next(item.damage.adjustedSharp, item.damage.adjustedBlunt, item.damage.attributes);
				grid.Set(flagPos, (item.pawn == null || !item.pawn.Downed) ? GetFlags(item) : 0);
				grid.Next(item.damage.adjustedSharp, item.damage.adjustedBlunt, item.damage.attributes);
				grid.Set(origin, 1.0f, new Vector2(origin.x - pos.x, origin.z - pos.z));
				for (int i = 0; i < item.path.Count; i++)
                {
                    IntVec3 cell = item.path[i];
					grid.Set(cell, 1.0f, new Vector2(cell.x - pos.x, cell.z - pos.z));
				}
                float r = range * 1.23f;
                float rSqr = range * range;
                float rHafledSqr = rSqr * Finder.Settings.FogOfWar_RangeFadeMultiplier * Finder.Settings.FogOfWar_RangeFadeMultiplier;               
				ShadowCastingUtility.CastWeighted(map, pos, (cell, carry, dist, coverRating) =>
                {
                    float d = pos.DistanceToSquared(cell);                    
                    // only set anything if visibility is ok
                    if (d < rSqr)
                    {
						// NOTE: the carry is the number of cover things between the source and the current cell.                  
						float visibility = (float)(r - dist) / r * (1 - coverRating);
						if (visibility > 0f)
                        {
                            if (playerAlliance)
                            {
                                if (d >= rHafledSqr)
                                {
                                    visibility *= Maths.Min(0.05f, 0.02499f);
                                }
                                else
                                {
                                    visibility = Maths.Max(visibility, 0.02500f);
                                }
                            }
                            if (scanForEnemies)
                            {
                                UInt64 flag = reader.GetEnemyFlags(cell);
                                if (flag != 0)
                                {
                                    ISpotRecord spotting = new ISpotRecord();
                                    spotting.cell = cell;
                                    spotting.flag = flag;
                                    spotting.visibility = visibility;
                                    item.spottings.Add(spotting);
                                }
                            }
                            grid.Set(cell, visibility, new Vector2(cell.x - pos.x, cell.z - pos.z));
                        }
                    }
                }, range, settings.carryLimit, buffer);
                if (scanForEnemies)
                {
                    if (item.spottings.Count > 0 || (Finder.Settings.Debug && Finder.Settings.Debug_ValidateSight))
                    {
                        // on the main thread check for enemies on or near this cell.
                        asyncActions.EnqueueMainThreadAction(delegate
                        {
                            if (!item.thing.Destroyed && item.thing.Spawned)
                            {
                                for (int i = 0; i < item.spottings.Count; i++)
                                {
                                    ISpotRecord record = item.spottings[i];
                                    Thing enemy;
                                    thingBuffer1.Clear();
                                    sightTracker.factionedUInt64Map.GetThings(record.flag, thingBuffer1);
                                    for (int j = 0; j < thingBuffer1.Count; j++)
                                    {
                                        enemy = thingBuffer1[j];
                                        if (enemy.Spawned
                                            && !enemy.Destroyed
                                            && (enemy.Position.DistanceToSquared(record.cell) < 225 || (enemy is Pawn enemyPawn && PawnPathUtility.GetMovingShiftedPosition(enemyPawn, 70).DistanceToSquared(record.cell) < 255))
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
                    IntVec3 cell = pawn.Position;
					IntVec3 temp;
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
            else
            {
                return thing.Position;
            }
        }
    }
}

