using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatAI.Comps
{
	public class ThingComp_Sighter : ThingComp
	{
		private CompPowerTrader _compPower;
		private CompMannable _compMannable;

		/// <summary>
		/// Parent sight radius.
		/// </summary>
		public int SightRadius
		{
			get => Props.radiusNight == null ? Props.radius : (int) Mathf.Lerp(Props.radiusNight.Value, Props.radius, parent.Map.skyManager.CurSkyGlow);
		}

		/// <summary>
		/// Source CompProperties_Sighter.
		/// </summary>
		public CompProperties_Sighter Props
		{
			get => props as CompProperties_Sighter;
		}

		/// <summary>
		/// Parent power trader.
		/// </summary>
		public CompPowerTrader CompPower
		{
			get => Props.powered ? (_compPower ?? (_compPower = parent?.GetComp_Fast<CompPowerTrader>() ?? null)) : null;
		}

		/// <summary>
		/// Parent mannable.
		/// </summary>
		public CompMannable CompMannable
		{
			get => Props.mannable ? (_compMannable ?? (_compMannable = parent?.GetComp_Fast<CompMannable>() ?? null)) : null;
		}

		/// <summary>
		/// Whether this sighter is active. Used by SightGrid to check if the parent should be skipped or not.
		/// </summary>
		public bool Active
		{
			get
			{
				CompPowerTrader power = CompPower;
				if (power != null && !power.PowerOn)
				{
					return false;
				}
				CompMannable mannable = CompMannable;
				if(mannable != null && !mannable.MannedNow)
				{
					return false;
				}
				return true;
			}
		}

		public ThingComp_Sighter()
		{
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			_compPower = parent?.GetComp_Fast<CompPowerTrader>();
			_compMannable = parent?.GetComp_Fast<CompMannable>();
			parent.Map.GetComp_Fast<SightTracker>().Register(parent);
		}		
	}
}

