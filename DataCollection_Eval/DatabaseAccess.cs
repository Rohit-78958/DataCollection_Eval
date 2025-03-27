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

namespace DataCollection_Eval
{
    public static class DatabaseAccess
    {
        public static string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
                    string ip = reader["IP"].ToString().Trim();
                    string currentMachineId = reader["MachineId"].ToString().Trim();

                    if (!string.IsNullOrEmpty(ip))
                    {
                        // If we're on a new MachineID, create a new MachineInfoDTO
                        if (currentMachine == null || lastMachineId != currentMachineId)
                        {
                            currentMachine = new MachineInfoDTO
                            {
                                MachineId = currentMachineId,
                                IpAddress = ip,
                                PortNo = Int32.TryParse(reader["IPPortNo"].ToString().Trim(), out int port) ? port : 0,
                                DataCollectionProtocol = Utility.GetProtocol(reader["DAPEnabled"].ToString()),
                                Gauges = new List<GaugeInformation>() // Initialize the list
                            };
                            machines.Add(currentMachine);
                            lastMachineId = currentMachineId;
                        }

                        string dataSource = currentMachine.DataCollectionProtocol;
                        if (dataSource.Equals("profinet", StringComparison.OrdinalIgnoreCase))
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
                                GaugeID = reader["GaugeID"].ToString().Trim(),
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
            SqlConnection connection = ConnectionManager.GetConnection();
            string query = "InsertGaugeTransaction"; 
            try
            {

                for (int gaugeCount = 0; gaugeCount < machine.Gauges.Count; gaugeCount++)
                {
                    GaugeInformation gaugeInformation = machine.Gauges[gaugeCount];
                    // Ensure DimensionDetails list is not null and limit to dimensionCount
                    if (gaugeInformation.DimensionDetails != null && gaugeInformation.DimensionDetails.Count > 0)
                    {
                        int insertedRows = 0;
                        for (int i = 0; i < gaugeInformation.DimensionDetails.Count; i++)
                        {
                            DimensionDetails dimension = gaugeInformation.DimensionDetails[i];

                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                command.CommandType = CommandType.StoredProcedure;

                                // Add parameters for the stored procedure
                                command.Parameters.AddWithValue("@MachineID", gaugeInformation.MachineID);
                                command.Parameters.AddWithValue("@GaugeID", gaugeInformation.GaugeID);
                                command.Parameters.AddWithValue("@DimensionID", dimension.DimensionID); // Use DimensionID from list
                                command.Parameters.AddWithValue("@MeasuredValue", Convert.ToDouble(dimension.MeasuredValue)); // Convert string to double
                                command.Parameters.AddWithValue("@DataSource", gaugeInformation.DataSource);

                                // Execute the command and track inserted rows
                                int rowsAffected = command.ExecuteNonQuery();
                                if (rowsAffected > 0)
                                {
                                    insertedRows += rowsAffected;
                                    Logger.WriteDebugLog($"Gauge Transaction Saved: MachineID {gaugeInformation.MachineID}, GaugeID {gaugeInformation.GaugeID}, DimensionID {dimension.DimensionID}");
                                }
                                else
                                {
                                    Logger.WriteDebugLog($"Gauge Transaction Save Failed: MachineID {gaugeInformation.MachineID}, GaugeID {gaugeInformation.GaugeID}, DimensionID {dimension.DimensionID}");
                                }
                            }
                        }

                        // Log overall success or failure
                        if (insertedRows > 0)
                        {
                            Logger.WriteDebugLog($"Successfully inserted {insertedRows} gauge transactions for MachineID {gaugeInformation.MachineID}");
                        }
                        else
                        {
                            Logger.WriteDebugLog($"No gauge transactions inserted for MachineID {gaugeInformation.MachineID}");
                        }
                    }
                    else
                    {
                        Logger.WriteDebugLog($"No DimensionDetails provided for MachineID {gaugeInformation.MachineID}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog($"Error inserting gauge transactions for MachineID {machine.MachineId}: {ex.Message}");
            }
            finally
            {
                connection?.Close();
            }
        }

    }
}