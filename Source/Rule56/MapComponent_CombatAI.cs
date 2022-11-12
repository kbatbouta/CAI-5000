using System;
using Verse;
using System.Collections.Generic;

namespace CombatAI
{
    public class MapComponent_CombatAI : MapComponent
    {
        /*      Threading
         * ----- ----- ----- -----
         */

        private AsyncActions asyncActions;

        /*   ISMA map elements
         * ----- ----- ----- -----
         */

        public CellFlooder flooder;
        public ISGrid<float> f_grid;

        /* 
         * ----- ----- ----- -----
         */

        public MapComponent_CombatAI(Map map) : base(map)
        {
            flooder = new CellFlooder(map);
            f_grid = new ISGrid<float>(map);
            asyncActions = new AsyncActions();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();            
            asyncActions.ExecuteMainThreadActions();
        }

        public void EnqueueMainThreadAction(Action action)
        {
            asyncActions.EnqueueMainThreadAction(action);            
        }
    }
}

