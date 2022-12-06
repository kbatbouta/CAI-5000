﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatAI.Utilities
{
#if DEBUG_REACTION
	public class ThingsTracker : MapComponent
	{
		public ThingsTrackingModel pawnsTracker;
		public ThingsTrackingModel weaponsTracker;
		public ThingsTrackingModel apparelTracker;
		public ThingsTrackingModel medicineTracker;
		public ThingsTrackingModel interceptorsTracker;

		private Dictionary<ThingDef, ThingsTrackingModel> trackersByDef =
			new Dictionary<ThingDef, ThingsTrackingModel>();

		public ThingsTracker(Map map) : base(map)
		{
			pawnsTracker = new ThingsTrackingModel(null, map, this);
			weaponsTracker = new ThingsTrackingModel(null, map, this);
			apparelTracker = new ThingsTrackingModel(null, map, this);
			medicineTracker = new ThingsTrackingModel(null, map, this);
			interceptorsTracker = new ThingsTrackingModel(null, map, this);

			foreach (var def in DefDatabase<ThingDef>.AllDefs)
				if (def.HasComp(typeof(CompProjectileInterceptor)))
					trackersByDef[def] = interceptorsTracker;
				else if (def.IsWeapon)
					trackersByDef[def] = weaponsTracker;
				else if (def.IsApparel)
					trackersByDef[def] = apparelTracker;
				else if (def.thingCategories?.Contains(ThingCategoryDefOf.Medicine) ?? false)
					trackersByDef[def] = medicineTracker;
				else if (def.race != null) trackersByDef[def] = pawnsTracker;
		}

		public override void MapComponentOnGUI()
		{
			base.MapComponentOnGUI();
			if (!Finder.Settings.Debug_DebugThingsTracker) return;
			if (Find.Selector.SelectedObjects.Count == 0) return;
			var thing = Find.Selector.SelectedObjects.Where(s => s is Thing).Select(s => s as Thing).First();
			var model = GetModelFor(thing);
			if (model != null)
			{
				IEnumerable<Thing> others;
				others = model.ThingsInRangeOf(thing.Position, 25);
				if (others != null)
				{
					var a = UI.MapToUIPosition(thing.DrawPos);
					Vector2 b;
					Vector2 mid;
					Rect rect;
					var index = 0;
					foreach (var other in others)
					{
						b = UI.MapToUIPosition(other.DrawPos);
						Widgets.DrawLine(a, b, Color.red, 1);

						mid = (a + b) / 2;
						rect = new Rect(mid.x - 25, mid.y - 15, 50, 30);
						Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
						Widgets.DrawBox(rect);
						Widgets.Label(rect,
							$"<color=gray>({index++}).</color> {Math.Round(other.Position.DistanceTo(thing.Position), 1)}");
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ThingsTrackingModel GetModelFor(Thing thing)
		{
			return GetModelFor(thing.def);
		}

		public ThingsTrackingModel GetModelFor(ThingDef def)
		{
			trackersByDef.TryGetValue(def, out var model);
			return model;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ThingsTrackingModel GetModelFor(TrackedThingsRequestCategory category)
		{
			switch (category)
			{
				case TrackedThingsRequestCategory.Pawns:
					return pawnsTracker;
				case TrackedThingsRequestCategory.Apparel:
					return apparelTracker;
				case TrackedThingsRequestCategory.Weapons:
					return weaponsTracker;
				case TrackedThingsRequestCategory.Medicine:
					return medicineTracker;
				case TrackedThingsRequestCategory.Interceptors:
					return interceptorsTracker;
				default:
					return null;
			}
		}

		public void Notify_Spawned(Thing thing)
		{
			var model = GetModelFor(thing);
			if (model != null) model.Register(thing);
		}

		public void Notify_DeSpawned(Thing thing)
		{
			var model = GetModelFor(thing);
			if (model != null) model.DeRegister(thing);
		}

		public void Notify_PositionChanged(Thing thing)
		{
			var model = GetModelFor(thing);
			if (model != null) model.Notify_ThingPositionChanged(thing);
		}
	}
#endif
}