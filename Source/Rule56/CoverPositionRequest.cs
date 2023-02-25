using System;
using System.Collections.Generic;
using Verse;
namespace CombatAI
{
    public struct CoverPositionRequest
    {
        public Pawn caster;

        public LocalTargetInfo target;

        public Verb verb;

        public float maxRangeFromCaster;

        public IntVec3 locus;

        public float maxRangeFromLocus;

        public bool checkBlockChance;

        public Func<IntVec3, bool> validator;

        public List<Thing> majorThreats;
    }
}
