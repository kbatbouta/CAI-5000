using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Net.NetworkInformation;
using static UnityEngine.GraphicsBuffer;
using CombatAI.Comps;
using TMPro;

namespace CombatAI
{
    public class SightHandler
    {
        private const int COVERCARRYLIMIT = 6;

        private class IThingSightRecord
        {
            /// <summary>
            /// The bucket index of the owner pawn.
            /// </summary>
            public int bucketIndex;
            /// <summary>
            /// Owner pawn.
            /// </summary>
            public Thing thing;
            /// <summary>
            /// The tick at which this pawn was updated.
            /// </summary>
            public int lastCycle;
            /// <summary>
            /// The thing faction on registeration.
            /// </summary>
            public Faction faction; 
        }        

        public readonly Map map;
        public readonly SightTracker sightTracker;
        public readonly ISignalGrid grid;
        public readonly Settings.SightPerformanceSettings settings;
        public readonly int bucketNum;

        private object locker = new object();
        private object locker_Events = new object();
        
        private List<IThingSightRecord> tmpInvalidRecords = new List<IThingSightRecord>();
        private List<IThingSightRecord> tmpInconsistentRecords = new List<IThingSightRecord>();

        private int ticksUntilUpdate;        
        private int curIndex;
        private ThreadStart threadStart;
        private Thread thread;        
        private readonly Dictionary<Thing, IThingSightRecord> records = new Dictionary<Thing, IThingSightRecord>();        
        private readonly List<IThingSightRecord>[] pool;
        private readonly List<Action> castingQueue = new List<Action>();
        private readonly List<Action> eventQueue = new List<Action>();        

        private bool mapIsAlive = true;
        private bool wait = false;        

        public SightHandler(SightTracker sightTracker, Settings.SightPerformanceSettings settings)
        {
            this.sightTracker = sightTracker;
            this.map = sightTracker.map;
            this.settings = settings;                    
            this.grid = new ISignalGrid(map);

            bucketNum = settings.buckets;
            ticksUntilUpdate = Rand.Int % this.settings.interval;            
            
            pool = new List<IThingSightRecord>[bucketNum];
            for (int i = 0; i < bucketNum; i++)
            {
                pool[i] = new List<IThingSightRecord>();
            }

            threadStart = new ThreadStart(OffMainThreadLoop);
            thread = new Thread(threadStart);
            thread.Start();
        }

