using System;
using System.Diagnostics;
using Verse;
namespace CombatAI
{
    public static class ExceptionUtility
    {
        public static bool enabled = true;
        
        public static void ShowExceptionGui(this Exception er, bool rethrow = true)
        {
            Log.Error($"ISMA: base error {er.ToString()}");
            if (enabled && Find.WindowStack.windows.Count(w => w is Window_Exception) <= 3)
            {
                StackTrace       trace  = new StackTrace();
                Window_Exception window = new Window_Exception(er, trace, string.Empty);
                Find.WindowStack.Add(window);
            }
            if (rethrow)
            {
                throw er;
            }
        }
    }
}
