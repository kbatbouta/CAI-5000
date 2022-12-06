using System;
using System.Collections.Generic;
using CombatAI.Gui;
using HarmonyLib;
using UnityEngine;
using Verse;
using static Verse.GenThreading;

namespace CombatAI
{
	public class CombatAIMod : Mod
	{
		public CombatAIMod(ModContentPack contentPack) : base(contentPack)
		{
			Finder.Harmony = new Harmony("Krkr.Rule56");
			Finder.Harmony.PatchAll();
			Finder.Mod = this;
			Finder.Settings = GetSettings<Settings>();
			if (Finder.Settings == null) Finder.Settings = new Settings();
			LongEventHandler.QueueLongEvent(ArmorUtility.Initialize, "CombatAI.Preparing", false, null);
			LongEventHandler.QueueLongEvent(CompatibilityManager.Initialize, "CombatAI.Preparing", false, null);
		}

		public override string SettingsCategory()
		{
			return R.Keyed.CombatAI;
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
			Finder.Settings.Write();
		}

		/*  ----------------------
		 *  
		 *  ---- Settings Gui ----
		 *          
		 */

		private void FillCollapsible_Basic(Listing_Collapsible collapsible)
		{
			if (Mod_CE.active)
			{
				collapsible.Lambda(20, (rect) =>
				{
					Widgets.DrawBoxSolid(rect, Color.white);
					rect = rect.ContractedBy(1);
					Widgets.DrawBoxSolid(rect, !Finder.Settings.LeanCE_Enabled ? Color.yellow : Color.green);
					rect.xMin += 5;
					GUI.color = Color.black;
					Text.CurFontStyle.fontStyle = FontStyle.Bold;
					Widgets.Label(rect,
						!Finder.Settings.LeanCE_Enabled
							? "Combat Extended Detected! PLEASE ENABLE CE PROFILE!"
							: "Combat Extended Detected!");
				}, useMargins: false);
				collapsible.Line(1);
			}

			collapsible.CheckboxLabeled(R.Keyed.CombatAI_Settings_Basic_CELean, ref Finder.Settings.LeanCE_Enabled);
			collapsible.Line(1);
			collapsible.CheckboxLabeled(R.Keyed.CombatAI_Settings_Basic_PerformanceOpt,
				ref Finder.Settings.PerformanceOpt_Enabled, R.Keyed.CombatAI_Settings_Basic_PerformanceOpt_Description);
			collapsible.Line(1);
			collapsible.CheckboxLabeled(R.Keyed.CombatAI_Settings_Basic_KillBoxKiller,
				ref Finder.Settings.Pather_KillboxKiller);
			collapsible.CheckboxLabeled(R.Keyed.CombatAI_Settings_Basic_Pather, ref Finder.Settings.Pather_Enabled);
			collapsible.CheckboxLabeled(R.Keyed.CombatAI_Settings_Basic_Caster, ref Finder.Settings.Caster_Enabled);
			collapsible.CheckboxLabeled(R.Keyed.CombatAI_Settings_Basic_Targeter, ref Finder.Settings.Targeter_Enabled);
			collapsible.Line(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Basic_DestWeight);
			collapsible.Gap(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Basic_DestWeight_Description);
			collapsible.Label(R.Keyed.CombatAI_Settings_Basic_DestWeight_Warning, fontStyle: FontStyle.Bold);
			collapsible.Gap(1);
			collapsible.Lambda(25, (rect) =>
			{
				var color = Finder.Settings.Pathfinding_DestWeight < 0.75f ? "red" : "while";
				var extra = Finder.Settings.Pathfinding_DestWeight < 0.75f
					? " <color=yellow>WILL IMPACT PERFORMANCE</color>"
					: "";
				Text.CurFontStyle.fontStyle = FontStyle.Bold;
				Finder.Settings.Pathfinding_DestWeight = Widgets.HorizontalSlider(rect,
					Finder.Settings.Pathfinding_DestWeight, 0.95f, Finder.Settings.AdvancedUser ? 0.3f : 0.65f, true,
					$"<color={color}>{Math.Round(Finder.Settings.Pathfinding_DestWeight * 100f, 1)}%</color>{extra}");
			}, useMargins: true);
		}

