using System;
using System.Collections.Generic;
using System.Linq;
using CombatAI.Comps;
using CombatAI.Gui;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using GUIUtility = CombatAI.Gui.GUIUtility;
namespace CombatAI
{
    public class Window_JobLogs : Window
    {
        private Map                 map;
        private Listing_Collapsible collapsible;
        private float               viewRatio;
        private bool                dragging;
        private JobLog              selectedLog;
        private Vector2             scorllPos;
        
        public ThingComp_CombatAI comp;

        public Window_JobLogs(ThingComp_CombatAI comp)
        {
            this.collapsible         = new Listing_Collapsible();
            this.viewRatio           = 0.5f;
            this.comp                = comp;
            this.map                 = comp.parent.Map;
            this.resizeable          = true;
            this.resizer             = new WindowResizer();
            this.draggable           = true;
            this.doCloseX            = true;
            this.preventCameraMotion = false;
        }

        public override Vector2 InitialSize
        {
            get => new Vector2(800, 600);
        }

        public Pawn Pawn
        {
            get => comp.selPawn;
        }
        
        public List<JobLog> Logs
        {
            get => comp.jobLogs;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            if (Find.Selector.SelectedPawns.Count == 1)
            {
                var temp = Find.Selector.SelectedPawns[0].GetComp_Fast<ThingComp_CombatAI>();
                if (temp != comp)
                {
                    comp        = temp;
                    map         = comp.parent.Map;
                    selectedLog = null;
                }
            }
            inRect.yMin += 10;
            Rect header = inRect.TopPartPixels(22);
            Widgets.DrawMenuSection(header);
            header.xMin += 10;
            CombatAI.Gui.GUIUtility.Row(header, new List<Action<Rect>>() 
            {
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "Job");
                },
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "ID");
                },
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "Duty");
                },
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "ThinkTrace.First");
                },
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "ThinkTrace.Lasts");
                },
                (rect) =>
                {
                    Widgets.Label(rect, "Timestamp");
                }
            }, false);
            inRect.yMin += 25;
            CombatAI.Gui.GUIUtility.ScrollView(selectedLog != null ? inRect.TopPart(viewRatio) : inRect, ref scorllPos, Logs, GetHeight, DrawJobLog);
            if (selectedLog != null)
            {
                Rect  botRect          = inRect.BottomPart(1 - viewRatio);
                Rect  barRect          = botRect.TopPartPixels(18);
                botRect.yMin += 18;
                Event current          = Event.current;
                bool  mouseOverDragBar = Mouse.IsOver(barRect);
                if (current.type == EventType.MouseDown && current.button == 0 && mouseOverDragBar)
                {
                    dragging  = true;
                    current.Use();
                }
                if (dragging)
                {
                    viewRatio = Mathf.Clamp((current.mousePosition.y - inRect.yMin) / (inRect.yMax - inRect.yMin), 0.2f, 0.8f);
                }
                if (current.type == EventType.MouseUp && current.button == 0 && dragging)
                {
                    dragging  = false;
                    current.Use();
                }
                DrawDragBar(barRect);
                DrawSelection(botRect);
            }
        }

        private void DrawSelection(Rect inRect)
        {
            Widgets.DrawBoxSolidWithOutline(inRect, this.collapsible.CollapsibleBGColor, Widgets.MenuSectionBGBorderColor);
            inRect                                    = inRect.ContractedBy(1);
            this.collapsible.CollapsibleBGBorderColor = this.collapsible.CollapsibleBGColor;
            this.collapsible.Expanded                 = true;
            this.collapsible.Begin(inRect, $"Details: {selectedLog.job}", false,false);
            this.collapsible.Label($"JobDef.defName:\t{selectedLog.job}");
            this.collapsible.Line(1);
            this.collapsible.Label($"DutyDef.defName:\t{selectedLog.duty}");
            this.collapsible.Line(1);
            this.collapsible.Lambda(40, (rect) =>
            {
                rect.xMin += 20;
                Rect top = rect.TopHalf();
                Rect bot = rect.BottomHalf();
                if (Mouse.IsOver(top))
                {
                    GlobalTargetInfo target = new GlobalTargetInfo(selectedLog.origin, map);
                    TargetHighlighter.Highlight(target, true, false, true);
                    Widgets.DrawHighlight(top);
                    if (Widgets.ButtonInvisible(top))
                    {
                        CameraJumper.TryJump(target);
                        map.debugDrawer.FlashCell(selectedLog.origin, 0.01f, "s", 120);
                    }
                }
                Widgets.Label(top, $"origin:\t\t{selectedLog.origin}");
                if (selectedLog.destination.IsValid && Mouse.IsOver(bot))
                {
                    GlobalTargetInfo target = new GlobalTargetInfo(selectedLog.destination, map);
                    TargetHighlighter.Highlight(target, true, false, true);
                    Widgets.DrawHighlight(bot);
                    if (Widgets.ButtonInvisible(bot))
                    {
                        CameraJumper.TryJump(target);
                        map.debugDrawer.FlashCell(selectedLog.destination, 0.99f, "d", 120);
                    }
                }
                Widgets.Label(bot,$"destination:\t{selectedLog.destination}");
            });
            this.collapsible.Line(1);
            foreach (string s in selectedLog.thinknode)
            {
                this.collapsible.Label(s);
            }
            this.collapsible.Line(1);
            foreach (string s in selectedLog.stacktrace)
            {
                this.collapsible.Label(s);
            }
            this.collapsible.End(ref inRect);
        }
        
        private void DrawDragBar(Rect inRect)
        {
            if (Mouse.IsOver(inRect))
            {
                Widgets.DrawHighlight(inRect);
            }
            inRect = inRect.ContractedBy(1);
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                inRect.yMin += 4;
                Widgets.DrawLine(new Vector2(inRect.xMin, inRect.yMin), new Vector2(inRect.xMax, inRect.yMin), Widgets.MenuSectionBGBorderColor, 1);
                inRect.yMin += 8;
                Widgets.DrawLine(new Vector2(inRect.xMin, inRect.yMin), new Vector2(inRect.xMax, inRect.yMin), Widgets.MenuSectionBGBorderColor, 1);
            });
        }
        
        private void DrawJobLog(Rect inRect, JobLog jobLog)
        {
            if (Widgets.ButtonInvisible(inRect))
            {
                selectedLog = jobLog;
            }
            if (selectedLog == jobLog)
            {
                Widgets.DrawHighlight(inRect);
            }
            Gui.GUIUtility.Row(inRect, new List<Action<Rect>>()
            {
                (rect) =>
                {
                    rect.xMin += 5;
                    Widgets.Label(rect, jobLog.job);
                },
                (rect) =>
                {
                    Widgets.Label(rect, $"{jobLog.id}");
                },
                (rect) =>
                {
                    Widgets.Label(rect, jobLog.duty);
                },
                (rect) =>
                {
                    Widgets.Label(rect, jobLog.thinknode.NullOrEmpty() ? "unknown" : jobLog.thinknode.First());
                },
                (rect) =>
                {
                    Widgets.Label(rect, jobLog.thinknode.NullOrEmpty() ? "unknown" : jobLog.thinknode.Last());
                },
                (rect) =>
                {
                    Widgets.Label(rect, $"{Math.Round((GenTicks.TicksGame - jobLog.timestamp) / 60f, 0)} seconds ago");
                }
            }, false, false);
        }
        
        private float GetHeight(JobLog jobLog)
        {
            return 20;
        }
    }
}
