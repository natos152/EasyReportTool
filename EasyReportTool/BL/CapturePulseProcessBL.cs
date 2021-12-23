using EasyReportTool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Timers;
using log4net;


namespace EasyReportTool
{
    public class CapturePulseProcessBL
    {
        #region Veribals and Objects
        const long MAXSIZECSVFILE = 2500000;
        readonly Timer timer = new Timer();
        readonly DateTime localDate = DateTime.Now;
        readonly StringBuilder sbColumn = new StringBuilder();
        string folderPath = "";
        string filePath = "";
        readonly string delimiter = ",";
        private static readonly ILog mLogger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Dictionary<string, PerformanceCounter> PerformanceCountersDictCPU = null;
        private Dictionary<string, PerformanceCounter> PerformanceCountersDictRAM = null;
        #endregion

        #region Lists
        readonly List<DataTemplate> dataTemplatesList = new List<DataTemplate>();
        List<PulseProcess> pulseProcessesNames = new List<PulseProcess>()
        {
            new PulseProcess("Server","Afcon.Pcim.Server"),
            new PulseProcess("AlarmPublisher","Afcon.Pcim.Server.Service.AlarmPublisher"),
            new PulseProcess("DataSheetPublisher","Afcon.Pulse.Server.Service.DataLoggerPublisher"),
            new PulseProcess("DDEAdaptor","Afcon.Pcim.Server.Service.DDEAdaptor"),
            new PulseProcess("Management","Afcon.Pcim.Server.Service.Management"),
            new PulseProcess("EventManagementPublisher","Afcon.Pcim.Server.Service.EventManagementPublisher"),
            new PulseProcess("OPCAdaptor","Afcon.Pcim.Server.Service.OPCAdaptor"),
            new PulseProcess("Scheduler","Afcon.Pcim.Server.Service.Scheduler"),
            new PulseProcess("SimulatorPublisher","Afcon.Pcim.Server.Service.SimulatorPublisher"),
            new PulseProcess("CallbackAdaptor","Afcon.Pcim.Server.Service.CallbackAdaptor"),
            new PulseProcess("DataLoggerPublisher","Afcon.Pulse.Server.Service.DataLoggerPublisher"),
            new PulseProcess("WorkStation","Afcon.Pcim.WorkStation"),
            new PulseProcess("Rtm","Rtm"),
            new PulseProcess("sqlservr","sqlservr"),
            new PulseProcess("Almh","Almh"),
            new PulseProcess("Dbsr","Dbsr")
        };
        #endregion

        //Check if the CSV file is open on the background 
        public bool IsFileLocked(string filename)
        {
            bool Locked = false;
            try
            {
                FileStream fs =
                    File.Open(filename, FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.None);
                fs.Close();
            }
            catch (IOException)
            {
                Locked = true;
            }
            return Locked;
        }


        /// <summary>
        /// check if has already CSV file on the folder path and if has one, 
        /// add new one with number.
        /// </summary>
        public void CountCSVFilesAndCreateNewOne()
        {
            string[] files = Directory.GetFiles(folderPath);
            int count = files.Count(file => { return file.Contains("PulseProcess"); });
            filePath = (count == 0) ? filePath : String.Format("{0}({1}).csv", filePath.Split('.')[0], count + 1);
            mLogger.InfoFormat($"file created {filePath}");
        }


        /// <summary>
        /// Create file in the directory of targetPath.
        /// </summary>
        /// <param name="targetPath"></param>
        public void CreateCSVFile(string targetPath)
        {
            var date = localDate.Date;
            string dateNowFolder = date.ToString("dd-MM-yyyy");

            try
            {
                folderPath = targetPath + dateNowFolder + "_Proccess";
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                filePath = folderPath + @"\PulseProcess.csv";
                CountCSVFilesAndCreateNewOne();
            }
            catch (Exception)
            {
                mLogger.ErrorFormat($"file not created on the path: {filePath}");
            }


            if (IsFileLocked(filePath))
            {
                System.Windows.MessageBox.Show("The CSV is open, please close it before.", "Error");
                return;
            }

            sbColumn.Append("time_stamp");
            sbColumn.Append(delimiter);

            for (int i = 0; i < pulseProcessesNames.Count; i++)
            {
                sbColumn.Append("CPU-" + pulseProcessesNames[i].ProcessDisplayName);
                sbColumn.Append(delimiter);
                sbColumn.Append("RAM-" + pulseProcessesNames[i].ProcessDisplayName);
                sbColumn.Append(delimiter);
            }
            try
            {
                AppendLineDataToCSV();
                mLogger.InfoFormat($"line of data added to {filePath}");
            }
            catch (Exception)
            {
                mLogger.ErrorFormat($"line of data not added to {filePath}");
            }
        }

