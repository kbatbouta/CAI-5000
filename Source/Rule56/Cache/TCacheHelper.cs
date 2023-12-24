using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
namespace CombatAI
{
    public static class TCacheHelper
    {
	    public static readonly List<Action> clearFuncs = new List<Action>();

        public static void ClearCache()
        {
            for (int i = 0; i < clearFuncs.Count; i++)
            {
                Action action = clearFuncs[i];
                action();
            }
        }

        internal static class IndexGetter<T> where T : notnull
        {
	        internal static readonly        bool              indexable;
	        internal static readonly unsafe delegate*<T, int> Default;

	        static unsafe IndexGetter()
	        {
		        foreach (MethodInfo method in typeof(Methods).GetMethods(AccessTools.all))
		        {
			        if (method.ReturnType == typeof(int))
			        {
				        ParameterInfo[] parameterInfos = method.GetParameters();
				        if (parameterInfos.Length == 1 && parameterInfos[0].ParameterType.IsAssignableFrom(typeof(T)))
				        {
					        Default   = (delegate*<T, int>) method.MethodHandle.GetFunctionPointer();
					        indexable = true;
					        break;
				        }
			        }
		        }
	        }
        }
        
        private static class Methods
        {
	        public static int Def(Def                               def)     => def.index;
	        public static int Thing(Thing                           thing)   => thing.thingIDNumber;
	        public static int ThingComp(ThingComp                   comp)    => comp.parent.thingIDNumber;
	        public static int Map(Map                               map)     => map.uniqueID;
	        public static int DesignationManager(DesignationManager manager) => manager.map.uniqueID;
	        public static int Room(Room                             room)    => room.ID;
	        public static int Ideo(Ideo                             ideo)    => ideo.id;
	        public static int HediffSet(HediffSet                   set)     => set.pawn.thingIDNumber;
	        public static int Bill(Bill                             bill)    => bill.loadID;
        }
    }
}
