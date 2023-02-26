using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public partial class Pawn_CustomDutyTracker : IExposable
    {
        public CustomPawnDuty       curCustomDuty;
        public Pawn                 pawn;
        public List<CustomPawnDuty> queue = new List<CustomPawnDuty>();

        public Pawn_CustomDutyTracker()
        {
        }

        public Pawn_CustomDutyTracker(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public DutyDef CurDutyDef
        {
            get => curCustomDuty?.duty?.def ?? pawn.mindState?.duty?.def ?? null;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Deep.Look(ref curCustomDuty, "curCustomDuty3");
            Scribe_Collections.Look(ref queue, "queue4", LookMode.Deep);
            if (queue == null)
            {
                queue = new List<CustomPawnDuty>();
            }
        }

        public void TickRare()
        {
            if (curCustomDuty != null)
            {
                if (curCustomDuty.finished)
                {
                    curCustomDuty       = null;
                    pawn.mindState.duty = null;
                }
                else if (curCustomDuty.expiresAt > 0 && curCustomDuty.expiresAt <= GenTicks.TicksGame)
                {
                    curCustomDuty       = null;
                    pawn.mindState.duty = null;
                }
                else if (curCustomDuty.endOnDistToFocusLarger > 0 && curCustomDuty.duty.focus.Cell.IsValid && curCustomDuty.duty.focus.Cell.DistanceToSquared(pawn.Position) > Maths.Sqr(curCustomDuty.endOnDistToFocusLarger))
                {
                    curCustomDuty       = null;
                    pawn.mindState.duty = null;
                }
                else if (curCustomDuty.endOnDistToFocusLess > 0 && curCustomDuty.duty.focus.Cell.IsValid && curCustomDuty.duty.focus.Cell.DistanceToSquared(pawn.Position) < Maths.Sqr(curCustomDuty.endOnDistToFocusLess))
                {
                    curCustomDuty       = null;
                    pawn.mindState.duty = null;
                }
                else if (curCustomDuty.duty.focus.Thing != null)
                {
                    Thing focus = curCustomDuty.duty.focus.Thing;
                    if (curCustomDuty.endOnFocusDestroyed && (focus.Destroyed || !focus.Spawned))
                    {
                        curCustomDuty       = null;
                        pawn.mindState.duty = null;

                    }
                    else if (!pawn.CanReach(focus, PathEndMode.InteractionCell, Danger.Deadly, true, true))
                    {
                        curCustomDuty       = null;
                        pawn.mindState.duty = null;
                    }
                    else if (curCustomDuty.endOnFocusDeath || curCustomDuty.endOnFocusDowned)
                    {
                        Pawn fpawn = focus as Pawn;
                        if (fpawn == null)
                        {
                            curCustomDuty       = null;
                            pawn.mindState.duty = null;
                        }
                        else if ((curCustomDuty.endOnFocusDowned || curCustomDuty.endOnFocusDeath) && fpawn.Dead)
                        {
                            curCustomDuty       = null;
                            pawn.mindState.duty = null;
                        }
                        else if (curCustomDuty.endOnFocusDowned && fpawn.Downed)
                        {
                            curCustomDuty       = null;
                            pawn.mindState.duty = null;
                        }
                        else if (curCustomDuty.endOnFocusDutyNot != null && fpawn.mindState?.duty?.def != curCustomDuty.endOnFocusDutyNot)
                        {
                            curCustomDuty       = null;
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
                    if (!next.finished)
                    {
                        if (next.startsAt > GenTicks.TicksGame)
                        {
                            break;
                        }
                        if (next.expiresAt < 0 || next.expiresAt > GenTicks.TicksGame)
                        {
                            curCustomDuty = next;
                            break;
                        }
                    }
                    queue.RemoveAt(0);
                }
            }
            if (curCustomDuty != null && pawn.mindState.duty != curCustomDuty.duty && !IsForcedDuty(pawn.mindState.duty?.def ?? null))
            {
                pawn.mindState.duty = curCustomDuty.duty;
            }
        }

        public void FinishAllDuties(DutyDef def, Thing focus = null)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                CustomPawnDuty custom = queue[i];
                if (custom.duty?.def == def && (focus == null || custom.duty.focus == focus || custom.duty.focusSecond == focus))
                {
                    custom.finished = true;
                }
            }
            if (curCustomDuty != null && curCustomDuty.duty?.def == def && (focus == null || curCustomDuty.duty.focus == focus || curCustomDuty.duty.focusSecond == focus))
            {
                curCustomDuty.finished = true;
            }
        }

        public void StartDuty(CustomPawnDuty duty, bool returnCurDutyToQueue = true)
        {
            if (IsForcedDuty(pawn.mindState.duty?.def ?? null) || curCustomDuty != null && IsForcedDuty(curCustomDuty.duty.def))
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

        private bool IsExitDuty(DutyDef def)
        {
            return def != null && (def == DutyDefOf.ExitMapBest || def == DutyDefOf.ExitMapRandom || def == DutyDefOf.ExitMapNearDutyTarget || def == DutyDefOf.ExitMapBestAndDefendSelf || def == DutyDefOf.TravelOrLeave || def == DutyDefOf.TravelOrWait);
        }

        private bool IsForcedDuty(DutyDef def)
        {
            return def != null && (IsExitDuty(def) || def == DutyDefOf.PrisonerEscape || def == DutyDefOf.PrisonerEscapeSapper || def == DutyDefOf.PrisonerAssaultColony || def == DutyDefOf.Kidnap || def == DutyDefOf.Steal);
        }

        public void Notify_TookDamage()
        {
            if (curCustomDuty is { endOnTookDamage: true })
            {
                curCustomDuty = null;
            }
        }
    }
}
