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

		public int SightRadius
		{
			get => Props.radiusNight == null ? Props.radius : (int) Mathf.Lerp(Props.radiusNight.Value, Props.radius, parent.Map.skyManager.CurSkyGlow);
		}

		public CompProperties_Sighter Props
		{
			get => props as CompProperties_Sighter;
		}
				
		public CompPowerTrader CompPower
		{
			get => Props.powered ? (_compPower ?? (_compPower = parent?.GetComp_Fast<CompPowerTrader>() ?? null)) : null;
		}
		
		public CompMannable CompMannable
		{
			get => Props.mannable ? (_compMannable ?? (_compMannable = parent?.GetComp_Fast<CompMannable>() ?? null)) : null;
		}

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

