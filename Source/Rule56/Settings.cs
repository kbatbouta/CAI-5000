﻿using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
namespace CombatAI
{
    public class Settings : ModSettings
    {

        private const int version                             = 15;
        public        int Advanced_SightThreadIdleSleepTimeMS = 1;

        /*
         * 
         * -- Advanced --
         * 
         */

        public bool AdvancedUser;
        public bool Caster_Enabled        = true;
        public bool Personalities_Enabled = true;

        /*
         * 
         * --  Debug  --
         * 
         */
#if DEBUG
		public bool Debug = true;
		public bool Debug_LogJobs = true;
#else
        public bool Debug;
        public bool Debug_LogJobs;
#endif
        public bool Debug_DebugDumpData               = false;
        public bool Debug_DebugThingsTracker          = false;
        public bool Debug_DrawAvoidanceGrid_Danger    = false;
        public bool Debug_DrawAvoidanceGrid_Proximity = false;
        public bool Debug_DrawShadowCasts;
        public bool Debug_DrawShadowCastsVectors = false;
        public bool Debug_DrawThreatCasts        = false;
        public bool Debug_DisablePawnGuiOverlay  = false;
        public bool Debug_DebugPathfinding       = false;
        public bool Debug_DebugAvailability      = false;
        public bool Debug_ValidateSight;
        public bool Enable_Groups = true;

        public bool Enable_Sprinting          = true;
        public bool Flank_Enabled             = true;
        public bool FogOfWar_Allies           = true;
        public bool FogOfWar_Animals          = true;
        public bool FogOfWar_AnimalsSmartOnly = true;

        public bool  FogOfWar_Enabled;
        public bool  FogOfWar_OldShader           = true;
        public float FogOfWar_FogColor            = 0.5f;
        public float FogOfWar_RangeFadeMultiplier = 0.5f;
        public float FogOfWar_RangeMultiplier     = 1.8f;
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
        public bool  Pather_DisableL1L2         = false;
        public bool  Pather_Enabled             = true;
        public bool  Pather_KillboxKiller       = true;
        public float Pathfinding_DestWeight     = 0.85f;
        public float Pathfinding_SappingMul     = 1.0f;
        public int   Pathfinding_SquadPathWidth = 4;
        public bool  Temperature_Enabled        = true;
        public bool  PerformanceOpt_Enabled     = true;
        public bool  React_Enabled              = true;
        public bool  Retreat_Enabled            = true;
        public bool  FinishedQuickSetup;

        private FactionTechSettings       FactionSettings_Undefined = new FactionTechSettings(TechLevel.Undefined, 1, 1, 1, 1, 1,1);
        private List<FactionTechSettings> FactionSettings           = new List<FactionTechSettings>(); 

        public SightPerformanceSettings SightSettings_FriendliesAndRaiders = new SightPerformanceSettings(1, 3, 16);
        public SightPerformanceSettings SightSettings_MechsAndInsects      = new SightPerformanceSettings(3, 10, 6);
        public SightPerformanceSettings SightSettings_Wildlife             = new SightPerformanceSettings(3, 10, 4);
        public SightPerformanceSettings SightSettings_SettlementTurrets    = new SightPerformanceSettings(3, 15, 12);
        public bool                     Targeter_Enabled                   = true;

        /*                 
         * -- * -- * -- * -- * -- * -- * -- * -- * --
         */

        public FactionTechSettings GetTechSettings(TechLevel level)
        {
	        for (int i = 0; i < FactionSettings.Count; i++)
	        {
		        FactionTechSettings settings = FactionSettings[i];
		        if (settings.tech == level)
		        {
			        return settings;
		        }
	        }
	        Log.Warning($"ISMA: Tech settings for {level} not found. returning default value.");
	        return FactionSettings_Undefined;
        }
        
