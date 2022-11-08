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

            public SightPerformanceSettings()
            {
            }

            public SightPerformanceSettings(int interval, int buckets)
            {
                this.interval = interval;
                this.buckets = buckets;
            }            

            public void ExposeData()
            {
                Scribe_Values.Look(ref interval, $"frequency.{version}");
                Scribe_Values.Look(ref buckets, $"buckets.{version}");
            }
        }

        private const int version = 0;

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

        public SightPerformanceSettings SightSettings_FriendliesAndRaiders = new Settings.SightPerformanceSettings(2, 5);        
        public SightPerformanceSettings SightSettings_MechsAndInsects = new Settings.SightPerformanceSettings(3, 10);
        public SightPerformanceSettings SightSettings_Wildlife = new Settings.SightPerformanceSettings(6, 10);
        public SightPerformanceSettings SightSettings_SettlementTurrets = new Settings.SightPerformanceSettings(8, 15);

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
        public bool Debug_DrawShadowCasts = false;
        public bool Debug_DrawShadowCastsVectors = false;
        public bool Debug_DrawAvoidanceGrid_Proximity = false;
        public bool Debug_DrawAvoidanceGrid_Danger = false;
        public bool Debug_DebugThingsTracker = false;

        /*                 
         * -- * -- * -- * -- * -- * -- * -- * -- * --
         */

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref SightSettings_FriendliesAndRaiders, $"CombatAI.SightSettings_FriendliesAndRaiders.{version}");
            if (SightSettings_FriendliesAndRaiders == null)
            {
                SightSettings_FriendliesAndRaiders = new SightPerformanceSettings(2, 5);
            }
            Scribe_Deep.Look(ref SightSettings_MechsAndInsects, $"CombatAI.SightSettings_MechsAndInsects.{version}");
            if (SightSettings_MechsAndInsects == null)
            {
                SightSettings_MechsAndInsects = new SightPerformanceSettings(3, 10);
            }            
            Scribe_Deep.Look(ref SightSettings_Wildlife, $"CombatAI.SightSettings_Wildlife.{version}");
            if (SightSettings_Wildlife == null)
            {
                SightSettings_Wildlife = new SightPerformanceSettings(6, 10);
            }
            Scribe_Deep.Look(ref SightSettings_SettlementTurrets, $"CombatAI.SightSettings_SettlementTurrets.{version}");
            if (SightSettings_SettlementTurrets == null)
            {
                SightSettings_SettlementTurrets = new SightPerformanceSettings(8, 15);
            }
            ScribeValues(); // Scribe values. (Will not scribe IExposables nor enums)
        }

        private void ScribeValues()
        {
            foreach (FieldInfo f in typeof(Settings).GetFields(BindingFlags.Public))
            {
                if (f.HasAttribute<UnsavedAttribute>() || f.IsInitOnly)
                {
                    continue;
                }
                if (f.FieldType == typeof(string) || f.FieldType == typeof(int) || f.FieldType == typeof(float) || f.FieldType == typeof(double) || f.FieldType == typeof(bool) || f.FieldType == typeof(byte))
                {
                    object[] args = new object[] { f.GetValue(this), $"CombatAI.{f.Name}.{version}", f.GetValue(this), false };
                    AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look), generics: new Type[] { f.FieldType })
                        .Invoke(null, args);
                    f.SetValue(this, args[0]);
                }
            }
        }
    }
}

