using System;
using Verse;
using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace CombatAI
{
    public class Pawn_CustomDutyTracker : IExposable
    {
        public class CustomPawnDuty : IExposable
        {
            public PawnDuty duty;
            public int expireAfter;
            public int startAfter;
            public int expiresAt = -1;
            public int startsAt = -1;
            public bool failOnFocusDeath;
            public bool failOnFocusDowned;
            public bool failOnFocusDestroyed;
            public bool canFlee = true;
            public bool canExitMap = true;
            public int failOnDistanceToFocus;
            public DutyDef failOnFocusDutyNot;            

            public void ExposeData()
            {
                Scribe_Deep.Look(ref duty, "duty");
                Scribe_Values.Look(ref expireAfter, "expireAfter");
                Scribe_Values.Look(ref startAfter, "startAfter");
                Scribe_Values.Look(ref startsAt, "startsAt", -1);
                Scribe_Values.Look(ref expiresAt, "expiresAt", -1);
                Scribe_Values.Look(ref failOnDistanceToFocus, "failOnDistanceToFocus");
                Scribe_Values.Look(ref failOnFocusDeath, "failOnFocusDeath");
                Scribe_Values.Look(ref failOnFocusDowned, "failOnFocusDowned");
                Scribe_Values.Look(ref failOnFocusDestroyed, "failOnFocusDestroyed");
                Scribe_Values.Look(ref canFlee, "canFlee", true);
                Scribe_Values.Look(ref canExitMap, "canExitMap", true);
                Scribe_Defs.Look(ref failOnFocusDutyNot, "failOnFocusDutyNot");
            }
        }        

        public Pawn pawn;
        public List<CustomPawnDuty> queue = new List<CustomPawnDuty>();
        public CustomPawnDuty curCustomDuty;

        public DutyDef CurDutyDef
        {
            get => curCustomDuty?.duty?.def ?? pawn.mindState?.duty?.def ?? null;
        }        

        public Pawn_CustomDutyTracker()
        {
        }

        public Pawn_CustomDutyTracker(Pawn pawn)
        {
            this.pawn = pawn;            
        }

        public void TickRare()
        {            
            if (curCustomDuty != null)
            {
                if (curCustomDuty.expiresAt > 0 && curCustomDuty.expiresAt <= GenTicks.TicksGame)
                {
                    curCustomDuty = null;
					pawn.mindState.duty = null;
				}
                else if (curCustomDuty.failOnDistanceToFocus > 0 && (curCustomDuty.duty.focus.Cell.IsValid == false || curCustomDuty.duty.focus.Cell.DistanceToSquared(pawn.Position) > curCustomDuty.failOnDistanceToFocus * curCustomDuty.failOnDistanceToFocus))
                {
                    curCustomDuty = null;
					pawn.mindState.duty = null;
				}
                else if (curCustomDuty?.duty.focus.Thing != null)
                {
                    Thing focus = curCustomDuty.duty.focus.Thing;
                    if (curCustomDuty.failOnFocusDestroyed && (focus.Destroyed || !focus.Spawned))
                    {
                        curCustomDuty = null;
                        pawn.mindState.duty = null;						

					}
                    else if (!pawn.CanReach(focus, PathEndMode.InteractionCell, Danger.Unspecified, true, true))
                    {
                        curCustomDuty = null;
						pawn.mindState.duty = null;
					}
                    else if (curCustomDuty.failOnFocusDeath || curCustomDuty.failOnFocusDowned)
                    {
                        Pawn fpawn = focus as Pawn;
                        if (fpawn == null)
                        {
                            curCustomDuty = null;
							pawn.mindState.duty = null;
						}
                        else if ((curCustomDuty.failOnFocusDowned || curCustomDuty.failOnFocusDeath) && fpawn.Dead)
                        {
                            curCustomDuty = null;
							pawn.mindState.duty = null;
						}
                        else if (curCustomDuty.failOnFocusDowned && fpawn.Downed)
                        {
                            curCustomDuty = null;
							pawn.mindState.duty = null;
						}
                        else if (curCustomDuty.failOnFocusDutyNot != null && fpawn.mindState?.duty?.def != curCustomDuty.failOnFocusDutyNot)
                        {
                            curCustomDuty = null;
							pawn.mindState.duty = null;
						}
                    }
                }
            }
            if (curCustomDuty == null && queue.Count > 0)
            {
                while (queue.Count > 0)
                {
                    CustomPawnDuty next = queue[0];
                    if (next.startsAt > GenTicks.TicksGame)
                    {
                        break;               
                    }
                    if (next.expiresAt < 0 || next.expiresAt > GenTicks.TicksGame)
                    {
                        curCustomDuty = next;
                        break;
                    }
                    else
                    {
                        queue.RemoveAt(0);
                    }
                }
            }
            if (curCustomDuty != null  && pawn.mindState.duty != curCustomDuty.duty && !(IsForcedDuty(pawn.mindState.duty?.def ?? null)))
            {                
                pawn.mindState.duty = curCustomDuty.duty;
            }
        }

        public void StartDuty(CustomPawnDuty duty, bool returnCurDutyToQueue = true)
        {            
            if (IsForcedDuty(pawn.mindState.duty?.def ?? null) || (curCustomDuty != null && IsForcedDuty(curCustomDuty.duty.def)))
            {
                return;
            }
            if (returnCurDutyToQueue)
            {
                if (curCustomDuty != null)
                {
                    EnqueueFirst(curCustomDuty);                 
                }
                else if (pawn.mindState.duty != null)
                {
                    CustomPawnDuty custom = new CustomPawnDuty();
                    custom.duty = pawn.mindState.duty;
                    EnqueueFirst(custom);
                }
            }
			if (duty.startAfter > 0)
			{
				duty.startsAt = GenTicks.TicksGame + duty.startAfter;
			}
			if (duty.expireAfter > 0)
			{
				duty.expiresAt = GenTicks.TicksGame + duty.expireAfter;
			}
			curCustomDuty = duty;
            if (pawn.mindState != null)
            {
                pawn.mindState.duty = duty.duty;
            }
        }        

        public void Enqueue(CustomPawnDuty duty)
        {
            if (duty.startAfter > 0)
            {
                duty.startsAt = GenTicks.TicksGame + duty.startAfter;
            }
            if (duty.expireAfter > 0)
            {
                duty.expiresAt = GenTicks.TicksGame + duty.expireAfter;
            }
            queue.Add(duty);
        }

        public void EnqueueFirst(CustomPawnDuty duty)
        {
            if (duty.startAfter > 0)
            {
                duty.startsAt = GenTicks.TicksGame + duty.startAfter;
            }
            if (duty.expireAfter > 0)
            {
                duty.expiresAt = GenTicks.TicksGame + duty.expireAfter;
            }
            queue.Insert(0, duty);
        }

        public bool Any(DutyDef def)
        {
            return curCustomDuty?.duty.def == def || queue.Any(d => d.duty.def == def);
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Deep.Look(ref curCustomDuty, "curCustomDuty3", LookMode.Deep);
            Scribe_Collections.Look(ref queue, "queue3", LookMode.Deep);
            if(queue == null)
            {
                queue = new List<CustomPawnDuty>();
			}
        }

        private bool IsExitDuty(DutyDef def)
        {            
            return def != null && ((def == DutyDefOf.ExitMapBest) || (def == DutyDefOf.ExitMapRandom) || (def == DutyDefOf.ExitMapNearDutyTarget) || (def == DutyDefOf.ExitMapBestAndDefendSelf) || (def == DutyDefOf.TravelOrLeave) || (def == DutyDefOf.TravelOrWait));
        }

        private bool IsForcedDuty(DutyDef def)
        {
            return def != null && (IsExitDuty(def) || (def == DutyDefOf.PrisonerEscape) || (def == DutyDefOf.PrisonerEscapeSapper) || (def == DutyDefOf.PrisonerAssaultColony) || (def == DutyDefOf.Kidnap) || (def == DutyDefOf.Steal));
        }
    }
}

