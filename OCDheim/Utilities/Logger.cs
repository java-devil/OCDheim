using System;
using BepInEx.Logging;

namespace OCDheim
{
    public static class Logger
    {
        private static LogLevel logLevel => LogLevel.Debug;

        public static void Debug(Func<string> func)
        {
            if (logLevel >= LogLevel.Debug)
            {
                Jotunn.Logger.LogDebug(func());
            }
        }
        
        public static void Info(Func<string> func)
        {
            if (logLevel >= LogLevel.Info)
            {
                Jotunn.Logger.LogInfo(func());
            }
        }
        
        public static void Warn(Func<string> func)
        {
            if (logLevel >= LogLevel.Warning)
            {
                Jotunn.Logger.LogWarning(func());
            }
        }
    }
}
