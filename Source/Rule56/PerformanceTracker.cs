using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace CombatAI
{
	public class PerformanceTracker : GameComponent
	{
		/// <summary>
		/// The mod perfomance level. A float ranging from 0 to 1.0f. the lower the value the worse is the current performance.
		/// Used to help maintian sustained performance.
		/// </summary>
		private float performance = 1.0f;
		public float Performance
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Finder.Settings.PerformanceOpt_Enabled ? performance : 1f;
		}

		private bool tpsCriticallyLow = false;
		public bool TpsCriticallyLow
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Finder.Settings.PerformanceOpt_Enabled && tpsCriticallyLow;
		}

		private float     avgTickTimeMs = 0.016f;
		private Stopwatch stopwatch     = new Stopwatch();
		/// <summary>
		/// Rolling avrage frame time.
		/// </summary>
		public float AvgTickTimeMs
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => avgTickTimeMs;
		}
		/// <summary>
		/// The expected ticks per second from the rolling average.
		/// </summary>
		public float Tps
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Maths.Min(1f / AvgTickTimeMs, TargetTps);
		}
		/// <summary>
		/// The expected tps deficit.
		/// </summary>
		public float TpsDeficit
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Maths.Max(TargetTps - Tps, 0f);
		}
		/// <summary>
		/// The current target Tps.
		/// </summary>
		public float TargetTps
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Maths.Min(Find.TickManager.TickRateMultiplier * 60f, 90f);
		}

		public PerformanceTracker(Game game)
		{
			Finder.Performance = this;
		}

		public override void GameComponentTick()
		{
			base.GameComponentTick();
			if (!stopwatch.IsRunning)
			{
				stopwatch.Start();
				return;
			}
			float deltaT = (float)stopwatch.ElapsedTicks / Stopwatch.Frequency;
			stopwatch.Restart();
			if (deltaT > 0.5f)
			{
				stopwatch.Stop();
				return;
			}
			avgTickTimeMs    = (avgTickTimeMs * 44f + deltaT) / 45f;
			performance      = Mathf.Clamp01((Tps < 55f ? 0.5f : 1.0f) * (1f - TpsDeficit / (TargetTps + 1)));
			tpsCriticallyLow = Performance < 0.667f && Tps <= 50f;
		}

		public override void GameComponentOnGUI()
		{
			base.GameComponentOnGUI();
			if (Finder.Settings.Debug)
			{
				string lowMsg = TpsCriticallyLow ? "<color=red>LOW</color>" : "NROMAL";
				Widgets.DrawBoxSolid(new Rect(Vector2.zero, new Vector2(100, 5)), Color.gray);
				Widgets.DrawBoxSolid(new Rect(Vector2.zero, new Vector2(100 * Performance, 5)), TpsCriticallyLow ? Color.yellow : Color.blue);
				Widgets.Label(new Rect(Vector2.zero, new Vector2(100, 25)), $"{Tps}\t{lowMsg}\t{Math.Round(Performance, 2)}");
			}
		}
	}
}
