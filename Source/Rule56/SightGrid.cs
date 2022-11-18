using System;
using System.Collections.Generic;
using System.Linq;
using CombatAI.Comps;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatAI
{
    public class SightGrid
    {
        private List<Vector3> buffer = new List<Vector3>(1024);
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
            /// Last cycle.
            /// </summary>
            public int lastCycle;
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
                this.faction = thing.Faction;
                this.bucketIndex = bucketIndex;
            }
        }

        private int ticksUntilUpdate;
        private bool wait = false;
        private AsyncActions asyncActions;
        private IBuckets<IBucketableThing> buckets;
        private readonly List<IBucketableThing> tmpDeRegisterList = new List<IBucketableThing>();
        private readonly List<IBucketableThing> tmpInvalidRecords = new List<IBucketableThing>();
        private readonly List<IBucketableThing> tmpInconsistentRecords = new List<IBucketableThing>();

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

        public SightGrid(SightTracker sightTracker, Settings.SightPerformanceSettings settings)
        {            
            this.sightTracker = sightTracker;
            this.map = sightTracker.map;
            this.settings = settings;                    
            this.grid = new ITSignalGrid(map);            
            this.asyncActions = new AsyncActions();            
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
            }
        }

        public virtual void TryDeRegister(Thing thing)
        {
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
            return (thing is Pawn pawn && !pawn.Dead) || thing is Building_TurretGun;
        }

        private bool Skip(IBucketableThing item)
        {
            if (!playerAlliance && item.thing is Pawn pawn)
            {                
                return (GenTicks.TicksGame - pawn.needs?.rest?.lastRestTick <= 30) || pawn.Downed;
            }
            if (item.thing is Building_TurretGun turret)
            {
                return !turret.Active || (turret.IsMannable && !(turret.mannableComp?.MannedNow ?? false));
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
            int ticks = GenTicks.TicksGame;            
            IntVec3 pos = GetShiftedPosition(item.thing);
            if (!pos.InBounds(map))
            {
                Log.Error($"ISMA: SighGridUpdater {item.thing} position is outside the map's bounds!");
                return false;
            }
            Thing thing = item.thing;
            ThingComp_CombatAI comp = thing.GetComp_Fast<ThingComp_CombatAI>(allowFallback: false);
            SightTracker.SightReader reader = comp?.sightReader ?? null;
            bool scanForEnemies = comp?.sightReader != null && reader != null && !(item.faction?.IsPlayerSafe() ?? false);           
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
                grid.Set(pos, 1.0f, Vector2.zero, GetFlags(item));                
                float r = range * 1.23f;
                float rSqr = range * range;
                float rHafledSqr = rSqr / 4f; 
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
                                    comp.Notify_EnemiesVisible(sightTracker.factionedUInt64Map.GetThings(flag).Where(t => t.Spawned && !t.Destroyed && t.Position.DistanceToSquared(cell) < 25 && t.HostileTo(thing)));
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
                        if (playerAlliance && d >= rHafledSqr)
                        {
                            visibility *= 0.05f; 
                        }
                        grid.Set(cell, visibility, new Vector2(cell.x - pos.x, cell.z - pos.z));
                    }
                }, range, settings.carryLimit, buffer);
                // if we are scanning for enemies
                if (scanForEnemies)
                {
                    // notify the pawn so they can start processing targets.
                    asyncActions.EnqueueMainThreadAction(delegate
                    {
                        if (!thing.Destroyed && thing.Spawned)
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

        private IntVec3 GetShiftedPosition(Thing thing)
        {
            if (thing is Pawn pawn)
            {
                return pawn.GetMovingShiftedPosition(40);
            }
            else
            {
                return thing.Position;
            }
        }
    }
}

