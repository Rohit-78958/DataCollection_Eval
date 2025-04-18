﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataCollection_Eval
{
    public partial class Service: ServiceBase
    {
        private static readonly List<Thread> _threads = new List<Thread>();
        private static readonly List<CreateClient> _clients = new List<CreateClient>();
        private static readonly string _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string _jsonFilePath = Path.Combine(_appPath + "\\GaugeInfo.json");
        public Service()
        {
            InitializeComponent();
        }

        public void StartDebug()
        {
            OnStart(null);
        }   

        protected override void OnStart(string[] args)
        {
            Thread.CurrentThread.Name = "DataCollectionService";
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            try
            {
                if (!Directory.Exists(_appPath + "\\Logs\\"))
                {
                    Directory.CreateDirectory(_appPath + "\\Logs\\");
                }
                ServiceStop.stop_service = 0;


                DatabaseAccess.PopulateGaugeInformationFromJson(_jsonFilePath);

                List<MachineInfoDTO> machines = DatabaseAccess.GetTPMTrakMachine();
                if (machines.Count == 0)
                {
                    Logger.WriteDebugLog("No machine is enabled for TPM-Trak. modify the machine setting and restart the service.");
                    return;
                }

                foreach (MachineInfoDTO machine in machines)
                {
                    CreateClient client = new CreateClient(machine);
                    _clients.Add(client);

                    ThreadStart job = new ThreadStart(client.GetClient);
                    Thread thread = new Thread(job)
                    {
                        Name = Utility.SafeFileName(machine.MachineId),
                        CurrentCulture = new System.Globalization.CultureInfo("en-US")
                    };
                    thread.Start();
                    _threads.Add(thread);
                    Logger.WriteDebugLog($"Machine: {machine.MachineId} started for DataCollection with IP: {machine.IpAddress}, Port: {machine.PortNo}, Protocol: {machine.DataCollectionProtocol}");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = args.ExceptionObject as Exception;
            if (e != null)
            {
                Logger.WriteErrorLog("Unhandled Exception caught : " + e.ToString());
                Logger.WriteErrorLog("Runtime terminating:" + args.IsTerminating);
                var threadName = Thread.CurrentThread.Name;
                Logger.WriteErrorLog("Exception from Thread = " + threadName);
                Process p = Process.GetCurrentProcess();
                StringBuilder str = new StringBuilder();
                if (p != null)
                {
                    str.AppendLine("Total Handle count = " + p.HandleCount);
                    str.AppendLine("Total Threads count = " + p.Threads.Count);
                    str.AppendLine("Total Physical memory usage: " + p.WorkingSet64);

                    str.AppendLine("Peak physical memory usage of the process: " + p.PeakWorkingSet64);
                    str.AppendLine("Peak paged memory usage of the process: " + p.PeakPagedMemorySize64);
                    str.AppendLine("Peak virtual memory usage of the process: " + p.PeakVirtualMemorySize64);
                    Logger.WriteErrorLog(str?.ToString());
                }
                Thread.CurrentThread.Abort();
            }
        }

        protected override void OnStop()
        {
            if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
            {
                Thread.CurrentThread.Name = "SmartDataService";
            }
            ServiceStop.stop_service = 1;
            Thread.SpinWait(60000 * 10);
            try
            {
                Logger.WriteDebugLog("Service Stop request has come!!! ");
                Logger.WriteDebugLog("Thread count is: " + _threads.Count.ToString());
                foreach (Thread thread in _threads)
                {
                    Logger.WriteDebugLog("Stopping the machine - " + thread.Name);
                    RequestAdditionalTime(2000);
                    thread.Join();
                }
                _threads.Clear();
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
            finally
            {
                _clients.Clear();
            }
            Logger.WriteDebugLog("Service has stopped.");
        }
    }
}
