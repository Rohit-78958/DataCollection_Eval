using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace DataCollection_Eval
{
    public static class Logger
    {
        static readonly string appPath;
        static readonly string enableLog;
        static Logger()
        {
            appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            enableLog = ConfigurationManager.AppSettings["EnableLogs"]?.ToString();
        }

        public static void WriteDebugLog(string str)
        {
            if (enableLog.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                CleanUpProcess.RenameLogFiles();            
                StreamWriter writer = null;
                try
                {
                    string progTime = String.Format("_{0:yyyyMMdd}", DateTime.Now);
                    string location = appPath + "\\Logs\\F-" + Thread.CurrentThread.Name + progTime + "-Status.txt";

                    writer = new StreamWriter(location, true, Encoding.UTF8, 8195);
                    writer.WriteLine(string.Format("{0} : Debug - {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffff"), str));
                    writer.Flush();
                }
                catch { }
                finally
                {
                    if (writer != null)
                    {
                        writer.Close();
                        writer.Dispose();
                    }
                }
            }
        }
        public static void WriteErrorLog(string str)
        {
            StreamWriter writer = null;
            try
            {
                CleanUpProcess.RenameLogFiles();
                string progTime = String.Format("_{0:yyyyMMdd}", DateTime.Now);
                string location = appPath + "\\Logs\\F-" + Thread.CurrentThread.Name + progTime + "-Status.txt";

                writer = new StreamWriter(location, true, Encoding.UTF8, 8195);
                writer.WriteLine(string.Format("{0} : Exception - {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffff"), str));
                writer.Flush();
            }
            catch { }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }
    }
}
