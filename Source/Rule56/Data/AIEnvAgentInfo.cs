using System;
using System.Collections.Generic;
using Verse;
namespace CombatAI
{
    public struct AIEnvAgentInfo : IExposable, IEquatable<Thing>, IEquatable<AIEnvAgentInfo>
    {
        public AIEnvAgentState state;
        public Thing        thing;

        public AIEnvAgentInfo(Thing thing, AIEnvAgentState state)
        {
            this.thing = thing;
            this.state = state;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref thing, "obsThing");
            Scribe_Values.Look(ref state, "obsAIAgentState");
        }
            
        public bool Equals(Thing other)
        {
            return other == thing;
        }
            
        public bool Equals(AIEnvAgentInfo other)
        {
            return other.thing == thing;
        }

        public override bool Equals(object obj)
        {
            return (obj is Thing thing && this.thing == thing) || (obj is AIEnvAgentInfo other && other.thing == this.thing);
        }

        public override int GetHashCode()
        {
            return thing?.GetHashCode() ?? -1;
        }
    }
}
