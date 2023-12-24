using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly Listing_Collapsible collapsible;
        private readonly Listing_Collapsible collapsible_dutyTest;

        public  ThingComp_CombatAI comp;
        private bool               dragging1;
        private bool               dragging2;
        private Map                map;
        private Vector2            scorllPos;
        private JobLog             selectedLog;
        private float              viewRatio1;
        private float              viewRatio2;

        public Window_JobLogs(ThingComp_CombatAI comp)
        {
            collapsible          = new Listing_Collapsible();
            collapsible_dutyTest = new Listing_Collapsible();
            viewRatio1           = 0.5f;
            viewRatio2           = 0.8f;
            this.comp            = comp;
            map                  = comp.parent.Map;
            resizeable           = true;
            resizer              = new WindowResizer();
            draggable            = true;
            doCloseX             = true;
            preventCameraMotion  = false;
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
            HyperTextDef[] pages =
            {
                CombatAI_HyperTextDefOf.CombatAI_DevJobTutorial1, CombatAI_HyperTextDefOf.CombatAI_DevJobTutorial2, CombatAI_HyperTextDefOf.CombatAI_DevJobTutorial3, CombatAI_HyperTextDefOf.CombatAI_DevJobTutorial4
            };
            Window_Slides slides = new Window_Slides(pages, true, false);
            Find.WindowStack.Add(slides);
        }


        public override void DoWindowContents(Rect inRect)
        {
            GUIUtility.ExecuteSafeGUIAction(() =>
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
            });
        }

        private void DoTestContents(Rect inRect)
        {
            Pawn pawn = comp.selPawn;
            if (pawn == null)
            {
                return;
            }
            collapsible_dutyTest.Expanded = true;
            collapsible_dutyTest.Begin(inRect, "Test tools", false, false);
            collapsible_dutyTest.Label("Test suite");
            collapsible_dutyTest.Gap(2);
            if (ButtonText(collapsible_dutyTest, "Assault colony duty"))
            {
                foreach (Pawn other in Find.Selector.SelectedPawns)
                {
                    other.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
                Messages.Message("Success: Assaulting colony", MessageTypeDefOf.CautionInput);
            }
            if (ButtonText(collapsible_dutyTest, "Defend position"))
            {

                Find.Targeter.BeginTargeting(new TargetingParameters
                {
                    canTargetAnimals   = false,
                    canTargetBuildings = false,
                    canTargetCorpses   = false,
                    canTargetHumans    = false,
                    canTargetSelf      = false,
                    canTargetMechs     = false,
                    canTargetLocations = true
                }, info =>
                {
                    if (info.Cell.IsValid)
                    {
                        foreach (Pawn other in Find.Selector.SelectedPawns)
                        {
                            other.mindState.duty = new PawnDuty(DutyDefOf.Defend, info);
                        }
                        Messages.Message("Success: Defending current position", MessageTypeDefOf.CautionInput);
                    }
                });
            }
            if (ButtonText(collapsible_dutyTest, "Hunt down enemy"))
            {
                foreach (Pawn other in Find.Selector.SelectedPawns)
                {
                    other.mindState.duty = new PawnDuty(DutyDefOf.HuntEnemiesIndividual);
                }
                Messages.Message("Success: Hunting enemies individuals", MessageTypeDefOf.CautionInput);
            }
            if (ButtonText(collapsible_dutyTest, "Escort"))
            {
                Find.Targeter.BeginTargeting(new TargetingParameters
                {
                    canTargetAnimals   = true,
                    canTargetBuildings = false,
                    canTargetCorpses   = false,
                    canTargetHumans    = true,
                    canTargetSelf      = false,
                    canTargetLocations = false,
                    canTargetMechs     = false
                }, info =>
                {
                    if (info.Thing is Pawn escortee)
                    {
                        foreach (Pawn other in Find.Selector.SelectedPawns)
                        {
                            other.mindState.duty        = new PawnDuty(CombatAI_DutyDefOf.CombatAI_Escort, escortee);
                            other.mindState.duty.radius = 15;
                        }
                        Messages.Message($"Success: Escorting {escortee}", MessageTypeDefOf.CautionInput);
                    }
                });
            }
            collapsible_dutyTest.Line(1);
            if (ButtonText(collapsible_dutyTest, "Flash pathfinding to"))
            {
                Find.Targeter.BeginTargeting(new TargetingParameters
                {
                    canTargetAnimals   = false,
                    canTargetBuildings = false,
                    canTargetCorpses   = false,
                    canTargetHumans    = false,
                    canTargetSelf      = false,
                    canTargetMechs     = false,
                    canTargetLocations = true
                }, info =>
                {
                    if (info.Cell.IsValid)
                    {
                        PathFinder_Patch.FlashSearch = true;
                        try
                        {
                            PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, info.Cell, pawn);
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
            if (ButtonText(collapsible_dutyTest, "Flash sapper path to"))
            {
	            Find.Targeter.BeginTargeting(new TargetingParameters
	            {
		            canTargetAnimals   = false,
		            canTargetBuildings = false,
		            canTargetCorpses   = false,
		            canTargetHumans    = false,
		            canTargetSelf      = false,
		            canTargetMechs     = false,
		            canTargetLocations = true
	            }, info =>
	            {
		            if (info.Cell.IsValid)
		            {
			            PathFinder_Patch.FlashSapperPath = true;
			            try
			            {
				            PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, info.Cell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings));
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
				            PathFinder_Patch.FlashSapperPath = false;
			            }
		            }
	            });
            }
            if (ButtonText(collapsible_dutyTest, "Region-wise distance"))
            {
                Find.Targeter.BeginTargeting(new TargetingParameters
                {
                    canTargetAnimals   = false,
                    canTargetBuildings = false,
                    canTargetCorpses   = false,
                    canTargetHumans    = false,
                    canTargetSelf      = false,
                    canTargetMechs     = false,
                    canTargetLocations = true
                }, info =>
                {
                    if (info.Cell.IsValid)
                    {
                        Stopwatch stopwatch = new Stopwatch();
                        int       dist      = 0;
                        stopwatch.Start();
                        for (int i = 0; i < 128; i++)
                        {
                            dist = comp.parent.Position.HeuristicDistanceTo_RegionWise(info.Cell, map);
                        }
                        stopwatch.Stop();
                        float time = (float)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f / 128f;
                        Messages.Message($"Distance is {dist} regions, took {time} ms", MessageTypeDefOf.CautionInput);
                    }
                });
            }
            if (ButtonText(collapsible_dutyTest, "Cell-wise distance"))
            {
                Find.Targeter.BeginTargeting(new TargetingParameters
                {
                    canTargetAnimals   = false,
                    canTargetBuildings = false,
                    canTargetCorpses   = false,
                    canTargetHumans    = false,
                    canTargetSelf      = false,
                    canTargetMechs     = false,
                    canTargetLocations = true
                }, info =>
                {
                    if (info.Cell.IsValid)
                    {
                        Stopwatch stopwatch = new Stopwatch();
                        float     dist      = 0;
                        stopwatch.Start();
                        for (int i = 0; i < 128; i++)
                        {
                            dist = comp.parent.Position.HeuristicDistanceTo(info.Cell, map);
                        }
                        stopwatch.Stop();
                        float time = (float)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f / 128f;
                        Messages.Message($"Distance is {dist} cells, took {time} ms", MessageTypeDefOf.CautionInput);
                    }
                });
            }
            if (ButtonText(collapsible_dutyTest, "Reachability check"))
            {
                Pawn parentPawn = comp.selPawn;
                Find.Targeter.BeginTargeting(new TargetingParameters
                {
                    canTargetAnimals   = false,
                    canTargetBuildings = false,
                    canTargetCorpses   = false,
                    canTargetHumans    = false,
                    canTargetSelf      = false,
                    canTargetMechs     = false,
                    canTargetLocations = true,
                    validator = info =>
                    {
                        if (info.Cell.IsValid && info.Cell.InBounds(map))
                        {
                            string result = $"ByPawn={parentPawn.CanReach(info.Cell, PathEndMode.InteractionCell, Danger.Deadly, true, true, TraverseMode.ByPawn)}\n"
                                            + $"NoPassClosedDoors={parentPawn.CanReach(info.Cell, PathEndMode.InteractionCell, Danger.Deadly, true, true, TraverseMode.NoPassClosedDoors)}\n"
                                            + $"NoPassClosedDoorsOrWater={parentPawn.CanReach(info.Cell, PathEndMode.InteractionCell, Danger.Deadly, true, true, TraverseMode.NoPassClosedDoorsOrWater)}\n"
                                            + $"PassDoors={parentPawn.CanReach(info.Cell, PathEndMode.InteractionCell, Danger.Deadly, true, true, TraverseMode.PassDoors)}\n"
                                            + $"PassAllDestroyableThings={parentPawn.CanReach(info.Cell, PathEndMode.InteractionCell, Danger.Deadly, true, true, TraverseMode.PassAllDestroyableThings)}\n"
                                            + $"PassAllDestroyableThingsNotWater={parentPawn.CanReach(info.Cell, PathEndMode.InteractionCell, Danger.Deadly, true, true, TraverseMode.PassAllDestroyableThingsNotWater)}\n";
                            Log.Message(result);
                        }
                        return true;
                    },
                }, info =>
                {
                    return;
                });
            }
            if (ButtonText(collapsible_dutyTest, "End all jobs"))
            {
                foreach (Pawn other in Find.Selector.SelectedPawns)
                {
                    other.jobs.ClearQueuedJobs();
                    other.jobs.StopAll();
                }
                Messages.Message("Success: All jobs stopped", MessageTypeDefOf.CautionInput);
            }

            collapsible_dutyTest.End(ref inRect);
        }

        private void DoJobLogContents(Rect inRect)
        {
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                GUIFont.Font                   = GUIFontSize.Tiny;
                GUIFont.CurFontStyle.fontStyle = FontStyle.Bold;
                if (Find.Selector.SelectedPawns.Count == 0)
                {
                    string message = "WARNING: No pawn selected or the previously selected pawn died!";
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
                        builder.Append(comp.jobLogs[i]);
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
                ThingComp_CombatAI temp = Find.Selector.SelectedPawns[0].AI();
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
            GUIUtility.Row(header, new List<Action<Rect>>
            {
                rect =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "Job".Fit(rect));
                },
                rect =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "ID".Fit(rect));
                },
                rect =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "Duty".Fit(rect));
                },
                rect =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "ThinkTrace.First".Fit(rect));
                },
                rect =>
                {
                    GUIFont.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, "ThinkTrace.Lasts".Fit(rect));
                },
                rect =>
                {
                    Widgets.Label(rect, "Timestamp".Fit(rect));
                }
            }, false);
            inRect.yMin += 25;
            if (!Logs.NullOrEmpty())
            {
                GUIUtility.ScrollView(selectedLog != null ? inRect.TopPart(viewRatio1) : inRect, ref scorllPos, Logs, GetHeight, DrawJobLog);
            }
            if (selectedLog != null)
            {
                Rect botRect = inRect.BottomPart(1 - viewRatio1);
                Rect barRect = botRect.TopPartPixels(18);
                botRect.yMin += 18;
                Event current          = Event.current;
                bool  mouseOverDragBar = Mouse.IsOver(barRect);
                if (current.type == EventType.MouseDown && current.button == 0 && mouseOverDragBar)
                {
                    dragging1 = true;
                    current.Use();
                }
                if (dragging1)
                {
                    viewRatio1 = Mathf.Clamp((current.mousePosition.y - inRect.yMin) / (inRect.yMax - inRect.yMin), 0.2f, 0.8f);
                }
                if (current.type == EventType.MouseUp && current.button == 0 && dragging1)
                {
                    dragging1 = false;
                    current.Use();
                }
                DrawDragBarHorizontal(barRect);
                DrawSelection(botRect);
            }
        }

        private void DrawSelection(Rect inRect)
        {
            Widgets.DrawBoxSolidWithOutline(inRect, collapsible.CollapsibleBGColor, Widgets.MenuSectionBGBorderColor);
            inRect                               = inRect.ContractedBy(1);
            collapsible.CollapsibleBGBorderColor = collapsible.CollapsibleBGColor;
            collapsible.Expanded                 = true;
            collapsible.Begin(inRect, $"Details: {selectedLog.job}", false, false);
            collapsible.Lambda(20, rect =>
            {
                if (Widgets.ButtonText(rect.LeftPartPixels(150), "Copy job data to clipboard"))
                {
                    UnityEngine.GUIUtility.systemCopyBuffer = selectedLog.ToString();
                    Messages.Message("Job info copied to clipboard", MessageTypeDefOf.CautionInput);
                }
                if (selectedLog.path != null && Widgets.ButtonText(rect.LeftPartPixels(300).RightPartPixels(150), "Flash path"))
                { 
	                Messages.Message("Flashed path", MessageTypeDefOf.CautionInput);
	                map.debugDrawer.debugCells.Clear();
	                for (int i = 0; i < selectedLog.path.Count; i++)
	                {
		                map.debugDrawer.FlashCell(selectedLog.path[i],(float)i / (selectedLog.path.Count), $"{selectedLog.path.Count - i}");
	                }
	                
                }
                if (selectedLog.pathSapper != null && Widgets.ButtonText(rect.LeftPartPixels(450).RightPartPixels(150), "Flash sapper path"))
                {
	                Messages.Message("Flashed sapper path", MessageTypeDefOf.CautionInput);
	                map.debugDrawer.debugCells.Clear();
	                for (int i = 0; i < selectedLog.pathSapper.Count; i++)
	                {
		                map.debugDrawer.FlashCell(selectedLog.pathSapper[i],(float)i / (selectedLog.pathSapper.Count), $"{selectedLog.pathSapper.Count - i}");
	                }
                }
            });
            collapsible.Label($"JobDef.defName:\t{selectedLog.job}");
            collapsible.Line(1);
            collapsible.Label($"DutyDef.defName:\t{selectedLog.duty}");
            collapsible.Label($"Notes:\t{selectedLog.note}");
            collapsible.Line(1);
            collapsible.Lambda(40, rect =>
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
                Widgets.Label(bot, $"destination:\t{selectedLog.destination}");
            });
            collapsible.Line(1);
            foreach (string s in selectedLog.thinknode)
            {
                collapsible.Label(s);
            }
            collapsible.Line(1);
            foreach (string s in selectedLog.stacktrace)
            {
                collapsible.Label(s);
            }
            collapsible.End(ref inRect);
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
            GUIUtility.Row(inRect, new List<Action<Rect>>
            {
                rect =>
                {
                    rect.xMin += 5;
                    Widgets.Label(rect, jobLog.job.Fit(rect));
                },
                rect =>
                {
                    Widgets.Label(rect, $"{jobLog.id}".Fit(rect));
                },
                rect =>
                {
                    Widgets.Label(rect, jobLog.duty.Fit(rect));
                },
                rect =>
                {
                    string val = jobLog.thinknode.NullOrEmpty() ? "unknown" : jobLog.thinknode.First();
                    Widgets.Label(rect, val.Fit(rect));
                },
                rect =>
                {
                    string val = jobLog.thinknode.NullOrEmpty() ? "unknown" : jobLog.thinknode.Last();
                    Widgets.Label(rect, val.Fit(rect));
                },
                rect =>
                {
                    Widgets.Label(rect, $"{Math.Round((GenTicks.TicksGame - jobLog.timestamp) / 60f, 0)} seconds ago".Fit(rect));
                }
            }, false);
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
                result = Widgets.ButtonText(rect, text, false, overrideTextAnchor: TextAnchor.MiddleLeft);
            });
            return result;
        }
    }
}
