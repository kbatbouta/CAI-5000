using System;
using Verse;
using System.Collections.Generic;

namespace CombatAI
{
    public class MapComponent_CombatAI : MapComponent
    {
        /*      Threading
         * ----- ----- ----- -----
         */

        private object locker = new object();
        private bool queueEmpty = false;

        private List<Action> queuedActions = new List<Action>();

        /*   ISMA map elements
         * ----- ----- ----- -----
         */

        public CellFlooder flooder;

        public TGrid<float> tempGrid;

        /* 
         * ----- ----- ----- -----
         */

        public MapComponent_CombatAI(Map map) : base(map)
        {
            flooder = new CellFlooder(map);
            tempGrid = new TGrid<float>(map);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (queueEmpty && GenTicks.TicksGame % 5 != 0)
            {
                return;
            }
            while (true)
            {
                Action action = null;
                lock (locker)
                {
                    if(queuedActions.Count > 0)
                    {
                        action = queuedActions[0];
                        queuedActions.RemoveAt(0);
                        queueEmpty = queuedActions.Count == 0;
                    }
                }
                if (action != null)
                {
                    try
                    {
                        action();
                    }
                    catch(Exception er)
                    {
                        Log.Error(er.Message);
                    }
                }
                else
                {
                    break;
                }            
            }
        }

        public void EnqueueMainThreadAction(Action action)
        {
            lock (locker)
            {
                queuedActions.Add(action);
                queueEmpty = true;
            }
        }
    }
}

