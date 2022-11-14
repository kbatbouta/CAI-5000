using System;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace CombatAI
{
    public class IShortTermMemoryGrid
    {
        public Map map;

        public IShortTermMemoryGrid(Map map)
        {
            this.map = map;
        }
    }
}

