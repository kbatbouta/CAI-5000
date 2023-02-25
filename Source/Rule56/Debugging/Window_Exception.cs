using System;
using System.Diagnostics;
using CombatAI.Gui;
using UnityEngine;
using Verse;
using GUIUtility = CombatAI.Gui.GUIUtility;
namespace CombatAI
{
    public class Window_Exception : Window
    {
        private readonly Listing_Collapsible collapsible;
        private readonly Exception           exception;
        private readonly string              report;
        private readonly StackTrace          trace;

        public Window_Exception(Exception exception, StackTrace trace, string report)
        {
            drawShadow     = true;
            forcePause     = true;
            layer          = WindowLayer.Super;
            draggable      = false;
            collapsible    = new Listing_Collapsible();
            this.exception = exception;
            this.trace     = trace;
            this.report    = report.NullOrEmpty() ? exception.Message : report;
        }

        public override Vector2 InitialSize
        {
            get
            {
                Vector2 vec = new Vector2();
                vec.x = Mathf.RoundToInt(Maths.Max(UI.screenWidth * 0.45f, 500));
                vec.y = Mathf.RoundToInt(Maths.Max(UI.screenHeight * 0.45f, 400));
                return vec;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect bot = inRect.BottomPartPixels(60);
            Rect top = inRect.TopPartPixels(inRect.height - 60);
            collapsible.Expanded = true;
            collapsible.Begin(top, "IMPORTANT: CAI-5000 Has encountered an <color=red>Error</color>");
            try
            {
                collapsible.Label($"Error type:<color=yellow>\t{typeof(Exception)}</color>");
                collapsible.Label($"{report}");
                collapsible.Line(1);
                collapsible.Label("Trace:", fontStyle: FontStyle.Bold);
                for (int i = 1; i < trace.FrameCount; i++)
                {
                    StackFrame frame = trace.GetFrame(i);
                    if (typeof(Root).IsAssignableFrom(frame.GetMethod().GetType()))
                    {
                        break;
                    }
                    collapsible.Gap(2);
                    collapsible.Label($"<color=red>{string.Format("{0,2}", i)}.strace</color>: {frame.GetMethod().DeclaringType}/{frame.GetMethod().ReflectedType}:{frame.GetMethod().Name}", fontSize: GUIFontSize.Smaller, fontStyle: FontStyle.Normal);
                    Type t = frame.GetMethod().GetType();
                    collapsible.Label($"<color=red>{string.Format("{0,2}", i)}.source.2</color>: assembly ({t.Assembly})", fontSize: GUIFontSize.Tiny);
                }
                collapsible.Line(1);
                collapsible.Gap();
            }
            catch (Exception er)
            {
                Log.Error(er.ToString());
            }
            collapsible.End(ref top);
            Widgets.TextArea(bot.TopPartPixels(40), "Please report this to CAI-5000 discord https://discord.gg/ftCjYB7jDe or on github https://github.com/kbatbouta/CAI-5000/issues/new", true);
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                if (Widgets.ButtonText(bot.BottomPartPixels(20).LeftHalf(), "Close"))
                {
                    Close();
                }
                GUI.color = Color.red;
                if (Widgets.ButtonText(bot.BottomPartPixels(20).RightHalf(), "Disable Pop-ups and Close"))
                {
                    ExceptionUtility.enabled = false;
                    Close();
                }
            });
        }
    }
}
