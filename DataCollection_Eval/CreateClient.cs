using System.Configuration;
using System.Net;
using System.Text;
using System;
using System.Net.Sockets;
using S7.Net;
using System.Threading;
using FocasLibrary;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace DataCollection_Eval
{
    class CreateClient
    {
        private readonly string _ipAddress;
        private readonly int _portNo;
        private readonly string _machineId;
        private readonly string _protocol;
        private readonly MachineInfoDTO machineDTO;
        private readonly object _lockerReleaseMemory = new object();
        private DateTime _CDT = DateTime.Now.Date;
        public CreateClient(MachineInfoDTO machine)
        {
            this.machineDTO = machine;
            this._ipAddress = machine.IpAddress;
            this._portNo = machine.PortNo;
            this._machineId = machine.MachineId;
            this._protocol = string.IsNullOrEmpty(machine.DataCollectionProtocol) ? "FOCAS" : machine.DataCollectionProtocol;
        }

        public void GetClient()
        {
            TcpClient tcpClient = null;
            _CDT = DateTime.Now.Date.AddDays(1);

            if (Utility.CheckPingStatus(_ipAddress))
            {
                Plc plc = null;
                int dimensionCount = int.MaxValue;
                ushort focasLibHandle = ushort.MinValue;

                while (true)
                {
                    try
                    {
                        #region StopService
                        if (ServiceStop.stop_service == 1)
                        {
                            try
                            {
                                Logger.WriteDebugLog("Stopping the service. Request from Service manager to stop the service.");
                                if (plc != null && plc.IsConnected)
                                {
                                    plc.Close();
                                }

                                if(focasLibHandle != ushort.MinValue)
                                    FocasLibrary.FocasLib.cnc_freelibhndl(focasLibHandle);


                                break;
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteErrorLog(ex.Message);
                                break;
                            }
                        }
                        #endregion StopService

                        #region Handle_files
                        if (_CDT < DateTime.Now)
                        {
                            if (Monitor.TryEnter(_lockerReleaseMemory, 100))
                            {
                                try
                                {
                                    if (_CDT < DateTime.Now)
                                    {
                                        Logger.WriteDebugLog("clean up process started.");
                                        CleanUpProcess.DeleteFiles("Logs");
                                        CleanUpProcess.DeleteFiles("TPMFiles");
                                        GC.Collect();
                                        GC.WaitForPendingFinalizers();
                                        Thread.Sleep(1000 * 10);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteErrorLog(ex.Message);
                                }
                                finally
                                {
                                    _CDT = _CDT.Date.AddDays(1);
                                    Monitor.Exit(_lockerReleaseMemory);
                                }
                            }
                        }
                        #endregion Handle_files
                    }
                    catch (Exception exxx)
                    {
                        Logger.WriteErrorLog("Exception inside main while loop Exception exxx : " + exxx.ToString());
                        Thread.Sleep(10000);
                    }

                    if (_protocol.Equals("profinet", StringComparison.OrdinalIgnoreCase))
                    {
                        plc = new Plc(CpuType.S71200, machineDTO.IpAddress, 0, 1);
                        plc.Open();

                        try
                        {
                            string dbNumber = ConfigurationManager.AppSettings["DBNumber"]?.ToString();
                            string doubleAddress1 = ConfigurationManager.AppSettings["DoubleAddress1"]?.ToString();
                            string doubleAddress2 = ConfigurationManager.AppSettings["DoubleAddress2"]?.ToString();
                            string doubleAddress3 = ConfigurationManager.AppSettings["DoubleAddress3"]?.ToString();

                            for (int i = 0; i < machineDTO.Gauges.Count; i++)
                            {
                                GaugeInformation gaugeInformation = machineDTO.Gauges[i];

                                gaugeInformation.DimensionDetails.Clear();
                                dimensionCount = gaugeInformation.DimensionsCount;

                                if (plc.IsConnected && dimensionCount > 0)
                                {

                                    double readValue = ((uint)plc?.Read($"DB{dbNumber}.DBD{doubleAddress1}")).ConvertToDouble();
                                    DimensionDetails dimension = new DimensionDetails()
                                    {
                                        DimensionID = $"{_machineId}_{gaugeInformation.GaugeID}_D {dimensionCount--}",
                                        MeasuredValue = readValue
                                    };
                                    gaugeInformation.DimensionDetails.Add(dimension);

                                    double readValue2 = ((uint)plc?.Read($"DB{dbNumber}.DBD{doubleAddress2}")).ConvertToDouble();
                                    DimensionDetails dimension2 = new DimensionDetails()
                                    {
                                        DimensionID = $"{_machineId}_{gaugeInformation.GaugeID}_D {dimensionCount--}",
                                        MeasuredValue = readValue2
                                    };
                                    gaugeInformation.DimensionDetails.Add(dimension2);


                                    double readValue3 = ((uint)plc?.Read($"DB{dbNumber}.DBD{doubleAddress3}")).ConvertToDouble();
                                    DimensionDetails dimension3 = new DimensionDetails()
                                    {
                                        DimensionID = $"{_machineId}_{gaugeInformation.GaugeID}_D {dimensionCount--}",
                                        MeasuredValue = readValue3
                                    };
                                    gaugeInformation.DimensionDetails.Add(dimension3);
                                }

                                machineDTO.Gauges[i] = gaugeInformation;
                            }

                            DatabaseAccess.InsertGaugeInformation(machineDTO);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteErrorLog("Exception inside main while loop Exception ex : " + ex.ToString());
                            Thread.Sleep(10000);
                        }
                        finally
                        {
                            plc?.Close();
                        }
                    }


                    else if (_protocol.Equals("focas", StringComparison.OrdinalIgnoreCase))
                    {
                        short doubleMacroLocation1 = short.Parse(ConfigurationManager.AppSettings["DoubleMacroLocation1"]);
                        short doubleMacroLocation2 = short.Parse(ConfigurationManager.AppSettings["DoubleMacroLocation2"]);
                        short doubleMacroLocation3 = short.Parse(ConfigurationManager.AppSettings["DoubleMacroLocation3"]);
                        short doubleMacroLocation4 = short.Parse(ConfigurationManager.AppSettings["DoubleMacroLocation4"]);

                        try
                        {
                            int ret = FocasLibrary.FocasLib.cnc_allclibhndl3(_ipAddress, (ushort)_portNo, 10, out focasLibHandle);
                            if (ret == 0)
                            {
                                for (int i = 0; i < machineDTO.Gauges.Count; i++)
                                {
                                    GaugeInformation gaugeInformation = machineDTO.Gauges[i];

                                    gaugeInformation.DimensionDetails.Clear();
                                    dimensionCount = gaugeInformation.DimensionsCount;

                                    if (dimensionCount > 0)
                                    {

                                        double doubleMacroData = FocasLibrary.FocasData.ReadMacroDouble(focasLibHandle, doubleMacroLocation1);
                                        DimensionDetails dimension = new DimensionDetails()
                                        {
                                            DimensionID = $"{_machineId}_{gaugeInformation.GaugeID}_D{dimensionCount--}",
                                            MeasuredValue = doubleMacroData
                                        };
                                        gaugeInformation.DimensionDetails.Add(dimension);

                                        double doubleMacroData1 = FocasLibrary.FocasData.ReadMacroDouble(focasLibHandle, doubleMacroLocation2);
                                        DimensionDetails dimension1 = new DimensionDetails()
                                        {
                                            DimensionID = $"{_machineId}_{gaugeInformation.GaugeID}_D{dimensionCount--}",
                                            MeasuredValue = doubleMacroData1
                                        };
                                        gaugeInformation.DimensionDetails.Add(dimension1);

                                        double doubleMacroData2 = FocasLibrary.FocasData.ReadMacroDouble(focasLibHandle, doubleMacroLocation3);
                                        DimensionDetails dimension2 = new DimensionDetails()
                                        {
                                            DimensionID = $"{_machineId}_{gaugeInformation.GaugeID}_D{dimensionCount--}",
                                            MeasuredValue = doubleMacroData2
                                        };
                                        gaugeInformation.DimensionDetails.Add(dimension2);

                                        double doubleMacroData3 = FocasLibrary.FocasData.ReadMacroDouble(focasLibHandle, doubleMacroLocation4);
                                        DimensionDetails dimension3 = new DimensionDetails()
                                        {
                                            DimensionID = $"{_machineId}_{gaugeInformation.GaugeID}_D{dimensionCount--}",
                                            MeasuredValue = doubleMacroData3
                                        };
                                        gaugeInformation.DimensionDetails.Add(dimension3);
                                    }

                                    machineDTO.Gauges[i] = gaugeInformation;
                                }

                                DatabaseAccess.InsertGaugeInformation(machineDTO);

                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteErrorLog("Exception inside main while loop Exception ex : " + ex.ToString());
                            Thread.Sleep(10000);
                            FocasLibrary.FocasLib.cnc_freelibhndl(focasLibHandle);
                        }
                    }

                    Thread.Sleep(1000);
                }
            }
            else
            {
                Logger.WriteErrorLog($"{_ipAddress} for {_machineId} is not reachable");
            }
        }
    }
}