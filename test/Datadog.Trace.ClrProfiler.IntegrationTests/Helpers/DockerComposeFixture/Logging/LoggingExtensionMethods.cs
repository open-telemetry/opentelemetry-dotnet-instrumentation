using System.Linq;

namespace DockerComposeFixture.Logging
{
    public static class LoggingExtensionMethods
    {
        public static void Log(this ILogger[] loggers, string msg)
        {
            foreach (var logger in loggers)
            {
                logger.Log(msg);
            }
        }

        public static string[] GetLoggedLines(this ILogger[] loggers)
        {
            var listLogger = (ListLogger)loggers.Single(l => l is ListLogger);
            return listLogger.LoggedLines.ToArray();
        }
    }
}