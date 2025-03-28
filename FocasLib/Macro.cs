using FocasLib;
using System;
using System.Collections.Generic;

namespace FocasLibrary
{
    public static partial class FocasData
    {
        public static int ReadMacro(ushort handle, short macroLocation)
        {
            FocasLibBase.ODBM odbm = new FocasLibBase.ODBM();
            int ret = FocasLib.cnc_rdmacro(handle, macroLocation, 10, odbm);
            if (ret == 0)
            {
                return odbm.mcr_val == 0 && odbm.dec_val == -1 ? -1 : (int)(odbm.mcr_val / Math.Pow(10.0, odbm.dec_val));
            }
            else
            {
                Logger.WriteErrorLog("cnc_rdmacro() failed. return value is = " + ret);
            }
            return -1;
        }

        public static double ReadAuxMacro(ushort handle, int macroLocation)
        {
            FocasLibBase.ODBPM odbpm = new FocasLibBase.ODBPM();
            int ret = FocasLib.cnc_rdpmacro(handle, macroLocation, odbpm);
            if (ret == 0)
            {
                return odbpm.mcr_val == 0 && odbpm.dec_val == -1 ? -1 : odbpm.mcr_val / Math.Pow(10.0, odbpm.dec_val);
            }
            else
            {
                Logger.WriteErrorLog("cnc_rdpmacro() failed. return value is = " + ret);
            }
            return -1;
        }

        public static double ReadMacroDouble(ushort handle, short macroLocation)
        {
            FocasLibBase.ODBM odbm = new FocasLibBase.ODBM();
            int ret = FocasLib.cnc_rdmacro(handle, macroLocation, 10, odbm);
            if (ret == 0)
            {
                return odbm.mcr_val == 0 && odbm.dec_val == -1 ? -1 : odbm.mcr_val / Math.Pow(10.0, odbm.dec_val);
            }
            else
            {
                Logger.WriteErrorLog("cnc_rdmacro() failed. return value is = " + ret);
            }
            return -1;
        }
    }
}