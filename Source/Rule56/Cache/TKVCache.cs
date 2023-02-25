using System.Runtime.CompilerServices;
namespace CombatAI
{
    public static class TKVCache<T, K, V>
    {
        public static readonly CachedDict<T, V> cache = new CachedDict<T, V>(512);

        static TKVCache()
        {
            TCacheHelper.clearFuncs.Add(() => cache.Clear());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet(T key, out V value, int expiry = -1)
        {
            return cache.TryGetValue(key, out value, expiry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Put(T key, V value)
        {
            cache[key] = value;
        }
    }
}
