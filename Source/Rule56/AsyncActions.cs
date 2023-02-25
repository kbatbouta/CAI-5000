using System;
using System.Collections.Generic;
using System.Threading;
using Verse;
namespace CombatAI
{
    public class AsyncActions
    {

        private readonly int hashOffset;

        public readonly  object locker_Main    = new object();
        public readonly  object locker_offMain = new object();
        private readonly int    mainLoopTickInterval;

        private readonly List<Action> queuedMainThreadActions = new List<Action>();
        private readonly List<Action> queuedOffThreadActions  = new List<Action>();
        private readonly Thread       thread;
        private          bool         mainThreadActionQueueEmpty;

        public AsyncActions(int mainLoopTickInterval = 5)
        {
            this.mainLoopTickInterval = mainLoopTickInterval;
            hashOffset                = Rand.Int % 128;
            thread                    = new Thread(OffMainThreadActionLoop);
        }

        public bool Alive
        {
            get;
            private set;
        } = true;

        public void Start()
        {
            thread.Start();
        }

        public void ExecuteMainThreadActions()
        {
            MainThreadActionLoop();
        }

        public void Kill()
        {
            Alive = false;
            try
            {
                lock (locker_Main)
                {
                    queuedMainThreadActions.Clear();
                }
                lock (locker_offMain)
                {
                    queuedOffThreadActions.Clear();
                }
                thread.Abort();
            }
            catch (Exception)
            {
            }
        }

        public void EnqueueOffThreadAction(Action action)
        {
            lock (locker_offMain)
            {
                if (queuedOffThreadActions.Count > 1024)
                {
                    Log.Error($"ISMA: AsyncActions is leaking memory with more than {queuedOffThreadActions.Count} actions queued!");
                }
                else
                {
                    queuedOffThreadActions.Add(action);
                }
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
            if (!mainThreadActionQueueEmpty || (GenTicks.TicksGame + hashOffset) % mainLoopTickInterval == 0)
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
            while (Alive)
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
