using System;
using System.Collections.Generic;
using CombatAI.Gui;
using CombatAI.R;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityEngine.Experimental.Playables;
using Verse;
using GUIUtility = CombatAI.Gui.GUIUtility;
namespace CombatAI
{
    public class Window_QuickSetup : Window
    {
        private readonly Listing_Collapsible collapsible;

        public Window_QuickSetup()
        {
            this.drawShadow  = true;
            this.forcePause  = true;
            this.layer       = WindowLayer.Super;
            this.draggable   = false;
            this.collapsible = new Listing_Collapsible();
        }

        public override Vector2 InitialSize
        {
            get
            {
                Vector2 vec = new Vector2();
                vec.x = Mathf.RoundToInt(Maths.Max(UI.screenWidth  * 0.35f, 450));
                vec.y = Mathf.RoundToInt(Maths.Max(UI.screenHeight * 0.45f, 400));
                return vec;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect = inRect.TopPartPixels(60);
            Rect optionsRect = inRect.BottomPartPixels(inRect.height - 60);
            optionsRect.height -= 25;
            inRect             =  inRect.BottomPartPixels(25);
            Gui.GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Gui.GUIFont.Anchor = TextAnchor.MiddleCenter;
                Gui.GUIFont.Font   = GUIFontSize.Medium;
                Widgets.Label(titleRect.TopHalf(), R.Keyed.CombatAI_Quick_Welcome);
                Gui.GUIFont.Font   = GUIFontSize.Small;
                Gui.GUIFont.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(titleRect.BottomHalf(), R.Keyed.CombatAI_Quick_Welcome_Description);
            });
            collapsible.Expanded = true;
            collapsible.Begin(optionsRect, R.Keyed.CombatAI_Quick_QuickSetup, drawIcon: false);
            collapsible.Label(R.Keyed.CombatAI_Quick_Difficulty);
            collapsible.Lambda(25, (rect) =>
            {
                DoDifficultySettings(rect);
            });
            collapsible.Line(1);
            FillCollapsible_FogOfWar(collapsible);
            collapsible.End(ref optionsRect);
            Gui.GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Text.Font = GameFont.Small;
                GUI.color = Color.green;
                if (Widgets.ButtonText(inRect.LeftHalf(), R.Keyed.CombatAI_Apply))
                {
                    Finder.Settings.FinishedQuickSetup = true;
                    Finder.Settings.Write();
                    Close();
                }
                GUI.color = Color.red;
                if (Widgets.ButtonText(inRect.RightHalf(), R.Keyed.CombatAI_Close))
                {
                    Close();
                }
            });
        }

        private void DoDifficultySettings(Rect inRect)
        {
            inRect.xMin  += 20;
            GUIFont.Font =  GUIFontSize.Small;
            GUIUtility.Row(inRect, new List<Action<Rect>>
            {
                rect =>
                {
                    GUI.color = Color.green;
                    if (Widgets.ButtonText(rect, Keyed.CombatAI_Settings_Basic_Presets_Easy))
                    {
                        DifficultyUtility.SetDifficulty(Difficulty.Easy);
                        Messages.Message(Keyed.CombatAI_Settings_Basic_Presets_Applied + " " + Keyed.CombatAI_Settings_Basic_Presets_Easy, MessageTypeDefOf.TaskCompletion);
                    }
                },
                rect =>
                {
                    if (Widgets.ButtonText(rect, Keyed.CombatAI_Settings_Basic_Presets_Normal))
                    {
                        DifficultyUtility.SetDifficulty(Difficulty.Normal);
                        Messages.Message(Keyed.CombatAI_Settings_Basic_Presets_Applied + " " + Keyed.CombatAI_Settings_Basic_Presets_Normal, MessageTypeDefOf.TaskCompletion);
                    }
                },
                rect =>
                {
                    if (Widgets.ButtonText(rect, Keyed.CombatAI_Settings_Basic_Presets_Hard))
                    {
                        DifficultyUtility.SetDifficulty(Difficulty.Hard);
                        Messages.Message(Keyed.CombatAI_Settings_Basic_Presets_Applied + " " + Keyed.CombatAI_Settings_Basic_Presets_Hard, MessageTypeDefOf.TaskCompletion);
                    }
                },
                rect =>
                {
                    GUI.color = Color.red;
                    if (Widgets.ButtonText(rect, Keyed.CombatAI_Settings_Basic_Presets_Deathwish))
                    {
                        DifficultyUtility.SetDifficulty(Difficulty.DeathWish);
                        Messages.Message(Keyed.CombatAI_Settings_Basic_PerformanceOpt_Warning, MessageTypeDefOf.CautionInput);
                        Messages.Message(Keyed.CombatAI_Settings_Basic_Presets_Applied + " " + Keyed.CombatAI_Settings_Basic_Presets_Deathwish, MessageTypeDefOf.TaskCompletion);
                    }
                }
            }, false);
        }
        
        private void FillCollapsible_FogOfWar(Listing_Collapsible collapsible)
		{
			collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_FogOfWar_Enable, ref Finder.Settings.FogOfWar_Enabled);

			if (Finder.Settings.FogOfWar_Enabled)
			{
                collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_FogOfWar_Animals, ref Finder.Settings.FogOfWar_Animals);
				collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_FogOfWar_Animals_SmartOnly, ref Finder.Settings.FogOfWar_AnimalsSmartOnly, disabled: !Finder.Settings.FogOfWar_Animals);
                collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_FogOfWar_Allies, ref Finder.Settings.FogOfWar_Allies);
				collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_FogOfWar_Turrets, ref Finder.Settings.FogOfWar_Turrets);
                
