using Verse;
namespace CombatAI
{
	public class Settings : ModSettings
	{

		private const int version                             = 2;
		public        int Advanced_SightThreadIdleSleepTimeMS = 1;

		/*
		 * 
		 * -- Advanced --
		 * 
		 */

		public bool AdvancedUser;
		public bool Caster_Enabled = true;

		/*
		 * 
		 * --  Debug  --
		 * 
		 */

		public bool Debug;
		public bool Debug_DebugDumpData               = false;
		public bool Debug_DebugThingsTracker          = false;
		public bool Debug_DrawAvoidanceGrid_Danger    = false;
		public bool Debug_DrawAvoidanceGrid_Proximity = false;
		public bool Debug_DrawShadowCasts;
		public bool Debug_DrawShadowCastsVectors = false;
		public bool Debug_DrawThreatCasts        = false;
		public bool Debug_DisablePawnGuiOverlay  = false;
		public bool Debug_ValidateSight;
		public bool Enable_Groups = true;

		public bool Enable_Sprinting          = true;
		public bool Flank_Enabled             = true;
		public bool FogOfWar_Allies           = true;
		public bool FogOfWar_Animals          = true;
		public bool FogOfWar_AnimalsSmartOnly = true;

		public bool  FogOfWar_Enabled;
		public float FogOfWar_FogColor            = 0.35f;
		public float FogOfWar_RangeFadeMultiplier = 0.5f;
		public float FogOfWar_RangeMultiplier     = 1.0f;
		public bool  FogOfWar_Turrets             = true;

		/*                 
		 * -- * -- * -- * -- * -- * -- * -- * -- * --
		 */

		/*
		 * 
		 * -- General --
		 * 
		 */

		public bool  LeanCE_Enabled;
		public bool  Pather_DisableL1L2     = false;
		public bool  Pather_Enabled         = true;
		public bool  Pather_KillboxKiller   = true;
		public float Pathfinding_DestWeight = 0.85f;
		public float Pathfinding_SappingMul = 1.0f;
		public bool  PerformanceOpt_Enabled = true;
		public bool  React_Enabled          = true;
		public bool  Retreat_Enabled        = true;
		public bool  FinishedQuickSetup     = false;


		public SightPerformanceSettings SightSettings_FriendliesAndRaiders = new SightPerformanceSettings(3, 5, 16);
		public SightPerformanceSettings SightSettings_MechsAndInsects      = new SightPerformanceSettings(3, 10, 6);
		public SightPerformanceSettings SightSettings_SettlementTurrets    = new SightPerformanceSettings(8, 15, 12);
		public SightPerformanceSettings SightSettings_Wildlife             = new SightPerformanceSettings(6, 5, 4);
		public bool                     Targeter_Enabled                   = true;

