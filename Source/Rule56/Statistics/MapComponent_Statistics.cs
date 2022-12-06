using System;
using RimWorld;
using Verse;
using System.Collections.Generic;
using CombatAI.Comps;
using RimWorld.Planet;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CombatAI.Statistics
{
    public class WorldComponent_Statistics : WorldComponent
    {
        private StringBuilder builder = new StringBuilder();
        private int[] visibleEnemiesTicks = new int[10];
        private int[] visibleEnemiesInjuries = new int[10];
        private float[] visibleEnemiesInjuryProbability = new float[10];
        private float[] temp = new float[10];

        public float[] VisibleEnemiesInjuryProbability
        {
            get
            {
                Array.Copy(visibleEnemiesInjuryProbability, temp, 10);
                return temp;
            }
        }

        public WorldComponent_Statistics(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            //if(GenTicks.TicksGame % GenTicks.TickLongInterval == 0)
            //{
            //    if (Finder.Settings.Debug)
            //    {
            //        builder.Clear();
            //        builder.AppendLine("<color=red>REPORT:</color> Injury probability by the number of visible enemies");
            //        builder.AppendLine("EnemyNum(num),\tInjury/Time(j/t),\t\tTime(t),\t\tInjuries(j)");
            //        for (int i = 0; i < 10; i++)
            //        {
            //            builder.AppendLine($"{i}num,\t{Math.Round(visibleEnemiesInjuryProbability[i],6)}j/t,\t{visibleEnemiesTicks[i]}t,\t{visibleEnemiesInjuries[i]}j");
            //        }
            //        Log.Message(builder.ToString());
            //    }
            //}
        }      

        public void Proccess(ThingComp_Statistics comp)
        {
            //for(int i = 0;i < 10; i++)
            //{
            //    visibleEnemiesTicks[i] += comp.visibleEnemiesTicks[i];
            //    visibleEnemiesInjuries[i] += comp.visibleEnemiesInjuries[i];                
            //    visibleEnemiesInjuryProbability[i] = (float)visibleEnemiesInjuries[i] / Maths.Max((float)visibleEnemiesTicks[i], 1);                                
            //}
            //for (int j = 1; j < 10; j++)
            //{
            //    visibleEnemiesInjuryProbability[j] += visibleEnemiesInjuryProbability[j - 1];
            //}
        }
    }
}

