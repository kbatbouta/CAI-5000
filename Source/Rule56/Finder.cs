using System;
using HarmonyLib;
using UnityEngine;

namespace CombatAI
{
	public static class Finder
	{
		public static Harmony Harmony;

		public static CombatAIMod Mod;

		public static Settings Settings;

		public static PerformanceTracker Performance;

		public static float P75 => Maths.Max(Performance.Performance, 0.75f);

		public static float P50 => Maths.Max(Performance.Performance, 0.50f);

		public static int MainThreadId;
	}
}