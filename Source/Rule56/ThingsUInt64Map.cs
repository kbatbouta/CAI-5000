using System;
using Verse;
using System.Collections.Generic;
using Verse.Noise;
using System.Diagnostics;
using System.Linq;

namespace CombatAI
{ 
    public class IThingsUInt64Map
    {
        private const UInt64 BOT = 0x00000000FFFFFFFF;
        private const UInt64 TOP = 0xFFFFFFFF00000000;

        private struct IBucketableThing : IBucketable
        {
            public Thing thing;
            public int BucketIndex => thing.GetThingFlagsIndex();
            public int UniqueIdNumber => thing.thingIDNumber;

            public IBucketableThing(Thing thing)
            {
                this.thing = thing;
            }
        }

        private IBuckets<IBucketableThing> buckets;

        public IThingsUInt64Map()
        {
            buckets = new IBuckets<IBucketableThing>(64);
        }

        public void Clear() =>
            buckets.Clear();

        public void Add(Thing thing) =>
            buckets.Add(new IBucketableThing(thing));

        public void Remove(Thing thing) =>
            buckets.RemoveId(thing.thingIDNumber);

        //public IEnumerable<Thing> GetThings(UInt64 flag)
        //{
        //    foreach(int index in GetBucketIndices(flag))
        //    {
        //        List<IBucketableThing> items = buckets.GetBucket(index);
        //        for(int i = 0;i < items.Count;i++)
        //        {
        //            yield return items[i].thing;
        //        }
        //    }                    
        //}

		public IEnumerable<Thing> GetThings(UInt64 flag)
		{
			unchecked
			{
				if (flag == 0)
				{
					yield break;
				}
				UInt64 c = 0xFF;
				int m = ((TOP & flag) != 0) ? 8 : 4;
				int i;
				if ((BOT & flag) != 0)
				{
					c = 0xFF;
					i = 0;
				}
				else
				{
					c = 0xFF00000000;
					i = 4;
				}
				for (; i < m; i++)
				{
					if ((c & flag) != 0)
					{
						UInt64 k = ((UInt64)1) << (i * 8);
						for (int j = 0; j < 8; j++)
						{
							if ((flag & k) != 0)
							{
								List<IBucketableThing> items = buckets.GetBucket(j + i * 8);
								for (int l = 0; l < items.Count; l++)
								{
									yield return items[l].thing;
								}
							}
							k = k << 1;
						}
					}
					c = c << 8;
				}
			}
		}

		public void GetThings(UInt64 flag, List<Thing> bufferList)
		{
			bufferList.Clear();
			unchecked
			{
				if (flag == 0)
				{
					return;
				}
				UInt64 c = 0xFF;
				int m = ((TOP & flag) != 0) ? 8 : 4;
				int i;
				if ((BOT & flag) != 0)
				{
					c = 0xFF;
					i = 0;
				}
				else
				{
					c = 0xFF00000000;
					i = 4;
				}
				for (; i < m; i++)
				{
					if ((c & flag) != 0)
					{
						UInt64 k = ((UInt64)1) << (i * 8);
						for (int j = 0; j < 8; j++)
						{
							if ((flag & k) != 0)
							{
								List<IBucketableThing> items = buckets.GetBucket(j + i * 8);
								for(int l = 0; l < items.Count; l++)
                                {
                                    bufferList.Add(items[l].thing);
								}
							}
							k = k << 1;
						}
					}
					c = c << 8;
				}
			}
		}

		public List<Thing> GetAllThings()
		{
			return buckets.GetAll().Select(t => t.thing).ToList();
		}

		private static IEnumerable<int> GetBucketIndices(UInt64 flag)
        {
            unchecked
            {
                if (flag == 0)
                {
                    yield break;
                }
                UInt64 c = 0xFF;
                int m = ((TOP & flag) != 0) ? 8 : 4;
                int i;
                if ((BOT & flag) != 0)
                {
                    c = 0xFF;
                    i = 0;
                }
                else
                {
                    c = 0xFF00000000;
                    i = 4;
                }
                for (; i < m; i++)
                {
                    if ((c & flag) != 0)
                    {
                        UInt64 k = ((UInt64)1) << (i * 8);
                        for (int j = 0; j < 8; j++)
                        {
                            if ((flag & k) != 0)
                            {
                                yield return j + i * 8;                                
                            }
                            k = k << 1;
                        }
                    }
                    c = c << 8;
                }
            }
        }
    }
}

