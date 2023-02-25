using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld.Planet;
using Verse;
namespace CombatAI
{
    public static class CompCache
    {
        private static          List<Action<Map>>                             mapComponents_removeActions;
        private static readonly Dictionary<Type, Action<ThingWithComps>>      thingComps_removeActions        = new Dictionary<Type, Action<ThingWithComps>>();
        private static readonly Dictionary<int, List<Action<ThingWithComps>>> thingComps_removeActionsByThing = new Dictionary<int, List<Action<ThingWithComps>>>();

        public static T GetComp_Fast<T>(this Thing thing, bool allowFallback = true) where T : ThingComp
        {
            if (thing is ThingWithComps thingWithComps)
            {
                return GetComp_Fast<T>(thingWithComps, allowFallback);
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetComp_Fast<T>(this ThingWithComps thing) where T : ThingComp
        {
            return GetComp_Fast<T>(thing, true);
        }

        public static T GetComp_Fast<T>(this ThingWithComps thing, bool allowFallback = true) where T : ThingComp
        {
            if (ThingComp_Cache<T>.compsById.TryGetValue(thing.thingIDNumber, out T value))
            {
                return value;
            }
            if (thing.comps == null)
            {
                return null;
            }
            if (!thingComps_removeActions.TryGetValue(typeof(T), out Action<ThingWithComps> removeAction))
            {
                MethodInfo info = AccessTools.Method(typeof(ThingComp_Cache<>).MakeGenericType(typeof(T)), "Remove");
                thingComps_removeActions.Add(typeof(T), removeAction = t =>
                {
                    info.Invoke(null, new[]
                    {
                        t
                    });
                });
            }
            if (!thingComps_removeActionsByThing.TryGetValue(thing.thingIDNumber, out List<Action<ThingWithComps>> actions))
            {
                thingComps_removeActionsByThing[thing.thingIDNumber] = actions = new List<Action<ThingWithComps>>();
            }
            for (int i = 0; i < thing.comps.Count; i++)
            {
                CompProperties props = thing.comps[i].props;
                if (props != null && props.compClass == typeof(T))
                {
                    actions.Add(removeAction);
                    return ThingComp_Cache<T>.compsById[thing.thingIDNumber] = thing.comps[i] as T;
                }
            }
            if (allowFallback)
            {
                for (int i = 0; i < thing.comps.Count; i++)
                {
                    if (thing.comps[i] is T comp)
                    {
                        actions.Add(removeAction);
                        return ThingComp_Cache<T>.compsById[thing.thingIDNumber] = comp;
                    }
                }
            }
            return null;
        }

        public static T GetComp_Fast<T>(this Map map) where T : MapComponent
        {
            T   comp;
            int count = MapComponent_Cache<T>.count;
            for (int i = 0; i < count; i++)
            {
                if ((comp = MapComponent_Cache<T>.comps[i]).map == map)
                {
                    return comp;
                }
            }
            if ((comp = map.GetComponent<T>()) != null)
            {
                MapComponent_Cache<T>.Add(comp);
            }
            return comp;
        }

        public static T GetWorldComp<T>() where T : WorldComponent
        {
            return WorldComponent_Cache<T>.Comp;
        }

        public static T GetGameComp<T>() where T : GameComponent
        {
            return GameComponent_Cache<T>.Comp;
        }

        public static T GetComp_Fast<T>(this World _) where T : WorldComponent
        {
            return WorldComponent_Cache<T>.Comp;
        }

        public static T GetComp_Fast<T>(this Game _) where T : GameComponent
        {
            return GameComponent_Cache<T>.Comp;
        }

        public static void Notify_ThingDestroyed(ThingWithComps thing)
        {
            if (thingComps_removeActionsByThing.TryGetValue(thing.thingIDNumber, out List<Action<ThingWithComps>> actions))
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    actions[i](thing);
                }
                actions.Clear();
                thingComps_removeActionsByThing.Remove(thing.thingIDNumber);
            }
        }


