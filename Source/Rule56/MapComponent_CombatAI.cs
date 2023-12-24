using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CombatAI.Gui;
using UnityEngine;
using Verse;
using GUIUtility = CombatAI.Gui.GUIUtility;
namespace CombatAI
{
    public class MapComponent_CombatAI : MapComponent
    {
	    private int clearCacheCountDown = 14400;
        /*      Threading
         * ----- ----- ----- -----
         */
        private readonly AsyncActions        asyncActions;
        private readonly Listing_Collapsible collapsible = new Listing_Collapsible();
        private          HashSet<IntVec3>    _drawnCells = new HashSet<IntVec3>(256);
        public           ISGrid<float>       f_grid;

        /*   ISMA map elements
         * ----- ----- ----- -----
         */

        public CellFlooder flooder;
        public CellFlooder flooder_heursitic;

        public InterceptorTracker interceptors;
        /*      Cache
         * ----- ----- ----- -----
         */

        public Dictionary<Pair<int, int>, int> regionWiseDist = new Dictionary<Pair<int, int>, int>(64);

        /* 
         * ----- ----- ----- -----
         */


        public MapComponent_CombatAI(Map map) : base(map)
        {
            flooder           = new CellFlooder(map);
            flooder_heursitic = new CellFlooder(map);
            f_grid            = new ISGrid<float>(map);
            asyncActions      = new AsyncActions();
            interceptors      = new InterceptorTracker(this);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            asyncActions.Start();
            // do it on load.
            Finder.MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            asyncActions.ExecuteMainThreadActions();
            interceptors.Tick();
        }

        public override void MapComponentUpdate()
        {
	        base.MapComponentUpdate();
	        if (clearCacheCountDown-- <= 0)
	        {
		        CacheUtility.ClearAllCache();
		        regionWiseDist.Clear();
		        clearCacheCountDown = 14400;
	        }
	        else if (clearCacheCountDown % 7200 == 0)
	        {
		        CacheUtility.ClearShortCache();
		        regionWiseDist.Clear();
	        }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();            
            if (Finder.Settings.Debug_LogJobs)
            {
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    Text.Font                    = GameFont.Tiny;
                    Finder.Settings.AdvancedUser = true;
                    Finder.Settings.Debug        = true;
                    string message = "WARNING: CAI-5000 Job logging is on! This will hurst performance! Please disable job logging in the debug settings in CAI";
                    Rect   rect    = new Rect(20, 3, message.GetWidthCached(), 20);
                    Widgets.Label(rect, message);
                });
            }
            if (Finder.Settings.Debug_DrawThreatCasts && !Find.Selector.SelectedPawns.NullOrEmpty())
            {
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    Pawn pawn = Find.Selector.SelectedPawns.First();
                    if (pawn != null)
                    {
                        Rect rect = new Rect(0, 35, UI.screenWidth * 0.2f, UI.screenHeight * 0.5f);
                        collapsible.Begin(rect, "Damage Potential Report");
                        DamageUtility.GetDamageReport(pawn, collapsible);
                        pawn.GetArmorReport(collapsible);
                        ArmorReport report = pawn.GetArmorReport();
                        collapsible.Line(4);
                        collapsible.Label($"armor. s:{report.Sharp}\tb:{report.Blunt}");
                        collapsible.End(ref rect);
                    }
                });
            }
        }

        public override void MapRemoved()
        {
            asyncActions.Kill();
            base.MapRemoved();
        }

        public void Notify_MapChanged()
        {
            regionWiseDist.Clear();
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
