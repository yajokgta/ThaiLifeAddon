using log4net;
using System;
using System.Configuration;
using System.Linq;
using System.Web.Configuration;

namespace ThaiLifeAddon.Helpers
{
    public class WriteLogFile
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WriteLogFile));
        public static string _LogFile = ConfigurationSettings.AppSettings["LogFile"];
        public static void LogAddon(string iText)
        {
            String LogFilePath = String.Format("{0}{1}_Log.txt", _LogFile, $"{DateTime.Now.ToString("yyyyMMdd")}_Addon");
            try
            {
                if (System.IO.Directory.Exists(_LogFile) == false)
                    System.IO.Directory.CreateDirectory(_LogFile);

                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(LogFilePath, true))
                {
                    System.Text.StringBuilder sbLog = new System.Text.StringBuilder();

                    String[] ListText = iText.Split('|').ToArray();

                    foreach (String s in ListText)
                    {
                        sbLog.AppendLine(s);
                    }

                    outfile.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToString("hh:mm:ss tt"), sbLog.ToString()));
                }
            }
            catch
            {

            }
        }
    }
}
