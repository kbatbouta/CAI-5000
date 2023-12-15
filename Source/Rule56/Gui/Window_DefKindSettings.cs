using System.Collections.Generic;
using System.Linq;
using CombatAI.R;
using UnityEngine;
using Verse;

namespace CombatAI.Gui
{
	public class Window_DefKindSettings : Window
	{
		private          Vector2                                                   pos;
		private          (ThingDef, PawnKindDef, Settings.DefKindAISettings)?      cur;
		private readonly Listing_Collapsible                                       collapsible;
		private readonly List<(ThingDef, PawnKindDef, Settings.DefKindAISettings)> defs;

		public Window_DefKindSettings()
		{
			drawShadow  = true;
			forcePause  = true;
			layer       = WindowLayer.Super;
			draggable   = false;
			collapsible = new Listing_Collapsible();
			defs        = new List<(ThingDef, PawnKindDef, Settings.DefKindAISettings)>();
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(d => d.race != null))
			{
				foreach (PawnKindDef kind in DefDatabase<PawnKindDef>.AllDefs.Where(d => d.race == def))
				{
					defs.Add((def, kind, Finder.Settings.GetDefKindSettings(def, kind)));
				}
				defs.Add((def, null, Finder.Settings.GetDefKindSettings(def, null)));
			}
		}

		public override Vector2 InitialSize
		{
			get
			{
				Vector2 vec = new Vector2();
				vec.x = Mathf.RoundToInt(Maths.Max(UI.screenWidth * 0.55f, 450));
				vec.y = Mathf.RoundToInt(Maths.Max(UI.screenHeight * 0.55f, 400));
				return vec;
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			collapsible.Expanded = true;
			collapsible.Begin(inRect, Keyed.CombatAI_DefKindSettings_Title, true, false);
			collapsible.Label(Keyed.CombatAI_DefKindSettings_Description);
			collapsible.Line(1);
			if (cur != null)
			{
				ThingDef    def  = cur?.Item1;
				PawnKindDef kind = cur?.Item2;
				collapsible.Label(Keyed.CombatAI_DefKindSettings_Selected + ":" + def.label + " " + (kind?.label ?? string.Empty), null, false, true, GUIFontSize.Smaller);
				collapsible.Line(1);
				Settings.DefKindAISettings settings = cur?.Item3;
				collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_Pather, ref settings.Pather_Enabled);
				collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_KillBoxKiller, ref settings.Pather_KillboxKiller, disabled: !settings.Pather_Enabled);
				collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_Temperature, ref settings.Temperature_Enabled, disabled: !settings.Pather_Enabled);
				collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_Reaction, ref settings.React_Enabled);
				collapsible.CheckboxLabeled(Keyed.CombatAI_Settings_Basic_Retreat, ref settings.Retreat_Enabled);
			}
			collapsible.Line(1);
			collapsible.Lambda(18, (rect) =>
			{
				Widgets.Label(rect.LeftPart(0.5f), "Def");
				Widgets.Label(rect.RightPart(0.5f), "Kind");
			}, false, true);
			collapsible.End(ref inRect);
			GUIUtility.ScrollView(inRect, ref pos, defs, (item) => 20, (rect, tuple) =>
			{
				ThingDef    def  = tuple.Item1;
				PawnKindDef kind = tuple.Item2;
				if (tuple == cur)
				{
					Widgets.DrawHighlight(rect);
				}
				Widgets.DefLabelWithIcon(rect.LeftPart(0.5f), def);
				if (kind != null)
				{
					Widgets.DefLabelWithIcon(rect.RightPart(0.5f), kind);
				}
				if (Widgets.ButtonInvisible(rect, false))
				{
					cur = tuple;
				}
			});
		}
	}
}