		private void FillCollapsible_FogOfWar(Listing_Collapsible collapsible)
		{
			collapsible.CheckboxLabeled(R.Keyed.CombatAI_Settings_Basic_FogOfWar_Enable,
				ref Finder.Settings.FogOfWar_Enabled);
			if (Finder.Settings.FogOfWar_Enabled)
			{
				collapsible.Line(1);

				collapsible.Label(R.Keyed.CombatAI_Settings_Basic_FogOfWar_Density);
				collapsible.Lambda(25,
					(rect) =>
					{
						Finder.Settings.FogOfWar_FogColor = Widgets.HorizontalSlider(rect,
							Finder.Settings.FogOfWar_FogColor, 0.0f, 1.0f, false,
							R.Keyed.CombatAI_Settings_Basic_FogOfWar_Density_Readouts.Formatted(
								Finder.Settings.FogOfWar_FogColor.ToString()));
					}, useMargins: true);

				collapsible.Line(1);

				collapsible.Label(R.Keyed.CombatAI_Settings_Basic_FogOfWar_RangeMul);
				collapsible.Lambda(25,
					(rect) =>
					{
						Finder.Settings.FogOfWar_RangeMultiplier = Widgets.HorizontalSlider(rect,
							Finder.Settings.FogOfWar_RangeMultiplier, 0.75f, 2.0f, false,
							R.Keyed.CombatAI_Settings_Basic_FogOfWar_RangeMul_Readouts.Formatted(
								Finder.Settings.FogOfWar_RangeMultiplier.ToString()));
					}, useMargins: true);

				collapsible.Line(1);

				collapsible.Label(R.Keyed.CombatAI_Settings_Basic_FogOfWar_FadeMul);
				collapsible.Lambda(25,
					(rect) =>
					{
						Finder.Settings.FogOfWar_RangeFadeMultiplier = Widgets.HorizontalSlider(rect,
							Finder.Settings.FogOfWar_RangeFadeMultiplier, 0.0f, 1.0f, false,
							R.Keyed.CombatAI_Settings_Basic_FogOfWar_FadeMul_Readouts.Formatted(
								Finder.Settings.FogOfWar_RangeFadeMultiplier.ToString()));
					}, useMargins: true);
			}
		}