        public override void ExposeData()
        {
	        base.ExposeData();
	        Scribe_Deep.Look(ref SightSettings_FriendliesAndRaiders, $"CombatAI.SightSettings_FriendliesAndRaiders2.{version}");
            if (SightSettings_FriendliesAndRaiders == null)
            {
                SightSettings_FriendliesAndRaiders = new SightPerformanceSettings(1, 3, 16);
            }
            Scribe_Deep.Look(ref SightSettings_MechsAndInsects, $"CombatAI.SightSettings_MechsAndInsects2.{version}");
            if (SightSettings_MechsAndInsects == null)
            {
                SightSettings_MechsAndInsects = new SightPerformanceSettings(3, 10, 6);
            }
            Scribe_Deep.Look(ref SightSettings_Wildlife, $"CombatAI.SightSettings_Wildlife2.{version}");
            if (SightSettings_Wildlife == null)
            {
                SightSettings_Wildlife = new SightPerformanceSettings(3, 10, 4);
            }
            Scribe_Deep.Look(ref SightSettings_SettlementTurrets, $"CombatAI.SightSettings_SettlementTurrets2.{version}");
            if (SightSettings_SettlementTurrets == null)
            {
                SightSettings_SettlementTurrets = new SightPerformanceSettings(3, 15, 12);
            }
            Scribe_Values.Look(ref LeanCE_Enabled, $"LeanCE_Enabled.{version}");

#if DEBUG
			Scribe_Values.Look(ref Debug, $"Debug.Debug.{version}", true);
			Scribe_Values.Look(ref Debug_LogJobs, $"Debug.Debug_LogJobs.1.{version}", true);
#else
            Scribe_Values.Look(ref Debug, $"Release.Debug.{version}");
            Scribe_Values.Look(ref Debug_LogJobs, $"Release.Debug_LogJobs.{version}");
#endif

            Scribe_Values.Look(ref FinishedQuickSetup, $"FinishedQuickSetup2.{version}");
            Scribe_Values.Look(ref Personalities_Enabled, $"Personalities_Enabled.{version}", true);
            Scribe_Values.Look(ref FogOfWar_OldShader, $"FogOfWar_OldShader.{version}", true);
            Scribe_Values.Look(ref Pather_Enabled, $"Pather_Enabled.{version}", true);
            Scribe_Values.Look(ref Caster_Enabled, $"Caster_Enabled.{version}", true);
            Scribe_Values.Look(ref Targeter_Enabled, $"Targeter_Enabled.{version}", true);
            Scribe_Values.Look(ref Temperature_Enabled, $"Temperature_Enabled.{version}", true);
            Scribe_Values.Look(ref React_Enabled, $"React_Enabled.{version}", true);
            Scribe_Values.Look(ref Retreat_Enabled, $"Retreat_Enabled.{version}", true);
            Scribe_Values.Look(ref Flank_Enabled, $"Flank_Enabled.{version}", true);
            Scribe_Values.Look(ref Pathfinding_DestWeight, $"Pathfinding_DestWeight.{version}", 0.85f);
            Scribe_Values.Look(ref Pathfinding_SquadPathWidth, $"Pathfinding_SquadPathWidth.{version}", 4);
            Scribe_Values.Look(ref AdvancedUser, $"AdvancedUser.{version}");
            Scribe_Values.Look(ref FogOfWar_FogColor, $"FogOfWar_FogColor.{version}", 0.65f);
            Scribe_Values.Look(ref FogOfWar_RangeFadeMultiplier, $"FogOfWar_RangeFadeMultiplier.{version}", 0.5f);
            Scribe_Values.Look(ref FogOfWar_RangeMultiplier, $"FogOfWar_RangeMultiplier.{version}", 1.8f);
            Scribe_Values.Look(ref Pather_KillboxKiller, $"Pather_KillboxKiller.{version}", true);
            Scribe_Values.Look(ref PerformanceOpt_Enabled, $"PerformanceOpt_Enabled.{version}", true);
            Scribe_Values.Look(ref FogOfWar_Enabled, $"FogOfWar_Enabled.{version}");
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

            Scribe_Collections.Look(ref FactionSettings, $"FactionSettings.{version}", LookMode.Deep);
            if (FactionSettings.NullOrEmpty())
            {
	            FactionSettings = new List<FactionTechSettings>();
            }
            foreach (TechLevel tech in Enum.GetValues(typeof(TechLevel)))
            {
	            if (!FactionSettings.Any(t => t.tech == tech))
	            {
		            FactionSettings.Clear();
		            break;
	            }
            }
            if (FactionSettings.NullOrEmpty())
            {
	            ResetTechSettings();
            }
        }

        public void ResetTechSettings()
        {
	        FactionSettings.Clear();
	        FactionSettings.Add(new FactionTechSettings(TechLevel.Undefined, retreat: 1, duck: 1, cover: 1, sapping: 1, pathing: 1, group: 1));
	        FactionSettings.Add(new FactionTechSettings(TechLevel.Animal, retreat: 0.0f, duck: 0.0f, cover: 0.25f, sapping: 2.0f, pathing: 1.15f, group: 2.0f));
	        FactionSettings.Add(new FactionTechSettings(TechLevel.Neolithic, retreat: 0.0f, duck: 0.0f, cover: 0.25f, sapping: 0.75f, pathing: 1.1f, group: 2.0f));
	        FactionSettings.Add(new FactionTechSettings(TechLevel.Medieval, retreat: 0.25f, duck: 0.25f, cover: 0.5f, sapping: 0.50f, pathing: 1f, group: 2.0f));
	        FactionSettings.Add(new FactionTechSettings(TechLevel.Industrial, retreat: 0.75f, duck: 0.75f, cover: 1, sapping: 1, pathing: 1, group: 1.25f));
	        FactionSettings.Add(new FactionTechSettings(TechLevel.Spacer, retreat: 0.95f, duck: 0.95f, cover: 1, sapping: 1, pathing: 1, group: 1.0f));
	        FactionSettings.Add(new FactionTechSettings(TechLevel.Archotech, retreat: 1, duck: 1, cover: 1, sapping: 1, pathing: 0.9f, group: 1.0f));
	        FactionSettings.Add(new FactionTechSettings(TechLevel.Ultra, retreat: 1.0f, duck: 1.0f, cover: 1.0f, sapping: 0.9f, pathing: 1, group: 1.0f));
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

        public class FactionTechSettings : IExposable
        {
	        public float     retreat;
	        public float     duck;
	        public float     cover;
	        public float     sapping;
	        public float     pathing;
	        public float     group;
	        public TechLevel tech;

	        public FactionTechSettings()
	        {
	        }
	        
	        public FactionTechSettings(TechLevel tech, float retreat, float duck, float cover, float sapping, float pathing, float group)
	        {
		        this.tech    = tech;
		        this.retreat = retreat;
		        this.duck    = duck;
		        this.cover   = cover;
		        this.sapping = sapping;
		        this.pathing = pathing;
		        this.group   = group;
	        }

	        public void ExposeData()
	        {
		        Scribe_Values.Look(ref tech, $"tech.{version}", tech);
		        Scribe_Values.Look(ref retreat, $"retreat.{version}", retreat);
		        Scribe_Values.Look(ref duck, $"duck.{version}", duck);
		        Scribe_Values.Look(ref cover, $"cover.{version}", cover);
		        Scribe_Values.Look(ref sapping, $"sapping.{version}", sapping);
		        Scribe_Values.Look(ref pathing, $"pathing.{version}", pathing);
		        Scribe_Values.Look(ref group, $"group.{version}", group);
	        }
        }
    }
}