        public static void Notify_MapRemoved(Map map)
        {
            if (mapComponents_removeActions == null)
            {
                mapComponents_removeActions = new List<Action<Map>>();

                foreach (Type compType in typeof(MapComponent).AllSubclassesNonAbstract())
                {
                    MethodInfo info = AccessTools.Method(typeof(MapComponent_Cache<>).MakeGenericType(compType), "Remove");
                    mapComponents_removeActions.Add(t =>
                    {
                        info.Invoke(null, new[]
                        {
                            t
                        });
                    });
                }
            }
            for (int i = 0; i < mapComponents_removeActions.Count; i++)
            {
                mapComponents_removeActions[i](map);
            }
        }

        public static void ClearCaches()
        {
            foreach (Type compType in typeof(MapComponent).AllSubclassesNonAbstract())
            {
                MethodInfo info = AccessTools.Method(typeof(MapComponent_Cache<>).MakeGenericType(compType), "Clear");
                info.Invoke(null, new object[0]);
            }
            foreach (Type compType in typeof(ThingComp).AllSubclassesNonAbstract())
            {
                MethodInfo info = AccessTools.Method(typeof(ThingComp_Cache<>).MakeGenericType(compType), "Clear");
                info.Invoke(null, new object[0]);
            }
            foreach (Type compType in typeof(GameComponent).AllSubclassesNonAbstract())
            {
                MethodInfo info = AccessTools.Method(typeof(GameComponent_Cache<>).MakeGenericType(compType), "Clear");
                info.Invoke(null, new object[0]);
            }
            foreach (Type compType in typeof(WorldComponent).AllSubclassesNonAbstract())
            {
                MethodInfo info = AccessTools.Method(typeof(WorldComponent_Cache<>).MakeGenericType(compType), "Clear");
                info.Invoke(null, new object[0]);
            }
        }

        private static class ThingComp_Cache<T> where T : ThingComp
        {
            public static readonly Dictionary<int, T> compsById = new Dictionary<int, T>(1024);

            public static void Remove(ThingWithComps thing)
            {
                if (compsById.ContainsKey(thing.thingIDNumber))
                {
                    compsById.Remove(thing.thingIDNumber);
                }
            }

            public static void Clear()
            {
                compsById.Clear();
            }
        }

        private static class MapComponent_Cache<T> where T : MapComponent
        {
            public static int count;
            public static T[] comps = new T[16];

            public static void Add(T comp)
            {
                if (count == comps.Length)
                {
                    Expand();
                }
                comps[count++] = comp;
            }

            public static void Remove(Map map)
            {
                for (int i = 0; i < count; i++)
                {
                    if (comps[i].map == map)
                    {
                        comps[i] = null;
                        for (int j = i + 1; j < count; j++)
                        {
                            comps[j - 1] = comps[j];
                        }
                        break;
                    }
                }
                count--;
            }

            public static void Clear()
            {
                count = 0;
                comps = new T[16];
            }

            private static void Expand(int targetSize = -1)
            {
                if (targetSize == -1)
                {
                    targetSize = comps.Length * 2;
                }
                T[] temp = new T[targetSize];
                Array.Copy(comps, temp, comps.Length);
                for (int i = 0; i < comps.Length; i++)
                {
                    comps[i] = null;
                }
                comps = temp;
            }
        }

        private static class WorldComponent_Cache<T> where T : WorldComponent
        {
            private static T value;

            public static T Comp
            {
                get
                {
                    if (value == null)
                    {
                        value = Find.World.GetComponent<T>();
                    }
                    return value;
                }
            }

            public static void Clear()
            {
                value = null;
            }
        }

        private static class GameComponent_Cache<T> where T : GameComponent
        {
            private static T value;

            public static T Comp
            {
                get
                {
                    if (value == null)
                    {
                        value = Current.Game.GetComponent<T>();
                    }
                    return value;
                }
            }

            public static void Clear()
            {
                value = null;
            }
        }
    }
}
