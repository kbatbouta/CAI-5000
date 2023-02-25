using HarmonyLib;
namespace CombatAI
{
    public static class Finder
    {
        public static Harmony Harmony;

        public static CombatAIMod Mod;

        public static Settings Settings;

        public static PerformanceTracker Performance;

        public static int MainThreadId;

        public static float P75
        {
            get => Maths.Max(Performance.Performance, 0.75f);
        }

        public static float P50
        {
            get => Maths.Max(Performance.Performance, 0.50f);
        }
    }
}
