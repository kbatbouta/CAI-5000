using System;
namespace CombatAI
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
    public class LoadNamed : Attribute
    {
        public string       name;
        public Type[]       prams;
        public LoadableType type = LoadableType.Unspecified;

        public LoadNamed(string name, Type[] prams = null)
        {
            this.name  = name;
            this.prams = prams;
        }

        public LoadNamed(string name, LoadableType type, Type[] prams = null)
        {
            this.type  = type;
            this.name  = name;
            this.prams = prams;
        }
    }
}
