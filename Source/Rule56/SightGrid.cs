using System;
using System.Collections.Generic;
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

        private class IBucketableThing : IBucketable
        {
            private int bucketIndex;

            /// <summary>
            /// Thing.
            /// </summary>
            public Thing thing;
            /// <summary>
            /// Thing's faction on IBucketableThing instance creation.
            /// </summary>
            public Faction faction;
            /// <summary>
            /// Dormant comp.
            /// </summary>
            public CompCanBeDormant dormant;
			/// <summary>
			/// Dormant comp.
			/// </summary>
			public ThingComp_CombatAI ai;
			/// <summary>
			/// Last cycle.
			/// </summary>
			public int lastCycle;
            /// <summary>
            /// Pawn pawn
            /// </summary>
            public List<IntVec3> path = new List<IntVec3>(16);
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
                this.dormant = thing.GetComp_Fast<CompCanBeDormant>();
                this.ai = thing.GetComp_Fast<ThingComp_CombatAI>();
                this.thing = thing;
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
                if(!Valid(item.thing))
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
                    grid.NextCycle();                    
                    wait = false;
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
            if (!thing.Spawned || thing.Destroyed)
            {
                return false;
            }
            return (thing is Pawn pawn && !pawn.Dead) || thing is Building_Turret;
        }

        private bool Skip(IBucketableThing item)
        {
            if (!playerAlliance && item.thing is Pawn pawn)
            {                
                return (GenTicks.TicksGame - pawn.needs?.rest?.lastRestTick <= 30) || pawn.Downed;
            }            
            if (item.thing is Building_CCTVBase cctv)
            {
                return !cctv.Active;
            }
            if (item.thing is Building_TurretGun turretGun)
            {
                return !turretGun.Active || (turretGun.IsMannable && !(turretGun.mannableComp?.MannedNow ?? false));
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
            int range = SightUtility.GetSightRange(item.thing, playerAlliance);
            if (range == 0)
            {
                return false;
            }
            int ticks = GenTicks.TicksGame;            
            IntVec3 origin = item.thing.Position;
            IntVec3 pos = GetShiftedPosition(item.thing, 40, item.path);            
            if (!pos.InBounds(map))
            {
                Log.Error($"ISMA: SighGridUpdater {item.thing} position is outside the map's bounds!");
                return false;
            }
            Thing thing = item.thing;
            Pawn pawn = item.thing as Pawn;            
            IntVec3 flagPos = pos;
            if(pawn != null)
            {
                flagPos = GetShiftedPosition(thing, 240, null);
			}
			ThingComp_CombatAI comp = item.ai ?? (item.ai = thing.GetComp_Fast<ThingComp_CombatAI>());
            SightTracker.SightReader reader = comp?.sightReader ?? null;
            bool scanForEnemies;
            if ((scanForEnemies = comp != null && ticks - comp.lastInterupted >= 45 && ticks - item.lastScannedForEnemies >= (!Finder.Performance.TpsCriticallyLow ? 10 : 15) && comp?.sightReader != null && reader != null && !(item.faction?.IsPlayerSafe() ?? false)))
            {
                if (item.dormant != null && !item.dormant.Awake)
                {
                    scanForEnemies = false;
                }
                else if(pawn != null && pawn.mindState?.duty?.def == DutyDefOf.SleepForever)
                {
                    scanForEnemies = false;
				}                
			}
			if (scanForEnemies)
			{
				item.lastScannedForEnemies = ticks;
                //
                //map.debugDrawer.FlashCell(pos, 1);                
            }
			Action action = () =>
            {
                if (scanForEnemies)
                {
                    asyncActions.EnqueueMainThreadAction(delegate
                    {
                        if (!thing.Destroyed && thing.Spawned)
                        {
                            comp.OnScanStarted();
                        }
                    });                  
                }
				grid.Next();
				grid.Set(flagPos, (pawn == null || !pawn.Downed) ? GetFlags(item) : 0);
				grid.Next();
				//grid.Set(posOriginal, 1.0f, Vector2.zero);
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
                    if (scanForEnemies)
                    {
                        UInt64 flag = reader.GetEnemyFlags(cell);
                        if (flag != 0)
                        {
                            // on the main thread check for enemies on or near this cell.
                            asyncActions.EnqueueMainThreadAction(delegate
                            {
                                if (!thing.Destroyed && thing.Spawned)
                                {
                                    thingBuffer1.Clear();
									sightTracker.factionedUInt64Map.GetThings(flag, thingBuffer1);
                                    for(int i = 0; i < thingBuffer1.Count; i++)
                                    {
                                        Thing enemy = thingBuffer1[i];                                        
                                        if (enemy.Spawned && !enemy.Destroyed && enemy.HostileTo(thing))
                                        {
                                            IntVec3 enemyPos = enemy.Position;
                                            if (enemy is Pawn enemyPawn)
                                            {
                                                enemyPos = enemyPawn.GetMovingShiftedPosition(240);
                                            }
                                            if (enemyPos.DistanceToSquared(cell) < 225)
                                            {
                                                comp.Notify_EnemyVisible(enemy);
                                            }
										}
                                    }
                                    //
									//comp.Notify_EnemiesVisible(.Where(t => t.Spawned && !t.Destroyed && t.Position.DistanceToSquared(cell) < 25 && t.HostileTo(thing)));
                                }
                            });                                                     
                        }
                    }
                    // NOTE: the carry is the number of cover things between the source and the current cell.                  
                    float visibility = (float)(r - dist) / r * (1 - coverRating);
                    float d = pos.DistanceToSquared(cell);
                    // only set anything if visibility is ok
                    if (visibility > 0f && d < rSqr)
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
                        grid.Set(cell, visibility, new Vector2(cell.x - pos.x, cell.z - pos.z));
                    }
                }, range, settings.carryLimit, buffer);				
				//if (grid.GetSignalNum(flagPos) == 0)
    //            {
				//	grid.Set(flagPos, 1.0f, Vector2.zero, (pawn == null || !pawn.Downed) ? GetFlags(item) : 0);
				//}
                // if we are scanning for enemies
                if (scanForEnemies)
                {
                    // notify the pawn so they can start processing targets.
                    asyncActions.EnqueueMainThreadAction(delegate
                    {
                        if (comp.parent != null && !comp.parent.Destroyed && comp.parent.Spawned)
                        {
                            comp.OnScanFinished();
                        }
                    });
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
				if (walls != null && pawn.TryGetCellIndexAhead(ticksAhead, out int index))
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

