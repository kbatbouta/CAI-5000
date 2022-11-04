using System;
using Verse;

namespace CombatAI
{
    public class ThingComp_AnimalAI : ThingComp
    {
        public Pawn SelPawn => parent as Pawn;

        public ThingComp_AnimalAI()
        {            
        }
    }
}

