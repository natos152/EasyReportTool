using EasyReportTool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Timers;

namespace EasyReportTool
{
    public class CapturePulseProcessBL
    {
        #region Veribals and Objects
        readonly Timer timer = new Timer();
        readonly DateTime localDate = DateTime.Now;
        readonly StringBuilder sbColumn = new StringBuilder();
        string folderPath = "";
        string filePath = "";
        readonly string delimiter = ",";
        string fixProcName = "";
        #endregion

        #region Lists
        readonly List<DataTemplate> dataTemplates = new List<DataTemplate>();
        readonly List<string> PulseProcess = new List<string>{
                    "Afcon.Pcim.Server",
                    "Afcon.Pcim.Server.Service.AlarmPublisher",
                    "Afcon.Pcim.Server.Service.DataSheetPublisher",
                    "Afcon.Pulse.Server.Service.DataLoggerPublisher",
                    "Afcon.Pcim.Server.Service.DDEAdaptor",
                    "Afcon.Pcim.Server.Service.Management",
                    "Afcon.Pcim.Server.Service.EventManagementPublisher",
                    "Afcon.Pcim.Server.Service.OPCAdaptor",
                    "Afcon.Pcim.Server.Service.Scheduler",
                    "Afcon.Pcim.Server.Service.SimulatorPublisher",
                    "Afcon.Pcim.Server.Service.CallbackAdaptor",
                    "Afcon.Pcim.WorkStation",
                    "Rtm",
                    "sqlservr",
                    "Almh",
                    "Dbsr"
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
        /// Create file in the directory of targetPath.
        /// if file exist, deleted it...
        /// Write firs line in file......
        /// </summary>
        /// <param name="targetPath"> this param is the path to the directory containing the .......</param>
        public void CreateCSVFile(string targetPath)
        {
            var date = localDate.Date;
            string dateNowFolder = date.ToString("dd-MM-yyyy");
            //Create a folder with date now to save the CSV file

            folderPath = targetPath + dateNowFolder + "_Proccess";
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            filePath = folderPath + @"\PulseProcess.csv";
            string[] files = Directory.GetFiles(folderPath);
            int count = files.Count(file => { return file.Contains(filePath); });

            filePath = (count == 0) ? filePath : String.Format("{0}({1}).csv", filePath.Split('.')[0], count + 1);


            if (IsFileLocked(filePath))
            {
                System.Windows.MessageBox.Show("The CSV is open, please close it before.", "Error");
                return;
            }

            sbColumn.Append("time_stamp");
            sbColumn.Append(delimiter);

            for (int i = 0; i < PulseProcess.Count; i++)
            {
                if (PulseProcess[i].Contains("Service"))
                {
                    int indexSt = PulseProcess[i].LastIndexOf(".");
                    fixProcName = PulseProcess[i].Substring(indexSt + 1);
                }
                else
                    fixProcName = PulseProcess[i];

                sbColumn.Append("CPU-" + fixProcName);
                sbColumn.Append(delimiter);
                sbColumn.Append("Memory-" + fixProcName);
                sbColumn.Append(delimiter);

            }

            AppendLineDataToCSV();
            System.Windows.MessageBox.Show("Record data started, Please dont close the Program.", "Success");
        }

        public void AppendLineDataToCSV()
        {
            sbColumn.AppendLine();
            sbColumn.Append(DateTime.Now);
            sbColumn.Append(delimiter);

            var dataList = GetDataFromTaskManager();

            for (int i = 0; i < dataList.Count; i++)
            {
                sbColumn.Append(dataList[i].CpuAmount.ToString());
                sbColumn.Append(delimiter);
                sbColumn.Append(dataList[i].MemoryAmount.ToString());
                sbColumn.Append(delimiter);
            }
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.Write(sbColumn);
                sbColumn.Clear();
            }
        }

        public List<DataTemplate> GetDataFromTaskManager()
        {
            dataTemplates.Clear();
            var CurrentRunningProcs = Process.GetProcesses();
            //System.Diagnostics.Stopwatch asdf = new Stopwatch();
            //asdf.Start();
            foreach (string procName in PulseProcess)
            {
                var data = new DataTemplate();
                Process proc = CurrentRunningProcs.FirstOrDefault(p => p.ProcessName.Equals(procName));
                if (proc != null)
                {
                    data.ProcessName = procName;
                    var countCPU = new PerformanceCounter("Process", "% Processor Time", proc.ProcessName);
                    var mbCPU = countCPU.RawValue / 1024;
                    data.CpuAmount = mbCPU;
                    var countMemory = new PerformanceCounter("Process", "Working Set - Private", proc.ProcessName);
                    var mbMemory = countMemory.RawValue / 1024;
                    data.MemoryAmount = mbMemory;
                    dataTemplates.Add(data);
                }
                else
                {
                    data.ProcessName = procName;
                    data.MemoryAmount = 0;
                    data.CpuAmount = 0;
                    dataTemplates.Add(data);
                }
            }
            //Debug.WriteLine(asdf.Elapsed);
            return dataTemplates;
        }

        public void InsertEachInterval(int interval)
        {
            timer.Interval = interval;
            timer.Elapsed += OnTimedEvent;
            timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            AppendLineDataToCSV();
        }

        public void StopRecord()
        {
            timer.Stop();
            if (File.Exists(folderPath + ".zip"))
            {
                File.Delete(folderPath + ".zip");
            }
            ZipFile.CreateFromDirectory(folderPath, folderPath + ".zip");
        }
    }
}