using System;
using System.Collections.Generic;
using Verse;
namespace CombatAI
{
    public static class RaidTargetDatabase
    {
        public static readonly List<ThingDef> allDefs = new List<ThingDef>();
        
        public static void Initialize()
        {
            try
            {
                foreach (RaidTargetCollectionDef collection in DefDatabase<RaidTargetCollectionDef>.AllDefs)
                {
                    if (!collection.Initialized)
                    {
                        collection.PostPostLoad();
                    }
                    allDefs.AddRange(collection.targetDefs);
                }
            }
            catch (Exception er)
            {
                Log.Error($"ISMA: failed to load raid targets with error {er.Message}, {er.ToString()}");
            }
        }
    }
}
