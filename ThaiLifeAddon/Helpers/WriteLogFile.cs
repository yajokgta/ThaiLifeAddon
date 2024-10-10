using log4net;

namespace ThaiLifeAddon.Helpers
{
    public class WriteLogFile
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WriteLogFile));

        public static void LogAddon(string text)
        {
            log.Info(text);
        }
    }
}
