using System.Configuration;
using System.Data.SqlClient;
using System;
using System.IO;
using System.Threading;
using System.Data;
using System.Reflection;
using System.Text;

namespace DataCollection_Eval
{
    public static class ConnectionManager
    {       
        static readonly string conString = ConfigurationManager.ConnectionStrings["SQLConnectionString"]?.ToString();
        public static SqlConnection GetConnection()
        {
            bool writeDown = false;
            DateTime dt = DateTime.Now;
            SqlConnection conn = new SqlConnection(conString);
            do
            {
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    if (writeDown == false)
                    {
                        dt = DateTime.Now.AddHours(2);
                        Logger.WriteErrorLog(ex.Message);
                        writeDown = true;
                    }
                    if (dt < DateTime.Now)
                    {                                    
                        Logger.WriteErrorLog(ex.Message);
                        writeDown = false;                                       
                    }
                    Thread.Sleep(1000);
                }

            } while (conn.State != ConnectionState.Open);
            return conn;
        }
    }

}
