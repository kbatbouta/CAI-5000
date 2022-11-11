using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace CombatAI
{
    public class AvoidanceTracker : MapComponent
    {
        public class AvoidanceReader
        {            
            private readonly Map map;
            private readonly CellIndices indices;
            private readonly AvoidanceTracker tacker;

            public AvoidanceReader(AvoidanceTracker tracker)
            {
                this.tacker = tracker;
                this.map = tracker.map;
                this.indices = tracker.map.cellIndices;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetDanger(IntVec3 cell) => 0;
            public float GetDanger(int index) => 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetProximity(IntVec3 cell) => 0;
            public float GetProximity(int index) => 0;                      
        }

        public AvoidanceTracker(Map map) : base(map)
        {            
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();           
        }

        public bool TryGetReader(Pawn pawn, out AvoidanceReader reader)
        {
            reader = null;
            return false;
        }        

        public void Notify_PawnSuppressed(IntVec3 cell)
        {                    
        }

        public void Notify_Bullet(IntVec3 cell)
        {                   
        }

        public void Notify_Smoke(IntVec3 cell)
        {                       
        }

        public void Notify_PathFound(Pawn pawn, PawnPath path)
        {                  
        }

        public void Notify_CoverPositionSelected(Pawn pawn, IntVec3 cell)
        {           
        }

        public void Notify_Injury(Pawn pawn, IntVec3 cell)
        {           
        }

        public void Notify_Death(Pawn pawn, IntVec3 cell)
        {         
        }
    }
}

