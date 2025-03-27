using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Configuration;

namespace DataCollection_Eval
{
    public static class CleanUpProcess
    {
        static readonly string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static void DeleteFiles(string folder)
        {           
            DirectoryInfo di = new DirectoryInfo(Path.Combine(appPath,folder));
            FileInfo[] files = di.GetFiles("*.txt");

            if (files.Length > 0)
            {
                int daysForDeleteFile = Convert.ToInt32(ConfigurationManager.AppSettings["DaysForDeleteFiles"]);
                foreach (FileInfo fi in files)
                {                    
                    TimeSpan ts = DateTime.Now.Subtract(fi.LastWriteTime);
                    int days = ts.Days + 1;                    
                    if (days >= daysForDeleteFile)
                    {
                        try
                        {
                            fi.Delete();
                        }
                        catch { }
                    }
                }
            }
        }

        public static void RenameLogFiles()
        {
            string progTime = String.Format("_{0:yyyyMMdd}", DateTime.Now);
            string location = appPath + "\\Logs\\F-" + Thread.CurrentThread.Name + progTime + "-Status.txt";
            FileInfo f = new FileInfo(location);
            if (f.Exists && f.Length > 2097152)
            {
                string newfile = appPath + "\\Logs\\" + (Path.GetFileNameWithoutExtension(f.Name)) + String.Format("_{0:HHmmss}", DateTime.Now) + ".txt";// + String.Format("{0:HHmmss}", DateTime.Now));
                try
                {
                    f.MoveTo(newfile);
                    Thread.Sleep(1000);
                    //Logger.WriteDebugLog( string.Format("File {0} has been renamed to {1}.", location, newfile));
                }
                catch(Exception ex)
                {
                    //Logger.WriteErrorLog(" RenameLogFiles(): " + ex.Message);
                }               
            }
        }

    }
}
