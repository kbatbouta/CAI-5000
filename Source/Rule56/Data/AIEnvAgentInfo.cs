using System;
using Verse;
namespace CombatAI
{
    public struct AIEnvAgentInfo : IEquatable<Thing>, IEquatable<AIEnvAgentInfo>
    {
        public          AIEnvAgentState state;
        public readonly Thing           thing;

        public AIEnvAgentInfo(Thing thing, AIEnvAgentState state)
        {
            this.thing = thing;
            this.state = state;
        }

        public bool IsValid
        {
            get => thing != null;
        }

        public AIEnvAgentInfo Combine(AIEnvAgentInfo other)
        {
            if (other.thing != thing)
            {
                throw new InvalidOperationException("Both items must have the same parent thing");
            }
            return new AIEnvAgentInfo(thing, state | other.state)
            {
                // TODO remember to copy and process any new fields here.
            };
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
            return obj is Thing thing && this.thing == thing || obj is AIEnvAgentInfo other && other.thing == this.thing;
        }

        public override int GetHashCode()
        {
            return thing?.GetHashCode() ?? -1;
        }
    }
}
