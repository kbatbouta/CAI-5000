using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CombatAI.Comps;
using CombatAI.Gui;
using CombatAI.Patches;
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
        private Listing_Collapsible collapsible_dutyTest;
        private float               viewRatio1;
        private float               viewRatio2;
        private bool                dragging1;
        private bool                dragging2;
        private JobLog              selectedLog;
        private Vector2             scorllPos;
        
        public ThingComp_CombatAI comp;

        public Window_JobLogs(ThingComp_CombatAI comp)
        {
            this.collapsible         = new Listing_Collapsible();
            this.collapsible_dutyTest = new Listing_Collapsible();
            this.viewRatio1          = 0.5f;
            this.viewRatio2          = 0.8f;
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
            get => new Vector2(1000, 600);
        }

        public Pawn Pawn
        {
            get => comp.selPawn;
        }
        
        public List<JobLog> Logs
        {
            get => comp.jobLogs;
        }

        public static void ShowTutorial()
        {
            HyperTextDef[] pages = new HyperTextDef[]
            {
                CombatAI_HyperTextDefOf.CombatAI_DevJobTutorial1,
                CombatAI_HyperTextDefOf.CombatAI_DevJobTutorial2,
                CombatAI_HyperTextDefOf.CombatAI_DevJobTutorial3,
                CombatAI_HyperTextDefOf.CombatAI_DevJobTutorial4,
            };
            Window_Slides slides = new Window_Slides(pages, forcePause:true, skippable: false);
            Find.WindowStack.Add(slides);
        }


        public override void DoWindowContents(Rect inRect)
        {
            Rect right   = inRect.RightPart(1 - viewRatio2);
            Rect left    = inRect.LeftPart(viewRatio2);
            Rect barRect = right.LeftPartPixels(18);
            right.xMin += 18;
            Event current          = Event.current;
            bool  mouseOverDragBar = Mouse.IsOver(barRect);
            if (current.type == EventType.MouseDown && current.button == 0 && mouseOverDragBar)
            {
                dragging2 = true;
                current.Use();
            }
            if (dragging2)
            {
                viewRatio2 = Mathf.Clamp((current.mousePosition.x - inRect.xMin) / (inRect.xMax - inRect.xMin), 0.6f, 0.9f);
            }
            if (current.type == EventType.MouseUp && current.button == 0 && dragging2)
            {
                dragging2 = false;
                current.Use();
            }
            DrawDragBarVertical(barRect);
            if (!(comp.parent?.Destroyed ?? true) && comp.parent.Spawned)
            {
                DoTestContents(right);
            }
            DoJobLogContents(left);
        }

        private void DoTestContents(Rect inRect)
        {
            Pawn pawn = comp.selPawn;
            if (pawn == null)
            {
                return;
            }
            this.collapsible_dutyTest.Expanded = true;
            this.collapsible_dutyTest.Begin(inRect, "Test tools", drawInfo:false, drawIcon: false);
            this.collapsible_dutyTest.Label("Test suite");
            this.collapsible_dutyTest.Gap(2);
            if (ButtonText(collapsible_dutyTest, "Assault colony duty"))
            {
                foreach (Pawn other in Find.Selector.SelectedPawns)
                {
                    other.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
                Messages.Message($"Success: Assaulting colony", MessageTypeDefOf.CautionInput);
            }
            if (ButtonText(collapsible_dutyTest, "Defend position"))
            {
                
                Find.Targeter.BeginTargeting(new TargetingParameters()
                {
                    canTargetAnimals   = false,
                    canTargetBuildings = false,
                    canTargetCorpses   = false,
                    canTargetHumans    = false,
                    canTargetSelf      = false,
                    canTargetMechs     = false,
                    canTargetLocations = true,
                }, info =>
                {
                    if (info.Cell.IsValid)
                    {
                        foreach (Pawn other in Find.Selector.SelectedPawns)
                        {
                            other.mindState.duty = new PawnDuty(DutyDefOf.Defend, info);
                        }
                        Messages.Message($"Success: Defending current position", MessageTypeDefOf.CautionInput);
                    }
                });
            }
            if (ButtonText(collapsible_dutyTest, "Hunt down enemy"))
            {
                foreach (Pawn other in Find.Selector.SelectedPawns)
                {
                    other.mindState.duty = new PawnDuty(DutyDefOf.HuntEnemiesIndividual);
                }
                Messages.Message($"Success: Hunting enemies individuals", MessageTypeDefOf.CautionInput);
            }
            if (ButtonText(collapsible_dutyTest, "Escort"))
            {
                Find.Targeter.BeginTargeting(new TargetingParameters()
                {
                    canTargetAnimals = true,
                    canTargetBuildings = false,
                    canTargetCorpses =  false,
                    canTargetHumans = true,
                    canTargetSelf = false,
                    canTargetLocations = false,
                    canTargetMechs = false,
                }, info =>
                {
                    if (info.Thing is Pawn escortee)
                    {
                        foreach (Pawn other in Find.Selector.SelectedPawns)
                        {
                            other.mindState.duty = new PawnDuty(DutyDefOf.Defend, escortee);
                        }
                        Messages.Message($"Success: Escorting {escortee}", MessageTypeDefOf.CautionInput);
                    }
                });
            }
            this.collapsible_dutyTest.Line(1);
            if (ButtonText(collapsible_dutyTest, "Flash pathfinding to"))
            {
                Find.Targeter.BeginTargeting(new TargetingParameters()
                {
                    canTargetAnimals   = false,
                    canTargetBuildings = false,
                    canTargetCorpses   = false,
                    canTargetHumans    = false,
                    canTargetSelf      = false,
                    canTargetMechs     = false,
                    canTargetLocations = true,
                }, info =>
                {
                    if (info.Cell.IsValid)
                    {
                        PathFinder_Patch.FlashSearch = true;
                        try
                        {
                            PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, info, pawn, PathEndMode.OnCell, null);
                            if (path is { Found: true })
                            {
                                path.ReleaseToPool();
                            }
                        }
                        catch (Exception er)
                        {
                            Log.Error(er.ToString());
                        }
                        finally
                        {
                            PathFinder_Patch.FlashSearch = false;
                        }
                    }
                });
            }
            
            this.collapsible_dutyTest.End(ref inRect);
        }
        
        private void DoJobLogContents(Rect inRect)
        {
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                GUIFont.Font = GUIFontSize.Tiny;
                GUIFont.CurFontStyle.fontStyle = FontStyle.Bold;
                if (Find.Selector.SelectedPawns.Count == 0)
                {
                    string message = $"WARNING: No pawn selected or the previously selected pawn died!";
                    Widgets.DrawBoxSolid(inRect.TopPartPixels(20).LeftPartPixels(message.GetWidthCached() + 20), Color.red);
                    Widgets.Label(inRect.TopPartPixels(20), message);
                }
                else
                {
                    Widgets.Label(inRect.TopPartPixels(20), $"Viewing job logs for <color=green>{comp.parent}</color>");
                }
                GUIFont.Font                   = GUIFontSize.Tiny;
                GUIFont.CurFontStyle.fontStyle = FontStyle.Normal;
                if (Widgets.ButtonText(inRect.TopPartPixels(18).RightPartPixels(175).LeftPartPixels(175), "Open Job Log Tutorial"))
                {
                    ShowTutorial();
                }
                GUI.color = Color.green;
                if (Widgets.ButtonText(inRect.TopPartPixels(18).RightPartPixels(350).LeftPartPixels(175), "Copy short report to clipboard") && comp.jobLogs.Count > 0)
                {
                    StringBuilder builder = new StringBuilder();
                    int           limit   = Maths.Min(comp.jobLogs.Count, 10);
                    builder.AppendFormat("{0} jobs copied", limit);
                    builder.AppendLine("------------------------------------------------------");
                    for (int i = 0; i < limit; i++)
                    {
                        builder.Append(comp.jobLogs[i].ToString());
                        if (i < limit - 1)
                        {
                            builder.AppendLine();
                            builder.AppendLine("------------------------------------------------------");
                            builder.AppendLine();
                        }
                    }
                    UnityEngine.GUIUtility.systemCopyBuffer = builder.ToString();
                    Messages.Message("Short report copied to clipboard", MessageTypeDefOf.CautionInput);
                }
            });
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
            inRect.yMin += 20;
            Rect header = inRect.TopPartPixels(22);
            Widgets.DrawMenuSection(header);
            header.xMin += 10;
            CombatAI.Gui.GUIUtility.Row(header, new List<Action<Rect>>() 
            {
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "Job".Fit(rect));
                },
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "ID".Fit(rect));
                },
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "Duty".Fit(rect));
                },
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "ThinkTrace.First".Fit(rect));
                },
                (rect) =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "ThinkTrace.Lasts".Fit(rect));
                },
                (rect) =>
                {
                    Widgets.Label(rect, "Timestamp".Fit(rect));
                }
            }, false);
            inRect.yMin += 25;
            CombatAI.Gui.GUIUtility.ScrollView(selectedLog != null ? inRect.TopPart(viewRatio1) : inRect, ref scorllPos, Logs, GetHeight, DrawJobLog);
            if (selectedLog != null)
            {
                Rect  botRect          = inRect.BottomPart(1 - viewRatio1);
                Rect  barRect          = botRect.TopPartPixels(18);
                botRect.yMin += 18;
                Event current          = Event.current;
                bool  mouseOverDragBar = Mouse.IsOver(barRect);
                if (current.type == EventType.MouseDown && current.button == 0 && mouseOverDragBar)
                {
                    dragging1  = true;
                    current.Use();
                }
                if (dragging1)
                {
                    viewRatio1 = Mathf.Clamp((current.mousePosition.y - inRect.yMin) / (inRect.yMax - inRect.yMin), 0.2f, 0.8f);
                }
                if (current.type == EventType.MouseUp && current.button == 0 && dragging1)
                {
                    dragging1  = false;
                    current.Use();
                }
                DrawDragBarHorizontal(barRect);
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
            this.collapsible.Lambda(20, (rect) =>
            {
                if (Widgets.ButtonText(rect.LeftPartPixels(150), "Copy job data to clipboard"))
                {
                    UnityEngine.GUIUtility.systemCopyBuffer = selectedLog.ToString();
                    Messages.Message("Job info copied to clipboard", MessageTypeDefOf.CautionInput);
                } 
            });
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
        
        private void DrawDragBarHorizontal(Rect inRect)
        {
            if (Mouse.IsOver(inRect))
            {
                Widgets.DrawHighlight(inRect);
            }
            inRect = inRect.ContractedBy(1);
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                inRect.yMin += inRect.height / 2;
                Widgets.DrawLine(new Vector2(inRect.xMin, inRect.yMin), new Vector2(inRect.xMax, inRect.yMin), Widgets.MenuSectionBGBorderColor, 1);
            });
        }
        
        private void DrawDragBarVertical(Rect inRect)
        {
            if (Mouse.IsOver(inRect))
            {
                Widgets.DrawHighlight(inRect);
            }
            inRect = inRect.ContractedBy(1);
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                inRect.xMin += inRect.width / 2;
                Widgets.DrawLine(new Vector2(inRect.xMin, inRect.yMin), new Vector2(inRect.xMin, inRect.yMax), Widgets.MenuSectionBGBorderColor, 1);
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
                    Widgets.Label(rect, jobLog.job.Fit(rect));
                },
                (rect) =>
                {
                    Widgets.Label(rect, $"{jobLog.id}".Fit(rect));
                },
                (rect) =>
                {
                    Widgets.Label(rect, jobLog.duty.Fit(rect));
                },
                (rect) =>
                {
                    Widgets.Label(rect, (jobLog.thinknode.NullOrEmpty() ? "unknown" : jobLog.thinknode.First()).Fit(rect));
                },
                (rect) =>
                {
                    Widgets.Label(rect, (jobLog.thinknode.NullOrEmpty() ? "unknown" : jobLog.thinknode.Last()).Fit(rect));
                },
                (rect) =>
                {
                    Widgets.Label(rect, $"{Math.Round((GenTicks.TicksGame - jobLog.timestamp) / 60f, 0)} seconds ago".Fit(rect));
                }
            }, false, false);
        }

        private float GetHeight(JobLog jobLog)
        {
            return 20;
        }

        private static bool ButtonText(Listing_Collapsible collapsible, string text)
        {
            bool result = false;
            collapsible.Lambda(20, rect =>
            {
                GUI.color =  Color.yellow;
                rect.xMin += 5;
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                result = Widgets.ButtonText(rect, text, false, overrideTextAnchor:TextAnchor.MiddleLeft);
            });
            return result;
        }
    }
}
