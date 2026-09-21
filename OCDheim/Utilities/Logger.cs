using System;

namespace OCDheim
{
    public static class Logger
    {
        private static LoggingLevel logLevel => Config.loggingLevel.Value;

        public static void Debug(Func<string> func)
        {
            if (logLevel >= LoggingLevel.DEBUG)
            {
                Jotunn.Logger.LogDebug(func());
            }
        }
        
        public static void Info(Func<string> func)
        {
            if (logLevel >= LoggingLevel.INFO)
            {
                Jotunn.Logger.LogInfo(func());
            }
        }
        
        public static void Warn(Func<string> func)
        {
            if (logLevel >= LoggingLevel.WARNING)
            {
                Jotunn.Logger.LogWarning(func());
            }
        }
    }
}