        public virtual void Tick()
        {
            if (GenTicks.TicksGame % 15 == 0)
            {
                ProcessEvents();
            }
            if (ticksUntilUpdate-- > 0 || wait)
            {
                return;
            }
            tmpInvalidRecords.Clear();
            tmpInconsistentRecords.Clear();
            List<IThingSightRecord> curPool = pool[curIndex];
            for(int i = 0; i < curPool.Count; i++)
            {
                IThingSightRecord record = curPool[i];
                if(!Valid(record))
                {
                    tmpInvalidRecords.Add(record);
                    continue;
                }
                if (!Consistent(record))
                {
                    tmpInconsistentRecords.Add(record);
                    continue;
                }
                TryCastSight(record);                                      
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
            ticksUntilUpdate = (int)settings.interval;            
            curIndex++;
            if (curIndex >= bucketNum)
            {
                wait = true;
                lock (locker)
                {
                    castingQueue.Add(delegate
                    {
                        lock (locker)
                        {
                            grid.NextCycle();                            
                            wait = false;
                        }
                    });
                }                
                curIndex = 0;                                                 
            }                                                 
        }

        public virtual void Register(Thing thing)
        {
            if (records.ContainsKey(thing))
            {
                TryDeRegister(thing);
            }
            if (Valid(thing))
            {
                IThingSightRecord record = new IThingSightRecord();
                record.thing = thing;
                record.bucketIndex = (thing.thingIDNumber + 19) % bucketNum;
                record.faction = thing.Faction;
                records.Add(thing, record);
                pool[record.bucketIndex].Add(record);
            }
        }

        public virtual void TryDeRegister(Thing thing)
        {            
            if (thing != null && records.TryGetValue(thing, out IThingSightRecord record))
            {
                pool[record.bucketIndex].Remove(record);
                records.Remove(record.thing);
            }
        }

        public virtual void Notify_MapRemoved()
        {
            try
            {
                lock (locker_Events)
                {
                    eventQueue.Clear();
                }
                mapIsAlive = false;
                thread.Join();
            }
            catch(Exception er)
            {
                Log.Error($"CAI: SightGridManager Notify_MapRemoved failed to stop thread with {er}");
            }
        }       

        private bool Consistent(IThingSightRecord record)
        {
            if(record.faction != record.thing.Faction)
            {
                return false;
            }
            return true;
        }

        private bool Valid(IThingSightRecord record) => Valid(record.thing);

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

        private bool Skip(IThingSightRecord record)
        {
            if (record.thing is Pawn pawn)
            {                
                return (GenTicks.TicksGame - pawn.needs?.rest?.lastRestTick <= 30) || pawn.Downed;
            }
            if (record.thing is Building_TurretGun turret)
            {
                return !turret.Active || (turret.IsMannable && !(turret.mannableComp?.MannedNow ?? false));
            }
            return false;
        }

        private UInt64 GetFlags(IThingSightRecord record)
        {
            return record.thing.GetThingFlags();
        }

        private bool TryCastSight(IThingSightRecord record)
        {
            if (grid.CycleNum == record.lastCycle || Skip(record))
            {
                return false;
            }
            int range = SightUtility.GetSightRange(record.thing);
            int ticks = GenTicks.TicksGame;            
            IntVec3 pos = GetShiftedPosition(record);
            if (!pos.InBounds(map))
            {
                Log.Error($"CE: SighGridUpdater {record.thing} position is outside the map's bounds!");
                return false;
            }
            ThingComp_CombatAI comp = record.thing.GetComp_Fast<ThingComp_CombatAI>(allowFallback: false);
            bool scanForEnemies = comp?.sightReader != null && !(record.faction?.IsPlayerSafe() ?? false);           
            Action action = () =>
            {
                if (scanForEnemies)
                {
                    lock (locker_Events)
                    {
                        eventQueue.Add(() =>
                        {
                            comp.OnScanStarted();
                        });
                    }
                }
                grid.Next();
                grid.Set(pos, 1.0f, Vector2.zero, GetFlags(record));                
                float r = range * 1.23f;
                float rSqr = range * range;                      
                ShadowCastingUtility.CastWeighted(map, pos, (cell, carry, dist, coverRating) =>
                {
                    if (scanForEnemies)
                    {
                        UInt64 flag = comp.sightReader.GetEnemyFlags(cell);
                        if (flag != 0)
                        {
                            lock (locker_Events)
                            {
                                eventQueue.Add(() =>
                                {                                    
                                    comp.Notify_EnemiesVisible(sightTracker.factionedIndices.GetThings(flag).Where(t => t.Spawned && !t.Destroyed && t.Position.DistanceToSquared(cell) < 25).ToList());                                                                        
                                });
                            }                            
                        }
                    }
                    // NOTE: the carry is the number of cover things between the source and the current cell.                       
                    float visibility = (float)(r - dist) / r * (1 - coverRating);
                    // only set anything if visibility is ok
                    if (visibility >= 0f && pos.DistanceToSquared(cell) < rSqr)
                    {
                        grid.Set(cell, visibility, new Vector2(cell.x - pos.x, cell.z - pos.z));
                    }
                }, range, COVERCARRYLIMIT);
                if (scanForEnemies)
                {
                    lock (locker_Events)
                    {
                        eventQueue.Add(() =>
                        {
                            comp.OnScanFinished();
                        });
                    }
                }
            };
            lock (locker)
            {
                castingQueue.Add(action);
            }
            record.lastCycle = grid.CycleNum;            
            return true;
        }        

        private void OffMainThreadLoop()
        {
            Action castAction;
            int castActionsLeft;
            while (mapIsAlive)
            {
                castAction = null;
                castActionsLeft = 0;
                lock (locker)
                {
                    if ((castActionsLeft = castingQueue.Count) > 0)
                    {
                        castAction = castingQueue[0];
                        castingQueue.RemoveAt(0);
                    }
                }                
                if (castAction != null)
                {                    
                    castAction.Invoke();
                }                
                if (castActionsLeft == 0)
                {
                    Thread.Sleep(Finder.Settings.Advanced_SightThreadIdleSleepTimeMS);
                }
            }
            Log.Message("ISMA: SightGridManager thread stopped!");
        }        

        private void ProcessEvents()
        {
            while (mapIsAlive)
            {
                Action action = null;
                lock (locker_Events)
                {
                    if (eventQueue.Count > 0)
                    {
                        action = eventQueue[0];
                        eventQueue.RemoveAt(0);
                    }
                }
                if (action == null)
                {
                    break;
                }
                else
                {
                    action();
                }
            }
        }

        private IntVec3 GetShiftedPosition(IThingSightRecord record)
        {
            if (record.thing is Pawn pawn)
            {
                return GetMovingShiftedPosition(pawn, settings.interval, bucketNum);
            }
            else
            {
                return record.thing.Position;
            }
        }

        private static IntVec3 GetMovingShiftedPosition(Pawn pawn, float updateInterval, int bucketNum)
        {
            PawnPath path;

            if (!(pawn.pather?.moving ?? false) || (path = pawn.pather.curPath) == null || path.NodesLeftCount <= 1)
            {
                return pawn.Position;
            }

            float distanceTraveled = Mathf.Min(pawn.GetStatValue(StatDefOf.MoveSpeed) * (updateInterval * bucketNum) / 60f, path.NodesLeftCount - 1);
            return path.Peek(Mathf.FloorToInt(distanceTraveled));
        }
    }
}

