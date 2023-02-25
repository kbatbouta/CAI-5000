using System;
namespace CombatAI
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RunIf : Attribute
    {
        public bool loaded;

        public RunIf(bool loaded)
        {
            this.loaded = loaded;
        }
    }
}
