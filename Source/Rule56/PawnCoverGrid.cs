using System;
using RimWorld;
using Verse;

namespace CombatAI
{
    //public class PawnCoverGrid
    //{
    //    private class IBucketableThing : IBucketable
    //    {
    //        private int bucketIndex;
    //        /// <summary>
    //        /// Thing.
    //        /// </summary>
    //        public Thing thing;
    //        /// <summary>
    //        /// Thing's faction on IBucketableThing instance creation.
    //        /// </summary>
    //        public Faction faction;
    //        /// <summary>
    //        /// Last cycle.
    //        /// </summary>
    //        public int lastCycle;
    //        /// <summary>
    //        /// Bucket index.
    //        /// </summary>
    //        public int BucketIndex =>
    //            bucketIndex;
    //        /// <summary>
    //        /// Thing id number.
    //        /// </summary>
    //        public int UniqueIdNumber =>
    //            thing.thingIDNumber;

    //        public IBucketableThing(Thing thing, int bucketIndex)
    //        {
    //            this.thing = thing;
    //            this.faction = thing.Faction;
    //            this.bucketIndex = bucketIndex;
    //        }
    //    }        

    //    private readonly AsyncActions asyncActions;
    //    private readonly IBuckets<IBucketableThing> buckets;

    //    public readonly Map map;
    //    public readonly ISignalGrid grid;

    //    public PawnCoverGrid(Map map)
    //    {
    //        this.map = map;
    //        this.grid = new ISignalGrid(map);
    //        this.buckets = new IBuckets<IBucketableThing>(20);
    //        this.asyncActions = new AsyncActions();
    //    }

    //    public void PawnCoverGridTick()
    //    {
    //        asyncActions.ExecuteMainThreadActions();

    //    }

    //    public void Register(Thing thing)
    //    {
    //        buckets.Add(new IBucketableThing(thing, thing.thingIDNumber % 20));
    //    }

    //    public void TryDeRegister(Thing thing)
    //    {
    //        buckets.RemoveId(thing.thingIDNumber);
    //    }
    //}
}

