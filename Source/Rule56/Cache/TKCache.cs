using System;
using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Runtime.CompilerServices;

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
			return cache.TryGetValue(key, out value, expiry);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Put(T key, K value)
		{
			cache[key] = value;
		}
	}
}