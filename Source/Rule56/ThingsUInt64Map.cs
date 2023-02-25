using System.Collections.Generic;
using System.Linq;
using Verse;
namespace CombatAI
{
    public class IThingsUInt64Map
    {
        private const ulong BOT = 0x00000000FFFFFFFF;
        private const ulong TOP = 0xFFFFFFFF00000000;

        private readonly IBuckets<IBucketableThing> buckets;

        public IThingsUInt64Map()
        {
            buckets = new IBuckets<IBucketableThing>(64);
        }

        public void Clear()
        {
            buckets.Clear();
        }

        public void Add(Thing thing)
        {
            buckets.Add(new IBucketableThing(thing));
        }

        public void Remove(Thing thing)
        {
            buckets.RemoveId(thing.thingIDNumber);
        }

        public IEnumerable<Thing> GetThings(ulong flag)
        {
            unchecked
            {
                if (flag == 0)
                {
                    yield break;
                }
                ulong c = 0xFF;
                int   m = (TOP & flag) != 0 ? 8 : 4;
                int   i;
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
                        ulong k = (ulong)1 << i * 8;
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

        public void GetThings(ulong flag, List<Thing> bufferList)
        {
            bufferList.Clear();
            unchecked
            {
                if (flag == 0)
                {
                    return;
                }
                ulong c = 0xFF;
                int   m = (TOP & flag) != 0 ? 8 : 4;
                int   i;
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
                        ulong k = (ulong)1 << i * 8;
                        for (int j = 0; j < 8; j++)
                        {
                            if ((flag & k) != 0)
                            {
                                List<IBucketableThing> items = buckets.GetBucket(j + i * 8);
                                for (int l = 0; l < items.Count; l++)
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

        private static IEnumerable<int> GetBucketIndices(ulong flag)
        {
            unchecked
            {
                if (flag == 0)
                {
                    yield break;
                }
                ulong c = 0xFF;
                int   m = (TOP & flag) != 0 ? 8 : 4;
                int   i;
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
                        ulong k = (ulong)1 << i * 8;
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

        private struct IBucketableThing : IBucketable
        {
            public readonly Thing thing;
            public int BucketIndex
            {
                get => thing.GetThingFlagsIndex();
            }
            public int UniqueIdNumber
            {
                get => thing.thingIDNumber;
            }

            public IBucketableThing(Thing thing)
            {
                this.thing = thing;
            }
        }
    }
}
