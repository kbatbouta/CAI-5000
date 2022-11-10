using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Threading;

namespace CombatAI
{
    public class AsyncActionsHelper
    {
        private bool alive = true;
        private bool mainThreadActionQueueEmpty = false;

        private readonly int hashOffset;
        private readonly object locker_Main = new object();
        private readonly object locker_offMain = new object();        
        private readonly Thread thread;
        
        private readonly List<Action> queuedMainThreadActions = new List<Action>();
        private readonly List<Action> queuedOffThreadActions = new List<Action>();
        private readonly int mainLoopTickInterval;

        public bool Alive
        {
            get => alive;
        }
        
        public AsyncActionsHelper(int mainLoopTickInterval = 5)
        {
            this.mainLoopTickInterval = mainLoopTickInterval;
            this.hashOffset = Rand.Int % 128;
            this.thread = new Thread(OffMainThreadActionLoop);
            this.thread.Start();
        }        

        public virtual void ExecuteMainThreadActions()
        {
            MainThreadActionLoop();
        } 

        public virtual void Kill()
        {
            alive = false;
            try
            {
                thread.Abort();
            }
            catch(Exception)
            {
            }
        }

        public void EnqueueOffThreadAction(Action action)
        {
            lock (locker_offMain)
            {
                queuedOffThreadActions.Add(action);
            }
        }

        public void EnqueueMainThreadAction(Action action)
        {            
            lock (locker_Main)
            {                                    
                queuedMainThreadActions.Add(action);
                mainThreadActionQueueEmpty = false;
            }            
        }

        private Action DequeueOffThreadAction()
        {
            Action action = null;
            lock (locker_offMain)
            {
                if (queuedOffThreadActions.Count > 0)
                {
                    action = queuedOffThreadActions[0];
                    queuedOffThreadActions.RemoveAt(0);
                }
            }
            return action;
        }

        private Action DequeueMainThreadAction()
        {
            Action action = null;
            lock (locker_Main)
            {
                if (queuedMainThreadActions.Count > 0)
                {
                    action = queuedMainThreadActions[0];
                    queuedMainThreadActions.RemoveAt(0);                    
                }
                mainThreadActionQueueEmpty = queuedMainThreadActions.Count == 0;
            }
            return action;
        }

        private void MainThreadActionLoop()
        {
            if (!mainThreadActionQueueEmpty || (GenTicks.TicksGame + this.hashOffset) % mainLoopTickInterval == 0)
            {
                while (true)
                {
                    Action action = DequeueMainThreadAction();
                    if (action != null)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception er)
                        {
                            Log.Error(er.ToString());
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void OffMainThreadActionLoop()
        {
            while (alive)
            {
                Action action = DequeueOffThreadAction();               
                if (action != null)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception er)
                    {
                        Log.Error(er.ToString());
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }                
            }
        }
    }
}

