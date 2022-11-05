//using System;
//using Verse;
//using System.Threading;
//using System.Collections.Generic;

//namespace CombatAI
//{
//    public abstract class SightTracker
//    {
//        private const int BucketNum = 30;

//        private int ticksToPushQueued = Rand.Int % 60;        
//        private int curBucket;
//        private bool alive = true;
//        private readonly object locker = new object();
//        private readonly Thread thread;

//        private readonly List<Thing>[] buckets = new List<Thing>[BucketNum];
//        private readonly List<Action> queuedActions = new List<Action>();
//        private readonly List<Action> offThreadActions = new List<Action>();
        
//        public readonly MapComponent_Sight comp;

//        public Map Map
//        {
//            get => comp.map;
//        }

//        public SightTracker(MapComponent_Sight sightComp)
//        {
//            ThreadStart start = new ThreadStart(OffMainThreadLoop);
//            this.thread = new Thread(start);
//            this.thread.Start();
//            this.comp = sightComp;
//        }

//        public void Tick()
//        {
//            if (ticksToPushQueued-- <= 0)
//            {
//                ticksToPushQueued = 10;
//                TryPushQueued();
//            }
//            List<Thing> bucket = buckets[curBucket];
//            bucket.RemoveAll(t => !ValidThing(t));
//            for (int i = 0; i < bucket.Count; i++)
//            {
                
//            }
//            curBucket = (curBucket + 1) % BucketNum;            
//            PostTick();
//        }

//        public abstract bool ValidThing(Thing thing);

//        public virtual void Destroy()
//        {
//            this.alive = false;
//            for (int i = 0; i < this.buckets.Length; i++)
//            {
//                this.buckets[i].Clear();
//            }
//            this.offThreadActions.Clear();
//            this.thread.Abort();
//        }

//        public virtual void PostTick()
//        {
//        }

//        private void TryPushQueued()
//        {            
//            try
//            {
//                if (queuedActions.Count > 0)
//                {
//                    lock (locker)
//                    {
//                        offThreadActions.AddRange(queuedActions);
//                    }
//                    queuedActions.Clear();
//                }
//            }
//            catch (Exception er)
//            {
//                Log.Error(er.Message);
//            }            
//        }

//        private void OffMainThreadLoop()
//        {
//            while (alive)
//            {
//                Action action = null;
//                lock (locker)
//                {
//                    if(offThreadActions.Count == 0)
//                    {
//                        action = offThreadActions.Pop();
//                    }
//                }
//                if(action != null)
//                {
//                    try
//                    {
//                        action.Invoke();
//                    }
//                    catch(Exception er)
//                    {
//                        Log.Error(er.Message);
//                    }
//                }
//                else
//                {
//                    Thread.Sleep(16);
//                }
//            }
//        }
//    }
//}

