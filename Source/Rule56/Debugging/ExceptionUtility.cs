using System;
using Verse;
namespace CombatAI
{
    public static class ExceptionUtility
    {
        public static bool enabled = true;

        public static void ShowExceptionGui(this Exception er, bool rethrow = true)
        {
            Log.Error($"ISMA: base error {er}");
//#if DEBUG
//            if (enabled && Find.WindowStack.windows.Count(w => w is Window_Exception) <= 3)
//            {
//                StackTrace       trace = new StackTrace();
//                Window_Exception window = new Window_Exception(er, trace, string.Empty);
//                Find.WindowStack.Add(window);
//            }
//#endif
            if (rethrow)
            {
                throw er;
            }
        }
    }
}