        /// <summary>
        /// add the line with data from the task manager.
        /// </summary>
        public void AppendLineDataToCSV()
        {
            sbColumn.AppendLine();
            sbColumn.Append(DateTime.Now);
            sbColumn.Append(delimiter);

            var dataList = GetDataFromTaskManager();

            for (int i = 0; i < dataList.Count; i++)
            {
                sbColumn.Append(dataList[i].CPUAmount);
                sbColumn.Append(delimiter);
                sbColumn.Append(dataList[i].RAMAmount);
                sbColumn.Append(delimiter);
            }
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.Write(sbColumn);
                sbColumn.Clear();
            }
            long _fileSize = new FileInfo(filePath).Length;
            if (_fileSize == MAXSIZECSVFILE)
            {
                mLogger.InfoFormat($"The file {filePath} is on max size.");
                CountCSVFilesAndCreateNewOne();
            }
        }


        /// <summary>
        /// Create a Dictionary with data to improve preformence of retrive data to CSV
        /// Return list of data with RAM and CPU
        /// </summary>
        /// <returns></returns>
        public List<DataTemplate> GetDataFromTaskManager()
        {
            dataTemplatesList.Clear();
            var CurrentRunningProcs = Process.GetProcesses();
            //fill the Dictionary first time with PerformanceCounter of cpu and ram and return the list with the seconed line of data also
            if (PerformanceCountersDictCPU == null && PerformanceCountersDictRAM == null)
            {
                PerformanceCountersDictCPU = new Dictionary<string, PerformanceCounter>();
                PerformanceCountersDictRAM = new Dictionary<string, PerformanceCounter>();

                foreach (PulseProcess pulseProcName in pulseProcessesNames)
                {

                    DataTemplate dataTemplate = new DataTemplate();
                    Process pulseProcess = CurrentRunningProcs.FirstOrDefault(
                        runningProc => runningProc.ProcessName == pulseProcName.ProcessFullName
                        );
                    if (pulseProcess != null)
                    {
                        dataTemplate.ProcessName = pulseProcName.ProcessDisplayName;
                        var counterCPU = new PerformanceCounter("Process", "% Processor Time", pulseProcess.ProcessName);
                        PerformanceCountersDictCPU.Add(pulseProcName.ProcessDisplayName, counterCPU);
                        var mbCPU = counterCPU.NextValue() / Environment.ProcessorCount;
                        dataTemplate.CPUAmount = mbCPU.ToString() + "%";

                        var counterRAM = new PerformanceCounter("Process", "Working Set - Private", pulseProcess.ProcessName);
                        PerformanceCountersDictRAM.Add(pulseProcName.ProcessDisplayName, counterRAM);
                        var mbRAM = (counterRAM.NextValue() / 1024) / 1024;
                        dataTemplate.RAMAmount = mbRAM.ToString("0.0");
                        dataTemplatesList.Add(dataTemplate);
                    }
                    else
                    {
                        dataTemplate.ProcessName = pulseProcName.ProcessDisplayName;
                        dataTemplate.RAMAmount = "-";
                        dataTemplate.CPUAmount = "-";
                        dataTemplatesList.Add(dataTemplate);
                    }
                }
                return dataTemplatesList;
            }
            else
            {
                foreach (PulseProcess pulseProcName in pulseProcessesNames)
                {
                    DataTemplate dataTemplate = new DataTemplate();
                    Process pulseProcess = CurrentRunningProcs.FirstOrDefault(
                        runningProc => runningProc.ProcessName == pulseProcName.ProcessFullName
                        );
                    if (pulseProcess != null && PerformanceCountersDictCPU.ContainsKey(pulseProcName.ProcessDisplayName))
                    {
                        PerformanceCounter dataCPU = PerformanceCountersDictCPU[pulseProcName.ProcessDisplayName];
                        PerformanceCounter dataRAM = PerformanceCountersDictRAM[pulseProcName.ProcessDisplayName];
                        dataTemplate.ProcessName = pulseProcName.ProcessDisplayName;

                        var processCPU = dataCPU.NextValue() / Environment.ProcessorCount;
                        dataTemplate.CPUAmount = processCPU.ToString() + "%";

                        var mbRAM = (dataRAM.NextValue() / 1024) / 1024;
                        dataTemplate.RAMAmount = mbRAM.ToString("0.0");

                        dataTemplatesList.Add(dataTemplate);
                    }
                    else
                    {
                        dataTemplate.ProcessName = pulseProcName.ProcessDisplayName;
                        dataTemplate.CPUAmount = "-";
                        dataTemplate.RAMAmount = "-";
                        dataTemplatesList.Add(dataTemplate);
                    }
                }
                return dataTemplatesList;
            }
        }

        /// <summary>
        /// each interval append line to csv
        /// </summary>
        /// <param name="interval"></param>
        public void InsertEachInterval(int interval)
        {
            timer.Interval = interval;
            timer.Elapsed += OnTimedEvent;
            timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                AppendLineDataToCSV();
            }
            catch (Exception)
            {
                mLogger.ErrorFormat($"line of data is not added to {filePath}");
            }
        }

        public void StopRecord()
        {
            timer.Stop();
            try
            {
                if (File.Exists(folderPath + ".zip"))
                {
                    File.Delete(folderPath + ".zip");
                }
                ZipFile.CreateFromDirectory(folderPath, folderPath + ".zip");
                mLogger.InfoFormat($"Zip file created successfully in the path -> {folderPath}");

            }
            catch (Exception)
            {
                mLogger.WarnFormat($"Zip file not created in the path -> {folderPath}, Zip file updated.");
            }
        }
    }
}