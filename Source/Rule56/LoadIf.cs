using System;
namespace CombatAI
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
    public class LoadIf : Attribute
    {
        public string packageId;

        public LoadIf(string packageId = null)
        {
            this.packageId = packageId.ToLower();
        }
    }
}
