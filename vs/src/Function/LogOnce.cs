namespace Architect
{
    // Logs each distinct error once, so an error repeating every frame doesn't flood the log
    internal static class LogOnce
    {
        private static readonly HashSet<string> reported = new HashSet<string>();

        public static void Error(string where, Exception e)
        {
            if (reported.Add(where + ":" + e.GetType().Name + ":" + e.Message)) MelonLogger.Error(where + ": " + e);
        }
    }
}
