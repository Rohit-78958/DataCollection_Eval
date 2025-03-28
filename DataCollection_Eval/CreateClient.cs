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
        private DateTime _nextCleanUp = DateTime.Now.Date;
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
            _nextCleanUp = DateTime.Now.Date.AddDays(1);

            if (Utility.CheckPingStatus(_ipAddress))
            {
                Plc plc = null;
                int dimensionCount;
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
                        if (_nextCleanUp < DateTime.Now)
                        {
                            if (Monitor.TryEnter(_lockerReleaseMemory, 100))
                            {
                                try
                                {
                                    if (_nextCleanUp < DateTime.Now)
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
                                    _nextCleanUp = _nextCleanUp.Date.AddDays(1);
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

                    #region Profinet PLC
                    if (_protocol.Equals("profinet", StringComparison.OrdinalIgnoreCase))
                    {
                        plc = new Plc(CpuType.S71200, machineDTO.IpAddress, 0, 1);
                        plc.Open();

                        try
                        {
                            string dbNumber = ConfigurationManager.AppSettings["DBNumber"]?.ToString();
                            string plcDoubleAddress1 = ConfigurationManager.AppSettings["PLCDoubleAddress1"]?.ToString();
                            string plcDoubleAddress2 = ConfigurationManager.AppSettings["PLCDoubleAddress2"]?.ToString();
                            string plcDoubleAddress3 = ConfigurationManager.AppSettings["PLCDoubleAddress3"]?.ToString();

                            // Refactored loop for updating each gauge's dimensions.
                            for (int gauge = 0; gauge < machineDTO.Gauges.Count; gauge++)
                            {
                                GaugeInformation gaugeInformation = machineDTO.Gauges[gauge];
                                // Clear previous dimension details
                                gaugeInformation.DimensionDetails.Clear();
                                dimensionCount = gaugeInformation.DimensionsCount;

                                if (plc.IsConnected && dimensionCount > 0)
                                {
                                    // Array of PLC addresses to read from
                                    string[] plcAddresses = { plcDoubleAddress1, plcDoubleAddress2, plcDoubleAddress3 };

                                    foreach (string address in plcAddresses)
                                    {
                                        // Create and add the dimension detail
                                        gaugeInformation.DimensionDetails.Add(CreateDimensionDetail(ref plc, gaugeInformation.GaugeID, dimensionCount, dbNumber, address));
                                        dimensionCount--;
                                    }
                                }

                                // Update the gauge information in the collection (if necessary)
                                machineDTO.Gauges[gauge] = gaugeInformation;
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
                    #endregion

                    #region FOCAS: Fanuc
                    else if (_protocol.Equals("focas", StringComparison.OrdinalIgnoreCase))
                    {
                        short fanucDoubleMacroLocation1 = short.Parse(ConfigurationManager.AppSettings["fanucDoubleMacroLocation1"]);
                        short fanucDoubleMacroLocation2 = short.Parse(ConfigurationManager.AppSettings["fanucDoubleMacroLocation2"]);
                        short fanucDoubleMacroLocation3 = short.Parse(ConfigurationManager.AppSettings["fanucDoubleMacroLocation3"]);
                        short fanucDoubleMacroLocation4 = short.Parse(ConfigurationManager.AppSettings["fanucDoubleMacroLocation4"]);

                        try
                        {
                            int ret = FocasLibrary.FocasLib.cnc_allclibhndl3(_ipAddress, (ushort)_portNo, 10, out focasLibHandle);
                            if (ret == 0)
                            {
                                for (int gauge = 0; gauge < machineDTO.Gauges.Count; gauge++)
                                {
                                    GaugeInformation gaugeInformation = machineDTO.Gauges[gauge];

                                    // Clear previous dimension details.
                                    gaugeInformation.DimensionDetails.Clear();
                                    dimensionCount = gaugeInformation.DimensionsCount;

                                    if (dimensionCount > 0)
                                    {
                                        // Array of Focas macro locations.
                                        short[] macroLocations = { fanucDoubleMacroLocation1, fanucDoubleMacroLocation2, fanucDoubleMacroLocation3, fanucDoubleMacroLocation4 };

                                        foreach (short location in macroLocations)
                                        {
                                            // Create and add the dimension detail.
                                            gaugeInformation.DimensionDetails.Add(CreateFocasDimensionDetail(gaugeInformation.GaugeID, dimensionCount, focasLibHandle, location));
                                            dimensionCount--;
                                        }
                                    }

                                    // Update the gauge information in the collection (if needed).
                                    machineDTO.Gauges[gauge] = gaugeInformation;
                                }

                                DatabaseAccess.InsertGaugeInformation(machineDTO);

                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteErrorLog("Exception inside main while loop Exception ex : " + ex.ToString());
                            Thread.Sleep(10000);
                            if(focasLibHandle != ushort.MinValue)
                                FocasLibrary.FocasLib.cnc_freelibhndl(focasLibHandle);
                        }
                    }
                    #endregion

                    Thread.Sleep(1000);
                }
            }
            else
            {
                Logger.WriteErrorLog($"{_ipAddress} for {_machineId} is not reachable");
            }
        }

        // Helper method to read a value using Profinet PLC and create a DimensionDetails instance.
        private DimensionDetails CreateDimensionDetail(ref Plc plc, string gaugeId, int dimensionCount, string dbNumber, string plcAddress)
        {
            try
            {
                double readValue = ((uint)plc?.Read($"DB{dbNumber}.DBD{plcAddress}")).ConvertToDouble();
                return new DimensionDetails
                {
                    DimensionID = $"{_machineId}_{gaugeId}_D{dimensionCount}",
                    MeasuredValue = readValue
                };
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("Exception inside CreateDimensionDetail() : " + ex.ToString());
                return null;
            }
        }

        // Helper method to read a value using Focas and create a DimensionDetails instance.
        private DimensionDetails CreateFocasDimensionDetail(string gaugeId, int dimensionCount, ushort focasLibHandle, short macroLocation)
        {
            try
            {
                double readValue = FocasLibrary.FocasData.ReadMacroDouble(focasLibHandle, macroLocation);
                return new DimensionDetails
                {
                    DimensionID = $"{_machineId}_{gaugeId}_D{dimensionCount}",
                    MeasuredValue = readValue
                };
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("Exception inside CreateFocasDimensionDetail() : " + ex.ToString());
                return null;
            }
        }
    }
}