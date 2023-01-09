using System.Runtime.CompilerServices;
using Verse;

namespace CombatAI
{
	public static class TKCache<T, K>
	{
		public static readonly CachedDict<T, K> cache = new CachedDict<T, K>(512);

		static TKCache()
		{
			TCacheHelper.clearFuncs.Add(() => cache.Clear());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryGet(T key, out K value, int expiry = -1)
		{
			if (key is Thing thing)
			{
				return TKVCache<int, T, K>.TryGet(thing.thingIDNumber, out value);
			}
			return cache.TryGetValue(key, out value, expiry);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Put(T key, K value)
		{
			if (key is Thing thing)
			{
				TKVCache<int, T, K>.Put(thing.thingIDNumber, value);
				return;
			}
			cache[key] = value;
		}
	}
}
