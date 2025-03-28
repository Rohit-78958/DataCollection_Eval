using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Collections;
using System.Threading;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text.Json;

namespace DataCollection_Eval
{
    public static class DatabaseAccess
    {
        public static string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static void PopulateGaugeInformationFromJson(string jsonFilePath)
        {
            try
            {
                string jsonData = File.ReadAllText(jsonFilePath);

                List<GaugeInformation> gaugeList = JsonSerializer.Deserialize<List<GaugeInformation>>(jsonData);

                DataTable gaugeTable = new DataTable();
                gaugeTable.Columns.Add("GaugeID", typeof(string));
                gaugeTable.Columns.Add("MachineID", typeof(string));
                gaugeTable.Columns.Add("DimensionsCount", typeof(int));

                foreach (var gauge in gaugeList)
                {
                    gaugeTable.Rows.Add(gauge.GaugeID, gauge.MachineID, gauge.DimensionsCount);
                }

                using (SqlConnection connection = ConnectionManager.GetConnection())
                {
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName = "GaugeInformation";

                        bulkCopy.ColumnMappings.Add("GaugeID", "GaugeID");
                        bulkCopy.ColumnMappings.Add("MachineID", "MachineID");
                        bulkCopy.ColumnMappings.Add("DimensionsCount", "DimensionsCount");

                        bulkCopy.WriteToServer(gaugeTable);
                    }
                }
                Logger.WriteDebugLog("Data successfully inserted into GaugeInformation using SqlBulkCopy.");
            }
            catch (FileNotFoundException ex)
            {
                Logger.WriteErrorLog($"Error: JSON file not found - {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog($"Unexpected error: {ex.Message}");
            }
        }

        public static List<MachineInfoDTO> GetTPMTrakMachine()
        {
            List<MachineInfoDTO> machines = new List<MachineInfoDTO>();
            string query = @"
                    SELECT 
                        mi.MachineId, 
                        mi.IP, 
                        mi.IPPortNo, 
                        mi.DAPEnabled, 
                        gi.GaugeID, 
                        gi.DimensionsCount AS GaugeDimensionsCount 
                    FROM MachineInformation mi
                    LEFT JOIN GaugeInformation gi ON mi.MachineID = gi.MachineID
                    WHERE mi.TPMTrakEnabled = 1 
                    ORDER BY mi.MachineID, gi.GaugeID";

            SqlConnection conn = ConnectionManager.GetConnection();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = null;

            try
            {
                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                MachineInfoDTO currentMachine = null;
                string lastMachineId = null;

                while (reader.Read())
                {
                    string ip = reader["IP"]?.ToString().Trim();
                    string currentMachineId = reader["MachineId"]?.ToString().Trim();

                    if (!string.IsNullOrEmpty(ip))
                    {
                        // If we're on a new MachineID, create a new MachineInfoDTO
                        if (currentMachine == null || lastMachineId != currentMachineId)
                        {
                            currentMachine = new MachineInfoDTO
                            {
                                MachineId = currentMachineId,
                                IpAddress = ip,
                                PortNo = Int32.TryParse(reader["IPPortNo"]?.ToString().Trim(), out int port) ? port : 0,
                                DataCollectionProtocol = Utility.GetProtocol(reader["DAPEnabled"]?.ToString())??"NA",
                                Gauges = new List<GaugeInformation>() // Initialize the list
                            };
                            machines.Add(currentMachine);
                            lastMachineId = currentMachineId;
                        }

                        string dataSource = currentMachine.DataCollectionProtocol;
                        if (dataSource.Equals("NA", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new Exception("No proper protocol specified!");
                        }
                        else if (dataSource.Equals("profinet", StringComparison.OrdinalIgnoreCase))
                        {
                            dataSource = "PLC";
                        }
                        else
                        {
                            dataSource = "Fanuc";
                        }

                        // Add gauge information if it exists
                        if (!reader.IsDBNull(reader.GetOrdinal("GaugeID")))
                        {
                            var gauge = new GaugeInformation
                            {
                                GaugeID = reader["GaugeID"]?.ToString().Trim(),
                                MachineID = currentMachine.MachineId,
                                DataSource = dataSource,
                                DimensionsCount = Convert.ToInt32(reader["GaugeDimensionsCount"]),
                                DimensionDetails = new List<DimensionDetails>()
                            };
                            currentMachine.Gauges.Add(gauge);   
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
            finally
            {
                reader?.Close();
                conn?.Close();
            }

            return machines;
        }

        public static void InsertGaugeInformation(MachineInfoDTO machine)
        {
            DataTable gaugeTransactions = new DataTable();
            gaugeTransactions.Columns.Add("MachineID", typeof(string));
            gaugeTransactions.Columns.Add("GaugeID", typeof(string));
            gaugeTransactions.Columns.Add("DimensionID", typeof(string));
            gaugeTransactions.Columns.Add("MeasuredValue", typeof(double));
            gaugeTransactions.Columns.Add("DataSource", typeof(string));

            // Populate the DataTable with rows.
            foreach (var gauge in machine.Gauges)
            {
                if (gauge.DimensionDetails == null || gauge.DimensionDetails.Count == 0)
                {
                    Logger.WriteDebugLog($"No DimensionDetails provided for MachineID {gauge.MachineID}");
                    continue;
                }
                foreach (var dimension in gauge.DimensionDetails)
                {
                    gaugeTransactions.Rows.Add(
                        gauge.MachineID,
                        gauge.GaugeID,
                        dimension.DimensionID,
                        dimension.MeasuredValue,
                        gauge.DataSource);
                }
            }

            using (SqlConnection connection = ConnectionManager.GetConnection())
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "GaugeTransactionData";

                    // Explicit column mappings
                    bulkCopy.ColumnMappings.Add("MachineID", "MachineID");
                    bulkCopy.ColumnMappings.Add("GaugeID", "GaugeID");
                    bulkCopy.ColumnMappings.Add("DimensionID", "DimensionID");
                    bulkCopy.ColumnMappings.Add("MeasuredValue", "MeasuredValue");
                    bulkCopy.ColumnMappings.Add("DataSource", "DataSource");

                    try
                    {
                        bulkCopy.WriteToServer(gaugeTransactions);
                        Logger.WriteDebugLog($"Successfully bulk inserted {gaugeTransactions.Rows.Count} gauge transactions.");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteErrorLog($"Error during bulk insert: {ex.Message}");
                    }
                }
            }
        }
    }
}