using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using Verse.AI;
using System.Threading;
using CombatAI.Statistics;
using System.Text;

namespace CombatAI.Comps
{
    public class ThingComp_Statistics : ThingComp
    {
        //private int lastInjuryTick = -1;
        //public int[] visibleEnemiesTicks = new int[10];
        //public int[] visibleEnemiesInjuries = new int[10];        

        public ThingComp_Statistics()
        {
        }        

        public override void CompTickRare()
        {
            base.CompTickRare();
            //if (Finder.Settings.Debug && parent.Spawned)
            //{
            //    if (parent.Faction?.IsPlayerSafe() ?? true)
            //    {
            //        return;
            //    }
            //    if (parent is Pawn pawn && pawn.GetSightReader(out SightTracker.SightReader reader) && !pawn.Downed)
            //    {
            //        int ticksIndex = Math.Min(Mathf.CeilToInt(reader.GetVisibilityToEnemies(parent.Position)), 9);
            //        visibleEnemiesTicks[ticksIndex]++;
            //        Find.World.GetComp_Fast<WorldComponent_Statistics>().Proccess(this);
            //    }
            //}
        }

        public void Notify_PawnTookDamage()
        {
            //if (Finder.Settings.Debug && GenTicks.TicksGame != lastInjuryTick && parent.Spawned)
            //{
            //    if (parent is Pawn pawn && pawn.GetSightReader(out SightTracker .SightReader reader))
            //    {
            //        lastInjuryTick = GenTicks.TicksGame;
            //        int ticksIndex = Math.Min(Mathf.CeilToInt(reader.GetVisibilityToEnemies(parent.Position)), 9);
            //        visibleEnemiesInjuries[ticksIndex]++;                    
            //    }
            //}
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            //if (Finder.Settings.Debug)
            //{
            //    Find.World.GetComp_Fast<WorldComponent_Statistics>().Proccess(this);
            //}
        }
    }
}

