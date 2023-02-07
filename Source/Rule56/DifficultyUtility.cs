using Verse;
namespace CombatAI
{
    public static class DifficultyUtility
    {
        public static void SetDifficulty(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
	                Finder.Settings.Pathfinding_DestWeight = 0.9f;
	                Finder.Settings.Caster_Enabled         = false;
	                Finder.Settings.Targeter_Enabled       = false;
	                Finder.Settings.Pather_Enabled         = true;
	                Finder.Settings.Pather_KillboxKiller   = false;
	                Finder.Settings.PerformanceOpt_Enabled = true;
	                Finder.Settings.React_Enabled          = false;
	                Finder.Settings.Retreat_Enabled        = false;
	                Finder.Settings.Flank_Enabled          = false;

	                Finder.Settings.Enable_Sprinting       = false;
	                Finder.Settings.Enable_Groups          = false;
	                Finder.Settings.Pathfinding_SappingMul = 1.5f;

	                Finder.Settings.SightSettings_FriendliesAndRaiders.interval = 3;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_FriendliesAndRaiders.buckets = 10;
	                }
	                Finder.Settings.SightSettings_Wildlife.interval = 6;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_Wildlife.buckets = 10;
	                }
	                Finder.Settings.SightSettings_MechsAndInsects.interval = 3;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_MechsAndInsects.buckets = 12;
	                }
                    break;
                case Difficulty.Normal:
	                Finder.Settings.Pathfinding_DestWeight                      = 0.8f;
	                Finder.Settings.Caster_Enabled                              = true;
	                Finder.Settings.Targeter_Enabled                            = true;
	                Finder.Settings.Pather_Enabled                              = true;
	                Finder.Settings.Pather_KillboxKiller                        = true;
	                Finder.Settings.PerformanceOpt_Enabled                      = true;
	                Finder.Settings.React_Enabled                               = true;
	                Finder.Settings.Retreat_Enabled                             = false;
	                Finder.Settings.Flank_Enabled                               = true;
	                Finder.Settings.Enable_Sprinting                            = false;
	                Finder.Settings.Enable_Groups                               = true;
	                Finder.Settings.Pathfinding_SappingMul                      = 1.3f;
	                Finder.Settings.SightSettings_FriendliesAndRaiders.interval = 3;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_FriendliesAndRaiders.buckets = 5;
	                }
	                Finder.Settings.SightSettings_Wildlife.interval = 3;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_Wildlife.buckets = 10;
	                }
	                Finder.Settings.SightSettings_MechsAndInsects.interval = 3;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_MechsAndInsects.buckets = 5;
	                }
                    break;
                case Difficulty.Hard:
	                Finder.Settings.Pathfinding_DestWeight = 0.75f;
	                Finder.Settings.Caster_Enabled         = true;
	                Finder.Settings.Targeter_Enabled       = true;
	                Finder.Settings.Pather_Enabled         = true;
	                Finder.Settings.Pather_KillboxKiller   = true;
	                Finder.Settings.React_Enabled          = true;
	                Finder.Settings.Retreat_Enabled        = true;
	                Finder.Settings.Flank_Enabled          = true;
	                Finder.Settings.PerformanceOpt_Enabled = true;

	                Finder.Settings.Enable_Sprinting       = false;
	                Finder.Settings.Enable_Groups          = true;
	                Finder.Settings.Pathfinding_SappingMul = 1.0f;

	                Finder.Settings.SightSettings_FriendliesAndRaiders.interval = 2;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_FriendliesAndRaiders.buckets = 5;
	                }
	                Finder.Settings.SightSettings_Wildlife.interval = 2;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_Wildlife.buckets = 5;
	                }
	                Finder.Settings.SightSettings_MechsAndInsects.interval = 2;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_MechsAndInsects.buckets = 5;
	                }
                    break;
                case Difficulty.DeathWish:
	                Finder.Settings.Pathfinding_DestWeight = 0.6f;
	                Finder.Settings.Caster_Enabled         = true;
	                Finder.Settings.Targeter_Enabled       = true;
	                Finder.Settings.Pather_Enabled         = true;
	                Finder.Settings.Pather_KillboxKiller   = true;
	                Finder.Settings.React_Enabled          = true;
	                Finder.Settings.Retreat_Enabled        = true;
	                Finder.Settings.Flank_Enabled          = true;
	                Finder.Settings.PerformanceOpt_Enabled = false;

	                Finder.Settings.Enable_Sprinting       = true;
	                Finder.Settings.Enable_Groups          = true;
	                Finder.Settings.Pathfinding_SappingMul = 1.0f;

	                Finder.Settings.SightSettings_FriendliesAndRaiders.interval = 1;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_FriendliesAndRaiders.buckets = 5;
	                }
	                Finder.Settings.SightSettings_Wildlife.interval = 2;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_Wildlife.buckets = 5;
	                }
	                Finder.Settings.SightSettings_MechsAndInsects.interval = 2;
	                if (Current.ProgramState != ProgramState.Playing)
	                {
		                Finder.Settings.SightSettings_MechsAndInsects.buckets = 5;
	                }
                    break;
            }
        }   
    }
}
