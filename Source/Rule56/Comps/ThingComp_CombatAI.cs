using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using Verse.AI;

namespace CombatAI.Comps
{
    public class ThingComp_CombatAI : ThingComp
    {
        private readonly ValWatcher<bool> visibleToEnemies;

        public SightTracker.SightReader sightReader;        

        public Pawn SelPawn
        {
            get => parent as Pawn;
        }

        public Map Map
        {
            get => parent.Map;
        }

        public bool Enabled
        {
            get => sightReader != null && SelPawn.Spawned && !SelPawn.Dead && !SelPawn.Downed && !(parent.Faction?.IsPlayerSafe() ?? false);
        }

        public ThingComp_CombatAI()
        {
            this.visibleToEnemies = new ValWatcher<bool>(false, (visible, old, ticks) => this.Notify_VisibilityToEnemies(visible), 240);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!Enabled)
            {
                return;
            }
            if (parent.IsHashIntervalTick(15))
            {
                visibleToEnemies.Current = sightReader.GetAbsVisibilityToEnemies(parent.Position) > 0;
            }
        }

        public void Notify_VisibilityToEnemies(bool visible)
        {
            if (!Enabled)
            {
                return;
            }
            if (visible)
            {
                Pawn pawn = SelPawn;                
                if (pawn != null)
                {
                    JobGiver_AIDefendSelf jobGiver = pawn.thinker.GetMainTreeThinkNode<JobGiver_AIDefendSelf>();
                    Job job = jobGiver.TryGiveJob(pawn);
                    if(job != null)
                    {
                        pawn.jobs.StopAll();
                        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    }
                }                            
            }
        }

        public void Notify_ApprochingEnemies()
        {            
        }

        public void Notify_SightReaderChanged(SightTracker.SightReader reader)
        {
            this.sightReader = reader;
        }
    }
}

