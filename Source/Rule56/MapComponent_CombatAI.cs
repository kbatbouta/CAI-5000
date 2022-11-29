using System;
using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using CombatAI.Gui;
using System.Linq;

namespace CombatAI
{
    public class MapComponent_CombatAI : MapComponent
    {
        private Listing_Collapsible collapsible = new Listing_Collapsible(true);
		private HashSet<IntVec3> _drawnCells = new HashSet<IntVec3>(256);

		/*      Threading
         * ----- ----- ----- -----
         */

		private AsyncActions asyncActions;

        /*   ISMA map elements
         * ----- ----- ----- -----
         */

        public CellFlooder flooder;
        public ISGrid<float> f_grid;

        public InterceptorTracker interceptors;

        /* 
         * ----- ----- ----- -----
         */


        public MapComponent_CombatAI(Map map) : base(map)
        {
            flooder = new CellFlooder(map);
            f_grid = new ISGrid<float>(map);
            asyncActions = new AsyncActions();
            interceptors = new InterceptorTracker(this);
		}

        public override void MapComponentTick()
        {
            base.MapComponentTick();      
            asyncActions.ExecuteMainThreadActions();
            interceptors.Tick();            
		}

		public override void MapComponentOnGUI()
        {
			base.MapComponentOnGUI();
            //if (!Find.Selector.Selc.())
            //{
            //    Thing thing = Find.Selector.SelectedObjects.First(o => o is Thing) as Thing;
            //    if (thing != null)
            //    {  
            //        Rect rect = new Rect(0, 0, UI.screenWidth * 0.2f, UI.screenHeight * 0.5f);
            //        collapsible.Begin(rect, "Damage Potential Report");
            //        DamageReport report = DamageUtility.GetDamageReport(thing, collapsible);
            //        collapsible.End(ref rect);
            //    }
            //}
        }

		public override void MapRemoved()
        {
            asyncActions.Kill();
			base.MapRemoved();
		}

		public void EnqueueMainThreadAction(Action action)
        {
            asyncActions.EnqueueMainThreadAction(action);
        }

		public void EnqueueOffThreadAction(Action action)
		{
			asyncActions.EnqueueOffThreadAction(action);
		}
	}
}