		private void FillCollapsible_Performance(Listing_Collapsible collapsible)
		{
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Description);
			/*
			 * ----------------------
			 */
			collapsible.Line(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Sight_Performance_FrienldiesAndEnemies);
			collapsible.Gap(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Frequency.Formatted(
				60f / (Finder.Settings.SightSettings_FriendliesAndRaiders.buckets *
				       Finder.Settings.SightSettings_FriendliesAndRaiders.interval)));
			collapsible.Lambda(25,
				(rect) =>
				{
					Finder.Settings.SightSettings_FriendliesAndRaiders.interval = (int)Widgets.HorizontalSlider(rect,
						Finder.Settings.SightSettings_FriendliesAndRaiders.interval, 1, 20, false,
						R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Interval.Formatted(Finder.Settings
							.SightSettings_FriendliesAndRaiders.interval));
				}, useMargins: true);
			if (Current.ProgramState != ProgramState.Playing)
				collapsible.Lambda(25,
					(rect) =>
					{
						Finder.Settings.SightSettings_FriendliesAndRaiders.buckets = (int)Widgets.HorizontalSlider(rect,
							Finder.Settings.SightSettings_FriendliesAndRaiders.buckets, 1, 20, false,
							R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Buckets.Formatted(
								Finder.Settings.SightSettings_FriendliesAndRaiders.buckets));
					}, useMargins: true);
			collapsible.Lambda(25, (rect) =>
			{
				Finder.Settings.SightSettings_FriendliesAndRaiders.carryLimit = (int)Widgets.HorizontalSlider(rect,
					Finder.Settings.SightSettings_FriendliesAndRaiders.carryLimit, 4, 16, false,
					R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_CarryLimit.Formatted(Finder.Settings
						.SightSettings_FriendliesAndRaiders.carryLimit));
				if (Mouse.IsOver(rect))
					TooltipHandler.TipRegion(rect,
						R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_CarryLimit_Description);
			}, useMargins: true);
			/*
			 * ----------------------
			 */
			collapsible.Line(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Sight_Performance_MechsAndInsect);
			collapsible.Gap(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Frequency.Formatted(
				60f / (Finder.Settings.SightSettings_MechsAndInsects.buckets *
				       Finder.Settings.SightSettings_MechsAndInsects.interval)));
			collapsible.Lambda(25,
				(rect) =>
				{
					Finder.Settings.SightSettings_MechsAndInsects.interval = (int)Widgets.HorizontalSlider(rect,
						Finder.Settings.SightSettings_MechsAndInsects.interval, 1, 20, false,
						R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Interval.Formatted(Finder.Settings
							.SightSettings_MechsAndInsects.interval));
				}, useMargins: true);
			if (Current.ProgramState != ProgramState.Playing)
				collapsible.Lambda(25,
					(rect) =>
					{
						Finder.Settings.SightSettings_MechsAndInsects.buckets = (int)Widgets.HorizontalSlider(rect,
							Finder.Settings.SightSettings_MechsAndInsects.buckets, 1, 20, false,
							R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Buckets.Formatted(
								Finder.Settings.SightSettings_MechsAndInsects.buckets));
					}, useMargins: true);
			collapsible.Lambda(25, (rect) =>
			{
				Finder.Settings.SightSettings_MechsAndInsects.carryLimit = (int)Widgets.HorizontalSlider(rect,
					Finder.Settings.SightSettings_MechsAndInsects.carryLimit, 4, 16, false,
					R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_CarryLimit.Formatted(Finder.Settings
						.SightSettings_MechsAndInsects.carryLimit));
				if (Mouse.IsOver(rect))
					TooltipHandler.TipRegion(rect,
						R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_CarryLimit_Description);
			}, useMargins: true);
			/*
			 * ----------------------
			 */
			collapsible.Line(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Sight_Performance_WildLife);
			collapsible.Gap(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Frequency.Formatted(
				60f / (Finder.Settings.SightSettings_Wildlife.buckets *
				       Finder.Settings.SightSettings_Wildlife.interval)));
			collapsible.Lambda(25,
				(rect) =>
				{
					Finder.Settings.SightSettings_Wildlife.interval = (int)Widgets.HorizontalSlider(rect,
						Finder.Settings.SightSettings_Wildlife.interval, 1, 20, false,
						R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Interval.Formatted(Finder.Settings
							.SightSettings_Wildlife.interval));
				}, useMargins: true);
			if (Current.ProgramState != ProgramState.Playing)
				collapsible.Lambda(25,
					(rect) =>
					{
						Finder.Settings.SightSettings_Wildlife.buckets = (int)Widgets.HorizontalSlider(rect,
							Finder.Settings.SightSettings_Wildlife.buckets, 1, 20, false,
							R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Buckets.Formatted(
								Finder.Settings.SightSettings_Wildlife.buckets));
					}, useMargins: true);
			collapsible.Lambda(25, (rect) =>
			{
				Finder.Settings.SightSettings_Wildlife.carryLimit = (int)Widgets.HorizontalSlider(rect,
					Finder.Settings.SightSettings_Wildlife.carryLimit, 4, 16, false,
					R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_CarryLimit.Formatted(Finder.Settings
						.SightSettings_Wildlife.carryLimit));
				if (Mouse.IsOver(rect))
					TooltipHandler.TipRegion(rect,
						R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_CarryLimit_Description);
			}, useMargins: true);
			/*
			 * ----------------------
			 */
			collapsible.Line(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Turrets);
			collapsible.Gap(1);
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Frequency.Formatted(
				(60f / ((float)Finder.Settings.SightSettings_SettlementTurrets.buckets *
				        Finder.Settings.SightSettings_SettlementTurrets.interval)).ToString()));
			collapsible.Lambda(25,
				(rect) =>
				{
					Finder.Settings.SightSettings_SettlementTurrets.interval = (int)Widgets.HorizontalSlider(rect,
						Finder.Settings.SightSettings_SettlementTurrets.interval, 1, 20, false,
						R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Interval.Formatted(Finder.Settings
							.SightSettings_SettlementTurrets.interval));
				}, useMargins: true);
			if (Current.ProgramState != ProgramState.Playing)
				collapsible.Lambda(25,
					(rect) =>
					{
						Finder.Settings.SightSettings_SettlementTurrets.buckets = (int)Widgets.HorizontalSlider(rect,
							Finder.Settings.SightSettings_SettlementTurrets.buckets, 1, 20, false,
							R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_Buckets.Formatted(
								Finder.Settings.SightSettings_SettlementTurrets.buckets));
					}, useMargins: true);
			collapsible.Lambda(25, (rect) =>
			{
				Finder.Settings.SightSettings_SettlementTurrets.carryLimit = (int)Widgets.HorizontalSlider(rect,
					Finder.Settings.SightSettings_SettlementTurrets.carryLimit, 4, 16, false,
					R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_CarryLimit.Formatted(Finder.Settings
						.SightSettings_SettlementTurrets.carryLimit));
				if (Mouse.IsOver(rect))
					TooltipHandler.TipRegion(rect,
						R.Keyed.CombatAI_Settings_Advance_Sight_Performance_Readouts_CarryLimit_Description);
			}, useMargins: true);
		}

		private void FillCollapsible_Advance(Listing_Collapsible collapsible)
		{
			collapsible.Label(R.Keyed.CombatAI_Settings_Advance_Warning);
			collapsible.Line(1);
			collapsible.CheckboxLabeled(R.Keyed.CombatAI_Settings_Advance_Enable, ref Finder.Settings.AdvancedUser);
			//if (Finder.Settings.AdvancedUser)
			//{
			//    collapsible.Lambda(25, (rect) =>
			//    {
			//        Finder.Settings.Advanced_SightThreadIdleSleepTimeMS = (int)Widgets.HorizontalSlider(rect, Finder.Settings.Advanced_SightThreadIdleSleepTimeMS, 1, 10, false, $"<color=red>Sight worker thread</color> idle sleep time MS {(int)(Finder.Settings.Advanced_SightThreadIdleSleepTimeMS)}");
			//    }, useMargins: true);
			//}
		}

		private void FillCollapsible_Debugging(Listing_Collapsible collapsible)
		{
			collapsible.CheckboxLabeled(R.Keyed.CombatAI_Settings_Debugging_Enable, ref Finder.Settings.Debug);
			collapsible.CheckboxLabeled("Draw sight grid", ref Finder.Settings.Debug_DrawShadowCasts);
			collapsible.CheckboxLabeled("Draw sight vector field", ref Finder.Settings.Debug_DrawShadowCastsVectors);
			collapsible.CheckboxLabeled("Draw threat (pawn armor vs enemy)", ref Finder.Settings.Debug_DrawThreatCasts);
			collapsible.CheckboxLabeled("Draw proximity grid", ref Finder.Settings.Debug_DrawAvoidanceGrid_Proximity);
			collapsible.CheckboxLabeled("Draw danger grid", ref Finder.Settings.Debug_DrawAvoidanceGrid_Danger);
			collapsible.CheckboxLabeled("Debug things tracker", ref Finder.Settings.Debug_DebugThingsTracker);
			collapsible.CheckboxLabeled("Debug validate sight <color=red>EXTREMELY BAD FOR PERFORMANCE</color>",
				ref Finder.Settings.Debug_ValidateSight);
			collapsible.Line(1);
			collapsible.Label("DO NOT USE THESE AT ALL - FOR DEVS ONLY - WILL FILL YOUR DISK IN 10 MINUTES");
			collapsible.CheckboxLabeled(
				"Dump data <color=red>EXTREMELY BAD FOR PERFORMANCE THIS IS DEAD DON'T TOUCH IT</color>",
				ref Finder.Settings.Debug_DebugDumpData);
		}

		/*  -----------------------
		 *  
		 *  -- Settings Gui core --
		 *          
		 */

		private bool collapsibleGroupInited = false;
		private Listing_Collapsible collapsible_basic = new Listing_Collapsible(true, true);
		private Listing_Collapsible collapsible_fog = new Listing_Collapsible(false, true);
		private Listing_Collapsible collapsible_advance = new Listing_Collapsible(true, true);
		private Listing_Collapsible collapsible_performance = new Listing_Collapsible(true, true);
		private Listing_Collapsible collapsible_debug = new Listing_Collapsible(true, true);

		private Listing_Collapsible.Group_Collapsible collapsible_groupLeft =
			new Listing_Collapsible.Group_Collapsible();

		private Listing_Collapsible.Group_Collapsible collapsible_groupRight =
			new Listing_Collapsible.Group_Collapsible();

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);
			if (!collapsibleGroupInited)
			{
				collapsibleGroupInited = true;
				collapsible_debug.Group = collapsible_groupRight;
				collapsible_groupRight.Register(collapsible_debug);

				collapsible_performance.Group = collapsible_groupRight;
				collapsible_groupRight.Register(collapsible_performance);

				collapsible_fog.Group = collapsible_groupLeft;
				collapsible_groupLeft.Register(collapsible_fog);

				collapsible_basic.Group = collapsible_groupLeft;
				collapsible_groupLeft.Register(collapsible_basic);
				collapsible_basic.Expanded = true;
			}

			var rectLeft = inRect.LeftHalf();
			// -------------
			// Left  section
			//
			// general settings
			// collapsible_basic.Expanded = true;
			if (!collapsible_groupLeft.AnyExpanded) collapsible_basic.Expanded = true;
			collapsible_basic.Begin(rectLeft, R.Keyed.CombatAI_Settings_Basic);
			FillCollapsible_Basic(collapsible_basic);
			collapsible_basic.End(ref rectLeft);
			rectLeft.yMin += 5;

			collapsible_fog.Begin(rectLeft, R.Keyed.CombatAI_Settings_Basic_FogOfWar);
			FillCollapsible_FogOfWar(collapsible_fog);
			collapsible_fog.End(ref rectLeft);
			rectLeft.yMin += 5;

			// -------------
			//
			// Right section
			var rectRight = inRect.RightHalf();
			rectRight.xMin += 5;
			collapsible_advance.Expanded = true;
			collapsible_advance.Begin(rectRight, R.Keyed.CombatAI_Settings_Advance);
			FillCollapsible_Advance(collapsible_advance);
			collapsible_advance.End(ref rectRight);
			rectRight.yMin += 5;

			// debug settings
			if (Finder.Settings.AdvancedUser)
			{
				collapsible_performance.Begin(rectRight, R.Keyed.CombatAI_Settings_Advance_Sight_Performance);
				FillCollapsible_Performance(collapsible_performance);
				collapsible_performance.End(ref rectRight);
				rectRight.yMin += 5;

				collapsible_debug.Begin(rectRight, R.Keyed.CombatAI_Settings_Debugging);
				FillCollapsible_Debugging(collapsible_debug);
				collapsible_debug.End(ref rectRight);
				rectRight.yMin += 5;
			}

			WriteSettings();
		}
	}
}