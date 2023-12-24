using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;
namespace CombatAI
{
    public static class TKCache<T, K>
    {
	    private static readonly        CachedDict<int, K>             dict_indexed;
        private static readonly        CachedDict<T, K>               dict;
        private static readonly unsafe delegate*<T, out K, int, bool> getter;
        private static readonly unsafe delegate*<T, K, void>          setter;

        static unsafe TKCache()
        {
	        if (TCacheHelper.IndexGetter<T>.indexable)
	        {
		        if (Finder.Settings.Debug)
		        {
			        Log.Message($"ISMA: {typeof(T)} using indexed cached!");
		        }
		        dict_indexed = new CachedDict<int, K>(128);
		        TCacheHelper.clearFuncs.Add(dict_indexed.Clear);
		        getter = (delegate*<T, out K, int, bool>) typeof(TKCache<T, K>).GetMethod("TryGet_Indexed", AccessTools.all).MethodHandle.GetFunctionPointer();
		        setter = (delegate*<T, K, void>)typeof(TKCache<T, K>).GetMethod("Put_Indexed", AccessTools.all).MethodHandle.GetFunctionPointer();
	        }
	        else
	        {
		        dict = new CachedDict<T, K>(128);
		        TCacheHelper.clearFuncs.Add(dict.Clear);
		        getter = (delegate*<T, out K, int, bool>) typeof(TKCache<T, K>).GetMethod("TryGet_Default", AccessTools.all).MethodHandle.GetFunctionPointer();
		        setter = (delegate*<T, K, void>)typeof(TKCache<T, K>).GetMethod("Put_Default", AccessTools.all).MethodHandle.GetFunctionPointer();
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet(T key, out K val, int expiry = -1)
        {
	        unsafe
	        {
		        return getter(key, out val, expiry);
	        }   
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Put(T key, K value)
        {
	        unsafe
	        {
		        setter(key, value);
	        }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool TryGet_Indexed(T key, out K val, int expiry)
        {
	        return dict_indexed.TryGetValue(TCacheHelper.IndexGetter<T>.Default(key), out val, expiry);
        }
	        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool TryGet_Default(T key, out K val, int expiry)
        {
	        return dict.TryGetValue(key, out val, expiry);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Put_Indexed(T key, K val)
        {
	        dict_indexed[TCacheHelper.IndexGetter<T>.Default(key)] = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Put_Default(T key, K val)
        {
	        dict[key] = val;
        }
    }
}
