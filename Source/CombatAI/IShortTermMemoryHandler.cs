using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatAI
{
    public class IShortTermMemoryHandler
    {
        public Map map;
        public IShortTermMemoryGrid grid;
        
        private int sizeX;
        private int sizeZ;
        private CellFlooder flooder;
        private CellIndices cellIndices;
        private ThreadStart threadStart;        
        private Thread thread;
        private object locker = new object();
        private int oldestQueuedAt = -1;
        private int offThreadCount;
        private readonly List<Action> mainThreadQueue = new List<Action>();
        private readonly List<Action> offThreadQueue = new List<Action>();
        private bool mapIsAlive = true;
        //private int ticksUntilUpdate = 0;
        //private int updateInterval = 15;

        public IShortTermMemoryHandler(Map map, int ticksPerUnit = 60, int maxUnits = 20)
        {
            this.map = map;           
            grid = new IShortTermMemoryGrid(map, ticksPerUnit, maxUnits);
            flooder = new CellFlooder(map);
            sizeX = map.cellIndices.mapSizeX;
            sizeZ = map.cellIndices.mapSizeZ;
            cellIndices = map.cellIndices;
            threadStart = new ThreadStart(OffMainThreadLoop);
            thread = new Thread(threadStart);
            thread.Start();            
        }

        public virtual void Tick()
        {
            //if(PerformanceTracker.TpsCriticallyLow)
            //{
            //    if (ticksUntilUpdate-- > 0)
            //        return;
            //    ticksUntilUpdate = (int) Mathf.Lerp(2, updateInterval * 4, PerformanceTracker.TpsLevel);
            //}
            if (mainThreadQueue.Count == 0)
                return;
            offThreadCount = 0;
            lock (locker)
            {
                offThreadQueue.AddRange(mainThreadQueue);
                offThreadCount = offThreadQueue.Count;
            }
            mainThreadQueue.Clear();
            oldestQueuedAt = -1;
        }        

        public virtual void Notify_MapRemoved()
        {
            try
            {
                mapIsAlive = false;
                thread.Join();
            }
            catch (Exception er)
            {
                Log.Error($"CE: SightGridManager Notify_MapRemoved failed to stop thread with {er}");
            }
        }

        public void Flood(IntVec3 center, float value, int radius)
        {
            if (!center.InBounds(map))
                return;
            if (mainThreadQueue.Count == 0)
                oldestQueuedAt = GenTicks.TicksGame;

            mainThreadQueue.Add(() =>
            {
                flooder.Flood(center, (cell, parent, dist) =>
                {
                    grid[cell] += value * Mathf.Clamp(dist / radius, 0.667f, 1.0f);
                }, null, null, radius);
            });
        }

        public void Set(IntVec3 center, float value, float maxDist)
        {                        
            if (mainThreadQueue.Count == 0)
                oldestQueuedAt = GenTicks.TicksGame;

            mainThreadQueue.Add(() =>
            {
                foreach (IntVec3 cell in new CellRect((int) (center.x - maxDist), (int) (center.z - maxDist), (int) (maxDist * 2), (int)(maxDist * 2)).Cells)
                {
                    if (cell.InBounds(map))
                        grid[cell] += value;
                }
            });
        }        

        private void OffMainThreadLoop()
        {
            Action action;
            int dangerRectLeft;            
            while (mapIsAlive)
            {
                dangerRectLeft = 0;
                action = null;
                lock (locker)
                {
                    if ((dangerRectLeft = offThreadQueue.Count) > 0)
                    {                        
                        action = offThreadQueue[0];                        
                        offThreadQueue.RemoveAt(0);
                    }
                }
                // threading goes brrrrrr
                if (action != null)
                    ApplyOffThread(action);
                // sleep so other threads can do stuff
                if (dangerRectLeft == 0)
                    Thread.Sleep(1);
            }
            Log.Message("CE: AvoidanceTracker thread stopped!");
        }

        private void ApplyOffThread(Action action)
        {
            // just so this show up in the analyzer.
            action.Invoke();
        }
    }
}

