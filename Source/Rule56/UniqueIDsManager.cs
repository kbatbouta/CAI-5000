using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;
namespace CombatAI
{
	[StaticConstructorOnStartup]
	public class UniqueIDsManager : GameComponent
	{
		private static readonly List<Action> scribeActions = new List<Action>();

		static UniqueIDsManager()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			foreach (Type t in typeof(UniqueIDsManager).Assembly.GetTypes().Where(t => typeof(ILoadReferenceable).IsAssignableFrom(t)))
			{
				RuntimeHelpers.RunClassConstructor(typeof(ILDatabase<>).MakeGenericType(t).TypeHandle);
				if (Finder.Settings.Debug)
				{
					Log.Message($"<color=red>ISMA</color> Success! Created references for {t.FullName}");
				}
			}
			stopwatch.Stop();
			Log.Message($"<color=blue>ISMA</color> Init for UniqueIDsManager took {(float)stopwatch.ElapsedTicks / Stopwatch.Frequency} ");
		}

		public UniqueIDsManager(Game game)
		{
		}

		public static int GetNextID<T>() where T : ILoadReferenceable, IExposable
		{
			return ILDatabase<T>.Counter++;
		}

		public override void ExposeData()
		{
			foreach (Action action in scribeActions)
			{
				action.Invoke();
			}
		}

		private static class ILDatabase<T> where T : ILoadReferenceable
		{
			private static readonly string name;
			private static          int    counter;

			static ILDatabase()
			{
				name = $"CombatAI.UniqueIDsManager.{typeof(T).Namespace}.{typeof(T).Name}";
				scribeActions.Add(ExposeInnerData);
			}

			public static int Counter
			{
				get => counter;
				set => counter = value;
			}

			private static void ExposeInnerData()
			{
				Scribe_Values.Look(ref counter, name, forceSave: true, defaultValue: 0);
			}
		}
	}
}
