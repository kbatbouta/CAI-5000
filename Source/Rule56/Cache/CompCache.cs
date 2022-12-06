using System;
using Verse;
using System.Collections.Generic;
using System.Drawing;
using HarmonyLib;
using System.Reflection;
using RimWorld.Planet;
using static HarmonyLib.Code;
using UnityEngine;

namespace CombatAI
{
	public static class CompCache
	{
		private static List<Action<Map>> mapComponents_removeActions = null;

		private static Dictionary<Type, Action<ThingWithComps>> thingComps_removeActions =
			new Dictionary<Type, Action<ThingWithComps>>();

		private static Dictionary<int, List<Action<ThingWithComps>>> thingComps_removeActionsByThing =
			new Dictionary<int, List<Action<ThingWithComps>>>();

		private static class ThingComp_Cache<T> where T : ThingComp
		{
			public static readonly Dictionary<int, T> compsById = new Dictionary<int, T>(1024);

			public static void Remove(ThingWithComps thing)
			{
				if (compsById.ContainsKey(thing.thingIDNumber)) compsById.Remove(thing.thingIDNumber);
			}

			public static void Clear()
			{
				compsById.Clear();
			}
		}

		private static class MapComponent_Cache<T> where T : MapComponent
		{
			public static int count = 0;
			public static T[] comps = new T[16];

			public static void Add(T comp)
			{
				if (count == comps.Length) Expand();
				comps[count++] = comp;
			}

			public static void Remove(Map map)
			{
				for (var i = 0; i < count; i++)
					if (comps[i].map == map)
					{
						comps[i] = null;
						for (var j = i + 1; j < count; j++) comps[j - 1] = comps[j];
						break;
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
				if (targetSize == -1) targetSize = comps.Length * 2;
				var temp = new T[targetSize];
				Array.Copy(comps, temp, comps.Length);
				for (var i = 0; i < comps.Length; i++) comps[i] = null;
				comps = temp;
			}
		}

		private static class WorldComponent_Cache<T> where T : WorldComponent
		{
			private static T value = null;

			public static T Comp
			{
				get
				{
					if (value == null) value = Find.World.GetComponent<T>();
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
			private static T value = null;

			public static T Comp
			{
				get
				{
					if (value == null) value = Current.Game.GetComponent<T>();
					return value;
				}
			}

			public static void Clear()
			{
				value = null;
			}
		}

		public static T GetComp_Fast<T>(this Thing thing, bool allowFallback = true) where T : ThingComp
		{
			if (thing is ThingWithComps thingWithComps) return GetComp_Fast<T>(thingWithComps, allowFallback);
			return null;
		}

		public static T GetComp_Fast<T>(this ThingWithComps thing, bool allowFallback = true) where T : ThingComp
		{
			if (ThingComp_Cache<T>.compsById.TryGetValue(thing.thingIDNumber, out var value)) return value;
			if (thing.comps == null) return null;
			if (!thingComps_removeActions.TryGetValue(typeof(T), out var removeAction))
			{
				var info = AccessTools.Method(typeof(ThingComp_Cache<>).MakeGenericType(new Type[] { typeof(T) }),
					"Remove");
				thingComps_removeActions.Add(typeof(T), removeAction = (t) => { info.Invoke(null, new[] { t }); });
			}

			if (!thingComps_removeActionsByThing.TryGetValue(thing.thingIDNumber, out var actions))
				thingComps_removeActionsByThing[thing.thingIDNumber] = actions = new List<Action<ThingWithComps>>();
			for (var i = 0; i < thing.comps.Count; i++)
			{
				var props = thing.comps[i].props;
				if (props != null && props.compClass == typeof(T))
				{
					actions.Add(removeAction);
					return ThingComp_Cache<T>.compsById[thing.thingIDNumber] = thing.comps[i] as T;
				}
			}

			if (allowFallback)
				for (var i = 0; i < thing.comps.Count; i++)
					if (thing.comps[i] is T comp)
					{
						actions.Add(removeAction);
						return ThingComp_Cache<T>.compsById[thing.thingIDNumber] = comp;
					}

			return null;
		}

		public static T GetComp_Fast<T>(this Map map) where T : MapComponent
		{
			T comp;
			var count = MapComponent_Cache<T>.count;
			for (var i = 0; i < count; i++)
				if ((comp = MapComponent_Cache<T>.comps[i]).map == map)
					return comp;
			if ((comp = map.GetComponent<T>()) != null) MapComponent_Cache<T>.Add(comp);
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
			if (thingComps_removeActionsByThing.TryGetValue(thing.thingIDNumber, out var actions))
			{
				for (var i = 0; i < actions.Count; i++) actions[i](thing);
				actions.Clear();
				thingComps_removeActionsByThing.Remove(thing.thingIDNumber);
			}
		}


		public static void Notify_MapRemoved(Map map)
		{
			if (mapComponents_removeActions == null)
			{
				mapComponents_removeActions = new List<Action<Map>>();

				foreach (var compType in typeof(MapComponent).AllSubclassesNonAbstract())
				{
					var info = AccessTools.Method(typeof(MapComponent_Cache<>).MakeGenericType(new Type[] { compType }),
						"Remove");
					mapComponents_removeActions.Add((t) => { info.Invoke(null, new[] { t }); });
				}
			}

			for (var i = 0; i < mapComponents_removeActions.Count; i++) mapComponents_removeActions[i](map);
		}

		public static void ClearCaches()
		{
			foreach (var compType in typeof(MapComponent).AllSubclassesNonAbstract())
			{
				var info = AccessTools.Method(typeof(MapComponent_Cache<>).MakeGenericType(new Type[] { compType }),
					"Clear");
				info.Invoke(null, new object[0]);
			}

			foreach (var compType in typeof(ThingComp).AllSubclassesNonAbstract())
			{
				var info = AccessTools.Method(typeof(ThingComp_Cache<>).MakeGenericType(new Type[] { compType }),
					"Clear");
				info.Invoke(null, new object[0]);
			}

			foreach (var compType in typeof(GameComponent).AllSubclassesNonAbstract())
			{
				var info = AccessTools.Method(typeof(GameComponent_Cache<>).MakeGenericType(new Type[] { compType }),
					"Clear");
				info.Invoke(null, new object[0]);
			}

			foreach (var compType in typeof(WorldComponent).AllSubclassesNonAbstract())
			{
				var info = AccessTools.Method(typeof(WorldComponent_Cache<>).MakeGenericType(new Type[] { compType }),
					"Clear");
				info.Invoke(null, new object[0]);
			}
		}
	}
}