using System;
using System.Collections.Generic;
using CombatAI.R;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI.Gui
{
    public class Window_QuickSetup : Window
    {
        private readonly Listing_Collapsible collapsible;
        private readonly Listing_Collapsible collapsible_fogOfWar;
        private          Difficulty          difficulty;
        private          bool                presetSelected;

        public Window_QuickSetup()
        {
            drawShadow           = true;
            forcePause           = true;
            layer                = WindowLayer.Super;
            draggable            = false;
            collapsible          = new Listing_Collapsible();
            collapsible_fogOfWar = new Listing_Collapsible();
        }

        public override Vector2 InitialSize
        {
            get
            {
                Vector2 vec = new Vector2();
                vec.x = Mathf.RoundToInt(Maths.Max(UI.screenWidth * 0.35f, 450));
                vec.y = Mathf.RoundToInt(Maths.Max(UI.screenHeight * 0.45f, 400));
                return vec;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect   = inRect.TopPartPixels(60);
            Rect optionsRect = inRect.BottomPartPixels(inRect.height - 60);
            optionsRect.height -= 60;
            inRect             =  inRect.BottomPartPixels(60);
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                GUIFont.Anchor = TextAnchor.MiddleCenter;
                GUIFont.Font   = GUIFontSize.Medium;
                Widgets.Label(titleRect.TopHalf(), Keyed.CombatAI_Quick_Welcome);
                GUIFont.Font   = GUIFontSize.Small;
                GUIFont.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(titleRect.BottomHalf(), Keyed.CombatAI_Quick_Welcome_Description);
            });
            collapsible.Expanded = true;
            collapsible.Begin(optionsRect, Keyed.CombatAI_Quick_QuickSetup, drawIcon: false);
            collapsible.Label(Keyed.CombatAI_Quick_Difficulty);
            collapsible.Lambda(25, rect =>
            {
                DoDifficultySettings(rect);
            });
            collapsible.Line(1);
            collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_PerformanceOpt, ref Finder.Settings.PerformanceOpt_Enabled);
            collapsible.Line(1);
            collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_KillBoxKiller, ref Finder.Settings.Pather_KillboxKiller);
            collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_RandomizedPersonality, ref Finder.Settings.Personalities_Enabled);
            collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_Sprinting, ref Finder.Settings.Enable_Sprinting, Keyed.CombatAI_Settings_Basic_Sprinting_Description);
            collapsible.End(ref optionsRect);
            optionsRect.yMin              += 5;
            collapsible_fogOfWar.Expanded =  true;
            collapsible_fogOfWar.Begin(optionsRect, Keyed.CombatAI_Settings_Basic_FogOfWar, drawIcon: false);
            FillCollapsible_FogOfWar(collapsible_fogOfWar);
            collapsible_fogOfWar.End(ref optionsRect);
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                if (presetSelected)
                {
                    DoSelected(inRect.TopHalf());
                }
                Text.Font = GameFont.Small;
                GUI.color = presetSelected ? Color.green : Color.red;
                if (Widgets.ButtonText(inRect.BottomHalf(), Keyed.CombatAI_Apply))
                {
                    if (!presetSelected)
                    {
                        Messages.Message(Keyed.CombatAI_Quick_Difficulty, MessageTypeDefOf.RejectInput);
                    }
                    else
                    {
                        Finder.Settings.FinishedQuickSetup = true;
                        Finder.Settings.Write();
                        Close();
                    }
                }
            });
        }

        private void DoSelected(Rect rect)
        {
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                rect.yMin += 2;
                rect.yMax -= 2;
                Widgets.DrawBox(rect.ContractedBy(1));
                rect.xMin      += 10;
                GUIFont.Anchor =  TextAnchor.MiddleLeft;
                GUIFont.Font   =  GUIFontSize.Small;
                Widgets.Label(rect, difficulty < Difficulty.DeathWish ? Keyed.CombatAI_Quick_Difficulty_Selected.Formatted(difficulty.ToString()) : Keyed.CombatAI_Quick_Difficulty_Selected_Warning.Formatted(difficulty.ToString()));
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
                        presetSelected = true;
                        DifficultyUtility.SetDifficulty(difficulty = Difficulty.Easy);
                        Messages.Message(Keyed.CombatAI_Settings_Basic_Presets_Applied + " " + Keyed.CombatAI_Settings_Basic_Presets_Easy, MessageTypeDefOf.TaskCompletion);
                    }
                },
                rect =>
                {
                    if (Widgets.ButtonText(rect, Keyed.CombatAI_Settings_Basic_Presets_Normal))
                    {
                        presetSelected = true;
                        DifficultyUtility.SetDifficulty(difficulty = Difficulty.Normal);
                        Messages.Message(Keyed.CombatAI_Settings_Basic_Presets_Applied + " " + Keyed.CombatAI_Settings_Basic_Presets_Normal, MessageTypeDefOf.TaskCompletion);
                    }
                },
                rect =>
                {
                    if (Widgets.ButtonText(rect, Keyed.CombatAI_Settings_Basic_Presets_Hard))
                    {
                        presetSelected = true;
                        DifficultyUtility.SetDifficulty(difficulty = Difficulty.Hard);
                        Messages.Message(Keyed.CombatAI_Settings_Basic_Presets_Applied + " " + Keyed.CombatAI_Settings_Basic_Presets_Hard, MessageTypeDefOf.TaskCompletion);
                    }
                },
                rect =>
                {
                    GUI.color = Color.red;
                    if (Widgets.ButtonText(rect, Keyed.CombatAI_Settings_Basic_Presets_Deathwish))
                    {
                        presetSelected = true;
                        DifficultyUtility.SetDifficulty(difficulty = Difficulty.DeathWish);
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
            }
        }

        private float HorizontalSlider_NewTemp(Rect rect, float val, float min, float max, bool middleAlinment, string label, float roundTo = -1)
        {
            Widgets.HorizontalSlider(rect, ref val, new FloatRange(min, max), label, roundTo);
            return val;
        }
    }
}