		/*                 
		 * -- * -- * -- * -- * -- * -- * -- * -- * --
		 */

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref SightSettings_FriendliesAndRaiders, $"CombatAI.SightSettings_FriendliesAndRaiders.{version}");
			if (SightSettings_FriendliesAndRaiders == null)
			{
				SightSettings_FriendliesAndRaiders = new SightPerformanceSettings(3, 5, 16);
			}
			Scribe_Deep.Look(ref SightSettings_MechsAndInsects, $"CombatAI.SightSettings_MechsAndInsects.{version}");
			if (SightSettings_MechsAndInsects == null)
			{
				SightSettings_MechsAndInsects = new SightPerformanceSettings(3, 10, 6);
			}
			Scribe_Deep.Look(ref SightSettings_Wildlife, $"CombatAI.SightSettings_Wildlife.{version}");
			if (SightSettings_Wildlife == null)
			{
				SightSettings_Wildlife = new SightPerformanceSettings(6, 10, 4);
			}
			Scribe_Deep.Look(ref SightSettings_SettlementTurrets, $"CombatAI.SightSettings_SettlementTurrets.{version}");
			if (SightSettings_SettlementTurrets == null)
			{
				SightSettings_SettlementTurrets = new SightPerformanceSettings(8, 15, 12);
			}
			Scribe_Values.Look(ref LeanCE_Enabled, $"LeanCE_Enabled.{version}");
			//if (!LeanCE_Enabled && Mod_CE.active)
			//{
			//    LeanCE_Enabled = true;
			//}
			//else if (LeanCE_Enabled && !Mod_CE.active)
			//{
			//	LeanCE_Enabled = false;
			//}
			Scribe_Values.Look(ref FinishedQuickSetup, $"FinishedQuickSetup2.{version}", false);
			Scribe_Values.Look(ref Pather_Enabled, $"Pather_Enabled.{version}", true);
			Scribe_Values.Look(ref Caster_Enabled, $"Caster_Enabled.{version}", true);
			Scribe_Values.Look(ref Targeter_Enabled, $"Targeter_Enabled.{version}", true);
			Scribe_Values.Look(ref React_Enabled, $"React_Enabled.{version}", true);
			Scribe_Values.Look(ref Retreat_Enabled, $"Retreat_Enabled.{version}", true);
			Scribe_Values.Look(ref Flank_Enabled, $"Flank_Enabled.{version}", true);
			Scribe_Values.Look(ref Pathfinding_DestWeight, $"Pathfinding_DestWeight.{version}", 0.85f);
			Scribe_Values.Look(ref AdvancedUser, $"AdvancedUser.{version}");
			Scribe_Values.Look(ref FogOfWar_FogColor, $"FogOfWar_FogColor.{version}", 0.35f);
			Scribe_Values.Look(ref FogOfWar_RangeFadeMultiplier, $"FogOfWar_RangeFadeMultiplier.{version}", 0.5f);
			Scribe_Values.Look(ref FogOfWar_RangeMultiplier, $"FogOfWar_RangeMultiplier.{version}", 1.0f);
			Scribe_Values.Look(ref Pather_KillboxKiller, $"Pather_KillboxKiller.{version}", true);
			Scribe_Values.Look(ref PerformanceOpt_Enabled, $"PerformanceOpt_Enabled.{version}", true);
			Scribe_Values.Look(ref FogOfWar_Enabled, $"FogOfWar_Enabled.{version}");
			Scribe_Values.Look(ref Debug, $"Debug.{version}");
			Scribe_Values.Look(ref Debug_ValidateSight, $"Debug_ValidateSight.{version}");
			Scribe_Values.Look(ref Debug_DrawShadowCasts, $"Debug_DrawShadowCasts.{version}");
			Scribe_Values.Look(ref Enable_Sprinting, $"Enable_Sprinting.{version}", true);
			Scribe_Values.Look(ref Enable_Groups, $"Enable_Groups.{version}", true);
			Scribe_Values.Look(ref FogOfWar_Turrets, $"FogOfWar_Turrets.{version}", true);
			Scribe_Values.Look(ref FogOfWar_Animals, $"FogOfWar_Animals.{version}", true);
			Scribe_Values.Look(ref FogOfWar_AnimalsSmartOnly, $"FogOfWar_AnimalsSmartOnly.{version}", true);
			Scribe_Values.Look(ref FogOfWar_Allies, $"FogOfWar_Allies.{version}", true);
			Scribe_Values.Look(ref Pathfinding_SappingMul, $"Pathfinding_SappingMul2.{version}", 1.0f);

			//ScribeValues(); // Scribe values. (Will not scribe IExposables nor enums)
		}
		/*                 
		 * -- * -- * -- * -- * -- * -- * -- * -- * --
		 */

		/*
		 * 
		 * -- Sub --
		 * 
		 */

		public class SightPerformanceSettings : IExposable
		{
			public int buckets;
			public int carryLimit;
			public int interval;

			public SightPerformanceSettings()
			{
			}

			public SightPerformanceSettings(int interval, int buckets, int carryLimit)
			{
				this.interval   = interval;
				this.buckets    = buckets;
				this.carryLimit = carryLimit;
			}

			public void ExposeData()
			{
				Scribe_Values.Look(ref interval, $"frequency.{version}");
				Scribe_Values.Look(ref buckets, $"buckets.{version}");
				Scribe_Values.Look(ref carryLimit, $"carryLimit.{version}");
			}
		}

		//private void ScribeValues()
		//{
		//    foreach (FieldInfo f in typeof(Settings).GetFields(BindingFlags.Public))
		//    {
		//        if (f.HasAttribute<UnsavedAttribute>() || f.IsInitOnly)
		//        {
		//            continue;
		//        }
		//        if (f.FieldType == typeof(string) || f.FieldType == typeof(int) || f.FieldType == typeof(float) || f.FieldType == typeof(double) || f.FieldType == typeof(bool) || f.FieldType == typeof(byte))
		//        {
		//            object[] args = new object[] { f.GetValue(this), $"CombatAI.{f.Name}.{version}", f.GetValue(this), false };
		//            AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look), generics: new Type[] { f.FieldType })
		//                .Invoke(null, args);
		//            f.SetValue(this, args[0]);
		//        }
		//    }
		//}
	}
}
