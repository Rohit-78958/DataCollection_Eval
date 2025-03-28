using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

namespace DataCollection_Eval
{
    public static class ServiceStop
    {
        public static volatile int stop_service = 0;
    }
}
