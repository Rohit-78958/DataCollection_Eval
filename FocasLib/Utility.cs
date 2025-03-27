using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using FocasLib;

namespace FocasLibrary
{
    public static class Utility
    {
        public static short PMC_GetAdrsCode(string adrs)
        {
            if (string.IsNullOrEmpty(adrs)) return 0;
            switch (adrs.ToUpper())
            {
                case "G":
                    return 0;
                case "F":
                    return 1;
                case "Y":
                    return 2;
                case "X":
                    return 3;
                case "A":
                    return 4;
                case "R":
                    return 5;
                case "T":
                    return 6;
                case "K":
                    return 7;
                case "C":
                    return 8;
                case "D":
                    return 9;
                case "M":
                    return 10;
                case "N":
                    return 11;
                case "E":
                    return 12;
                case "Z":
                    return 13;
                default:
                    return -1;
            }
        }
    }
}

