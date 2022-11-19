using System;
using Verse;
using RimWorld;
using System.Reflection;
using HarmonyLib;

namespace CombatAI
{
    public class Settings : ModSettings
    {
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
            public int interval;
            public int buckets;
            public int carryLimit;

            public SightPerformanceSettings()
            {
            }

            public SightPerformanceSettings(int interval, int buckets, int carryLimit)
            {
                this.interval = interval;
                this.buckets = buckets;
                this.carryLimit = carryLimit;
            }            

            public void ExposeData()
            {
                Scribe_Values.Look(ref interval, $"frequency.{version}");
                Scribe_Values.Look(ref buckets, $"buckets.{version}");
                Scribe_Values.Look(ref carryLimit, $"carryLimit.{version}");
            }
        }

        private const int version = 1;

        /*                 
         * -- * -- * -- * -- * -- * -- * -- * -- * --
         */

        /*
         * 
         * -- General --
         * 
         */

        public bool LeanCE_Enabled = false;
        public bool Pather_Enabled = true;
        public bool Caster_Enabled = true;
        public bool Targeter_Enabled = true;
		public bool FogOfWar_Enabled = false;
		public bool PerformanceOpt_Enabled = true;
        public bool Pather_DisableL1L2 = false;
        public bool Pather_KillboxKiller = true;
        public float Pathfinding_DestWeight = 0.85f;
        public float FogOfWar_FogColor = 0.35f;
		public float FogOfWar_RangeMultiplier = 1.0f;
		public float FogOfWar_RangeFadeMultiplier = 0.5f;

		public SightPerformanceSettings SightSettings_FriendliesAndRaiders = new Settings.SightPerformanceSettings(2, 5, 12);        
        public SightPerformanceSettings SightSettings_MechsAndInsects = new Settings.SightPerformanceSettings(3, 10, 6);
        public SightPerformanceSettings SightSettings_Wildlife = new Settings.SightPerformanceSettings(6, 10, 4);
        public SightPerformanceSettings SightSettings_SettlementTurrets = new Settings.SightPerformanceSettings(8, 15, 12);

        /*
         * 
         * -- Advanced --
         * 
         */

        public bool AdvancedUser = false;
        public int Advanced_SightThreadIdleSleepTimeMS = 1;

        /*
         * 
         * --  Debug  --
         * 
         */

        public bool Debug = false;
        public bool Debug_ValidateSight = false;
        public bool Debug_DrawShadowCasts = false;
        public bool Debug_DrawShadowCastsVectors = false;
        public bool Debug_DrawAvoidanceGrid_Proximity = false;
        public bool Debug_DrawAvoidanceGrid_Danger = false;
        public bool Debug_DebugThingsTracker = false;
        public bool Debug_DebugDumpData = false;

        /*                 
         * -- * -- * -- * -- * -- * -- * -- * -- * --
         */

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref SightSettings_FriendliesAndRaiders, $"CombatAI.SightSettings_FriendliesAndRaiders.{version}");
            if (SightSettings_FriendliesAndRaiders == null)
            {
                SightSettings_FriendliesAndRaiders = new SightPerformanceSettings(2, 5, 12);
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
            Scribe_Values.Look(ref Pather_Enabled, $"Pather_Enabled.{version}", true);
            Scribe_Values.Look(ref Caster_Enabled, $"Caster_Enabled.{version}", true);            
            Scribe_Values.Look(ref Targeter_Enabled, $"Targeter_Enabled.{version}", true);
            Scribe_Values.Look(ref Pathfinding_DestWeight, $"Pathfinding_DestWeight.{version}", 0.85f);
            Scribe_Values.Look(ref AdvancedUser, $"AdvancedUser.{version}");
			Scribe_Values.Look(ref FogOfWar_FogColor, $"FogOfWar_FogColor.{version}", 0.35f);
			Scribe_Values.Look(ref FogOfWar_RangeFadeMultiplier, $"FogOfWar_RangeFadeMultiplier.{version}", 0.5f);
			Scribe_Values.Look(ref FogOfWar_RangeMultiplier, $"FogOfWar_RangeMultiplier.{version}", 1.0f);
			Scribe_Values.Look(ref Pather_KillboxKiller, $"Pather_KillboxKiller.{version}", true);
            Scribe_Values.Look(ref PerformanceOpt_Enabled, $"PerformanceOpt_Enabled.{version}", true);
			Scribe_Values.Look(ref FogOfWar_Enabled, $"FogOfWar_Enabled.{version}", false);
			Scribe_Values.Look(ref Debug, $"Debug.{version}");
            Scribe_Values.Look(ref Debug_ValidateSight, $"Debug_ValidateSight.{version}");
            Scribe_Values.Look(ref Debug_DrawShadowCasts, $"Debug_DrawShadowCasts.{version}");            
            //ScribeValues(); // Scribe values. (Will not scribe IExposables nor enums)
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

