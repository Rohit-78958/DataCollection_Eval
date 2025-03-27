using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DataCollection_Eval
{
    class Utility
    {
        public static bool CheckPingStatus(string ipAddress)
        {
            Ping ping = null;
            try
            {
                int retryCount = 2;
                while (true)
                {
                    ping = new Ping();
                    PingReply reply = ping.Send(ipAddress, 1000);
                    if (reply.Status == IPStatus.Success)
                    {
                        return true;
                    }
                    else
                    {
                        if (retryCount > 0)
                        {
                            retryCount--;
                            continue;
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
                return false;
            }
            finally
            {
                ping?.Dispose();
            }
        }

        public static string GetProtocol(string protocol)
        {
            if (protocol.Equals("0"))
            {
                return "Focas";
            }
            else if (protocol.Equals("1"))
            {
                return "Profinet";
            }
            else
            {
                return "Fanuc";
            }
        }

        public static string SafeFileName(string name)
        {
            StringBuilder str = new StringBuilder(name);
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                str = str.Replace(c, '_');
            }
            return str.ToString();
        }
    }
}
