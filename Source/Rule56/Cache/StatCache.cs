using System;
using RimWorld;
using Verse;
namespace CombatAI
{
    public static class StatCache
    {

        private static readonly CachedDict<ICacheKey, float> cache = new CachedDict<ICacheKey, float>(1024);

        public static float GetStatValue_Fast(this Thing thing, StatDef stat, int expiry, bool applyPostProcess = true)
        {
            if (thing == null)
            {
                return stat.defaultBaseValue;
            }
            ICacheKey key = ICacheKey.For(thing, stat, applyPostProcess);
            if (cache.TryGetValue(key, out float value, expiry))
            {
                return value;
            }
            return cache[key] = thing.GetStatValue(stat, applyPostProcess);
        }

        public static void ClearCache()
        {
            cache.Clear();
        }

        private struct ICacheKey : IEquatable<ICacheKey>
        {
            public int  thingIdNumber;
            public int  statDefIndex;
            public bool applyPostProcess;

            public static ICacheKey For(Thing thing, StatDef stat, bool applyPostProcess)
            {
                ICacheKey key = new ICacheKey();
                key.thingIdNumber    = thing.thingIDNumber;
                key.statDefIndex     = stat.index;
                key.applyPostProcess = applyPostProcess;
                return key;

            }

            public static ICacheKey For(int thingIdNumber, int statDefIndex, bool applyPostProcess)
            {
                ICacheKey key = new ICacheKey();
                key.thingIdNumber    = thingIdNumber;
                key.statDefIndex     = statDefIndex;
                key.applyPostProcess = applyPostProcess;
                return key;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + thingIdNumber.GetHashCode();
                    hash = hash * 23 + statDefIndex.GetHashCode();
                    hash = hash * 23 + (applyPostProcess ? 17 : 0);
                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                return obj is ICacheKey && Equals((ICacheKey)obj);
            }

            public int GetHashCode(ICacheKey obj)
            {
                return obj.GetHashCode();
            }

            public bool Equals(ICacheKey other)
            {
                return thingIdNumber == other.thingIdNumber
                       && statDefIndex == other.statDefIndex
                       && applyPostProcess == other.applyPostProcess;
            }
        }
    }
}
