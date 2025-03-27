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

        public static int ReadMacroOrAux(ushort handle, int macroLocation)
        {
            return macroLocation < 1000 ? ReadMacro(handle, (short)macroLocation) : Convert.ToInt32(ReadAuxMacro(handle, macroLocation));
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

        public static double ReadMacroDouble2(ushort handle, short macroLocation)
        {
            FocasLibBase.ODBM odbm = new FocasLibBase.ODBM();
            int ret = FocasLib.cnc_rdmacro(handle, macroLocation, 10, odbm);
            if (ret == 0)
            {
                return odbm.mcr_val == 0 && odbm.dec_val == -1 ? double.MaxValue : odbm.mcr_val / Math.Pow(10.0, odbm.dec_val);
            }
            else
            {
                Logger.WriteErrorLog("cnc_rdmacro() failed. return value is = " + ret);
            }
            return double.MaxValue;
        }

        public static short WriteMacro(ushort handle, short macroLocation, int value)
        {
            return FocasLib.cnc_wrmacro(handle, macroLocation, 10, value, 0);
        }

        public static short WriteMacro(ushort handle, short macroLocation, decimal value)
        {
            short decimalPlaces = BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
            short intValue = Convert.ToInt16(value * (decimal)Math.Pow(10, decimalPlaces));
            return FocasLib.cnc_wrmacro(handle, macroLocation, 10, intValue, decimalPlaces);
        }

        public static short WriteAuxMacro(ushort handle, int macroLocation, decimal value)
        {
            short decimalPlaces = BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
            int intValue = Convert.ToInt32(value * (decimal)Math.Pow(10, decimalPlaces));
            return FocasLib.cnc_wrpmacro(handle, macroLocation, intValue, decimalPlaces);
        }

        public static short WriteMacroOrAux(ushort handle, int macroLocation, decimal value)
        {
            return macroLocation < 1000 ? WriteMacro(handle, (short)macroLocation, (int)value) : WriteAuxMacro(handle, macroLocation, value);
        }

        /// <summary>
        /// currently only supports max 10 macro location read at a time
        /// </summary>
        /// <param name="startLoc"></param>
        /// <param name="EndLoc"></param>
        /// <returns>List of macro values</returns>
        public static List<double> ReadMacroRange(ushort handle, short startLoc, short EndLoc)
        {
            List<double> values = new List<double>();
            FocasLibBase.IODBMR odbmr = new FocasLibBase.IODBMR();
            //int ret = FocasLib.cnc_rdmacroRange(711, 720, 8 + (8 * 10), odbmr);
            int ret = FocasLib.cnc_rdmacror(handle, startLoc, EndLoc, 8 + (8 * 14), odbmr);
            if (ret == 0)
            {
                values.Add(GetMacroValue(odbmr.data.data1));
                values.Add(GetMacroValue(odbmr.data.data2));
                values.Add(GetMacroValue(odbmr.data.data3));
                values.Add(GetMacroValue(odbmr.data.data4));
                values.Add(GetMacroValue(odbmr.data.data5));
                values.Add(GetMacroValue(odbmr.data.data6));
                values.Add(GetMacroValue(odbmr.data.data7));
                values.Add(GetMacroValue(odbmr.data.data8));
                values.Add(GetMacroValue(odbmr.data.data9));
                values.Add(GetMacroValue(odbmr.data.data10));
                values.Add(GetMacroValue(odbmr.data.data11));
                values.Add(GetMacroValue(odbmr.data.data12));
                values.Add(GetMacroValue(odbmr.data.data13));
                values.Add(GetMacroValue(odbmr.data.data14));
            }
            else
            {
                Logger.WriteErrorLog("cnc_rdmacror() failed. return value is = " + ret);
            }
            return values;
        }

        public static List<double> ReadMacroRangeWithIgnore(ushort handle, short startLoc, short EndLoc, List<int> IgnoreList)
        {
            List<double> values = new List<double>();
            FocasLibBase.IODBMR odbmr = new FocasLibBase.IODBMR();

            short remainingLocs = (short)(EndLoc - startLoc + 1); // Calculate remaining locations to read
            int indexToCheck = startLoc;
            while (remainingLocs > 0)
            {
                short blockSize = Math.Min(remainingLocs, (short)14); // Read in chunks of 14 or remainingLocs if less
                int ret = FocasLib.cnc_rdmacror(handle, startLoc, (short)(startLoc + blockSize - 1), (short)(8 + (8 * blockSize)), odbmr);
                if (ret == 0)
                {
                    if (!IgnoreList.Contains(indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data1));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data2));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data3));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data4));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data5));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data6));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data7));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data8));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data9));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data10));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data11));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data12));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data13));
                    }
                    if (!IgnoreList.Contains(++indexToCheck))
                    {
                        values.Add(GetMacroValue(odbmr.data.data14));
                    }
                    indexToCheck++;
                    remainingLocs -= blockSize; // Update remaining locations
                    startLoc += blockSize; // Move startLoc to the next chunk
                }
                else
                {
                    Logger.WriteErrorLog("cnc_rdmacror() failed. return value is = " + ret);
                    break; // Exit loop in case of failure
                }
            }

            return values;
        }

        public static FocasLibBase.IODBMR_data GetPropertyValue(object obj, string propertyName)
        {
            return (FocasLibBase.IODBMR_data)obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
        }

        public static List<double> ReadAuxMacroRange(ushort handle, int startLoc, int EndLoc)
        {
            List<double> values = new List<double>();
            FocasLibBase.IODBPR odbpr = new FocasLibBase.IODBPR();
            int ret = FocasLib.cnc_rdpmacror(handle, startLoc, EndLoc, 8 + (8 * 14), odbpr);
            if (ret == 0)
            {
                values.Add(GetAuxMacroValue(odbpr.data.data1));
                values.Add(GetAuxMacroValue(odbpr.data.data2));
                values.Add(GetAuxMacroValue(odbpr.data.data3));
                values.Add(GetAuxMacroValue(odbpr.data.data4));
                values.Add(GetAuxMacroValue(odbpr.data.data5));
                values.Add(GetAuxMacroValue(odbpr.data.data6));
                values.Add(GetAuxMacroValue(odbpr.data.data7));
                values.Add(GetAuxMacroValue(odbpr.data.data8));
                values.Add(GetAuxMacroValue(odbpr.data.data9));
                values.Add(GetAuxMacroValue(odbpr.data.data10));
                values.Add(GetAuxMacroValue(odbpr.data.data11));
                values.Add(GetAuxMacroValue(odbpr.data.data12));
                values.Add(GetAuxMacroValue(odbpr.data.data13));
                values.Add(GetAuxMacroValue(odbpr.data.data14));
            }
            else
            {
                Logger.WriteErrorLog("cnc_rdpmacror() failed. return value is = " + ret);
            }
            return values;
        }

        public static List<double> ReadMacroOrAuxRange(ushort handle, int startLoc, int EndLoc, List<int> ignoreList)
        {
            if (startLoc < 1000)
            {
                if (ignoreList.Count == 1 && ignoreList[0] == 0)
                {
                    return ReadMacroRange(handle, (short)startLoc, (short)EndLoc);
                }
                else
                {
                    return ReadMacroRangeWithIgnore(handle, (short)startLoc, (short)EndLoc, ignoreList);
                }
                //return ReadMacroRangeWithIgnore(handle, (short)startLoc, (short)EndLoc, ignoreList);
            }
            else
            {
                //return ReadAuxMacroRange(handle, startLoc, EndLoc).Select(i => Convert.ToInt32(i)).ToList();
                return ReadAuxMacroRange(handle, startLoc, EndLoc);
            }
        }

        public static List<string> ReadAuxMacroRangeCustom(ushort handle, int startLoc, int EndLoc)
        {
            List<string> values = new List<string>();
            FocasLibBase.IODBPR odbpr = new FocasLibBase.IODBPR();
            int ret = FocasLib.cnc_rdpmacror(handle, startLoc, EndLoc, 8 + (8 * 14), odbpr);
            if (ret == 0)
            {
                values.Add(GetAuxMacroValueCustom(odbpr.data.data1)[0]);
                values.Add(GetAuxMacroValueCustom(odbpr.data.data1)[1].PadRight(4, '0'));
                values.Add(GetAuxMacroValueCustom(odbpr.data.data2)[0]);
                values.Add(GetAuxMacroValueCustom(odbpr.data.data2)[1].PadRight(3, '0'));
                values.Add(GetAuxMacroValueCustom(odbpr.data.data3)[0]);
                //string StDateValue = (GetAuxMacroValueCustom(odbpr.data.data3)[1].Length == 5) ? GetAuxMacroValueCustom(odbpr.data.data3)[1]+"0" : GetAuxMacroValueCustom(odbpr.data.data3)[1];
                string StDateValue = GetAuxMacroValueCustom(odbpr.data.data3)[1].PadRight(6, '0');
                values.Add(StDateValue);
                values.Add(GetAuxMacroValueCustom(odbpr.data.data4)[0]);
                //string StTimeValue = (GetAuxMacroValueCustom(odbpr.data.data4)[1].Length == 5) ? GetAuxMacroValueCustom(odbpr.data.data4)[1] + "0" : GetAuxMacroValueCustom(odbpr.data.data4)[1];
                string StTimeValue = GetAuxMacroValueCustom(odbpr.data.data4)[1].PadRight(6, '0');
                values.Add(StTimeValue);
                values.Add(GetAuxMacroValueCustom(odbpr.data.data5)[0]);
                //string EnDateValue = (GetAuxMacroValueCustom(odbpr.data.data5)[1].Length == 5) ? GetAuxMacroValueCustom(odbpr.data.data5)[1] + "0" : GetAuxMacroValueCustom(odbpr.data.data5)[1];
                string EnDateValue = GetAuxMacroValueCustom(odbpr.data.data5)[1].PadRight(6, '0');
                values.Add(EnDateValue);
                values.Add(GetAuxMacroValueCustom(odbpr.data.data6)[0]);
                //string EnTimeValue = (GetAuxMacroValueCustom(odbpr.data.data6)[1].Length == 5) ? GetAuxMacroValueCustom(odbpr.data.data6)[1] + "0" : GetAuxMacroValueCustom(odbpr.data.data6)[1];
                string EnTimeValue = GetAuxMacroValueCustom(odbpr.data.data6)[1].PadRight(6, '0');
                values.Add(EnTimeValue);
                values.Add(GetAuxMacroValueCustom(odbpr.data.data7)[0]);
                values.Add(GetAuxMacroValueCustom(odbpr.data.data7)[1]);
            }
            else
            {
                Logger.WriteErrorLog("cnc_rdpmacror() failed. return value is = " + ret);
            }
            return values;
        }

        public static double GetMacroValue(FocasLibBase.IODBMR_data data)
        {
            return data.mcr_val == 0 && data.dec_val == -1 ? 0 : data.mcr_val / Math.Pow(10.0, data.dec_val);
        }

        public static double GetAuxMacroValue(FocasLibBase.IODBPR_data data)
        {
            return data.mcr_val == 0 && data.dec_val == -1 ? 0 : (double)(data.mcr_val / Math.Pow(10.0, data.dec_val));
        }

        public static List<string> GetAuxMacroValueCustom(FocasLibBase.IODBPR_data data)
        {
            List<string> OpData = new List<string>();
            string DTvalue = GetAuxMacroValue(data).ToString();
            string[] SplitValues = DTvalue.Split('.');
            OpData.Add(SplitValues[0]);
            if (SplitValues.Length == 2)
            {
                OpData.Add(SplitValues[1]);
            }
            else
            {
                OpData.Add("0");
            }

            return OpData;
        }

        public static short ReadParametershort(ushort handle, short parameter)
        {
            short partsCount = 0;
            FocasLibBase.IODBPSD_1 para = new FocasLibBase.IODBPSD_1();
            short ret = FocasLib.cnc_rdparam(handle, parameter, 0, 8, para);
            if (ret == 0)
            {
                partsCount = para.idata;
            }
            else
            {
                Logger.WriteErrorLog(string.Format("Parameter : {0} - cnc_rdparam() failed. return value is = {1}", parameter, ret));
            }

            return partsCount;
        }

        public static int ReadParameterInt(ushort handle, short parameter)
        {
            int partsCount = 0;
            FocasLibBase.IODBPSD_1 para = new FocasLibBase.IODBPSD_1();
            short ret = FocasLib.cnc_rdparam(handle, parameter, 0, 8, para);
            if (ret == 0)
            {
                partsCount = para.ldata;
            }
            else
            {
                Logger.WriteErrorLog(string.Format("Parameter : {0} - cnc_rdparam() failed. return value is = {1}", parameter, ret));
            }
            return partsCount;
        }

        public static double ReadModalA(ushort handle)
        {
            double prograamdFeedRate = 0;
            FocasLibBase.ODBMDL_4 para = new FocasLibBase.ODBMDL_4();
            short ret = FocasLib.cnc_modal(handle, -2, 0, para);
            if (ret == 0)
            {
                int feed = para.raux1.data4.aux_data;
                _ = para.raux1.data4.flag1;
                byte flag2 = para.raux1.data4.flag2;

                double num = flag2 & 7;
                if (num == 0.0)
                {
                    return feed;
                }
                else
                {
                    prograamdFeedRate = feed / Math.Pow(10.0, num);
                }
                //partsCount = para.ldata;
            }
            else
            {
                // Logger.WriteErrorLog(string.Format("Parameter : {0} - cnc_rdparam() failed.
                // return value is = {1}", parameter, ret));
            }
            return prograamdFeedRate;
        }
    }
}