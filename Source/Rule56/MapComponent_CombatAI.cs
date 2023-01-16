using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CombatAI.Gui;
using UnityEngine;
using Verse;
namespace CombatAI
{
	public class MapComponent_CombatAI : MapComponent
	{

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

		public InterceptorTracker interceptors;

		/* 
		 * ----- ----- ----- -----
		 */


		public MapComponent_CombatAI(Map map) : base(map)
		{
			flooder      = new CellFlooder(map);
			f_grid       = new ISGrid<float>(map);
			asyncActions = new AsyncActions();
			interceptors = new InterceptorTracker(this);
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

		public override void MapComponentOnGUI()
		{
			base.MapComponentOnGUI();
			if (Finder.Settings.Debug_DrawThreatCasts && !Find.Selector.SelectedPawns.NullOrEmpty())
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
			}
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
