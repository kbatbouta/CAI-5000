using System;
using Verse;
using RimWorld;
using System.Reflection;
using HarmonyLib;

namespace CombatAI
{
    public class Settings : ModSettings
    {
        private const int version = 0;

        /*                 
         * -- * -- * -- * -- * -- * -- * -- * -- * --
         */

        /*
         * 
         * -- General --
         * 
         */

        public bool Pather_Enabled = true;
        public bool Caster_Enabled = true;
        public bool Targeter_Enabled = true;

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

