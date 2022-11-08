using System;
using Verse;
using System.Collections.Generic;
using Verse.Noise;
using System.Diagnostics;

namespace CombatAI
{ 
    public class FlagedBuckets
    {
        private const UInt64 BOT = 0x00000000FFFFFFFF;
        private const UInt64 TOP = 0xFFFFFFFF00000000;

        private List<Thing>[] buckets = new List<Thing>[64];

        public FlagedBuckets()
        {
            for(int i =0;i < 64; i++)
            {
                buckets[i] = new List<Thing>();
            }
        }

        public void Clear()
        {
            for(int i = 0;i < 64; i++)
            {
                buckets[i].Clear();                
            }
        }

        public void Add(Thing thing)
        {
            List<Thing> things = buckets[thing.GetThingFlagsIndex()];
            if (!things.Contains(thing))
            {
                things.Add(thing);
            }
        }

        public void Remove(Thing thing)
        {
            buckets[thing.GetThingFlagsIndex()].RemoveAll(t => t == thing);
        }        

        public IEnumerable<Thing> GetThings(UInt64 flag)
        {
            foreach(int index in GetBucketIndices(flag))
            {
                List<Thing> things = buckets[index];
                for(int i = 0;i < things.Count;i++)
                {
                    yield return things[i];
                }
            }                    
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