//				collapsible.Label(Keyed.CombatAI_Settings_Basic_FogOfWar_Density);
//				collapsible.Lambda(25, rect =>
//				{
//					Finder.Settings.FogOfWar_FogColor = HorizontalSlider_NewTemp(rect, Finder.Settings.FogOfWar_FogColor, 0.0f, 1.0f, false, Keyed.CombatAI_Settings_Basic_FogOfWar_Density_Readouts.Formatted(Finder.Settings.FogOfWar_FogColor.ToString()), 0.05f);
//				}, useMargins: true);
//                
//				collapsible.Label(Keyed.CombatAI_Settings_Basic_FogOfWar_RangeMul);
//				collapsible.Lambda(25, rect =>
//				{
//					Finder.Settings.FogOfWar_RangeMultiplier = HorizontalSlider_NewTemp(rect, Finder.Settings.FogOfWar_RangeMultiplier, 0.75f, 2.0f, false, Keyed.CombatAI_Settings_Basic_FogOfWar_RangeMul_Readouts.Formatted(Finder.Settings.FogOfWar_RangeMultiplier.ToString()), 0.05f);
//				}, useMargins: true);
//
//
//				collapsible.Label(Keyed.CombatAI_Settings_Basic_FogOfWar_FadeMul);
//				collapsible.Lambda(25, rect =>
//				{
//					Finder.Settings.FogOfWar_RangeFadeMultiplier = HorizontalSlider_NewTemp(rect, Finder.Settings.FogOfWar_RangeFadeMultiplier, 0.0f, 1.0f, false, Keyed.CombatAI_Settings_Basic_FogOfWar_FadeMul_Readouts.Formatted(Finder.Settings.FogOfWar_RangeFadeMultiplier.ToString()), 0.05f);
//				}, useMargins: true);
			}
        }
        
        private float HorizontalSlider_NewTemp(Rect rect, float val, float min, float max, bool middleAlinment, string label, float roundTo = -1)
        {
            Widgets.HorizontalSlider(rect, ref val, new FloatRange(min, max), label, roundTo);
            return val;
        }
    }
}
