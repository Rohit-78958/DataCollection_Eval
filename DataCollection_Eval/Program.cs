using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace DataCollection_Eval
{
    static class Program
    {
        static void Main()
        {

#if (!DEBUG)
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");        
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] { new SmartDataService() };
            ServiceBase.Run(ServicesToRun);
#else

            Service service = new Service();
            service.StartDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#endif
        }
    }
}
