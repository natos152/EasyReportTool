using EasyReportTool;
using EasyReportTool.Enums;
using EasyReportTool.Models;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Management.Automation;
using System.Windows;
using System.Windows.Threading;

namespace EasyReportTool2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        string targetPath = @"C:\AFCON\Support\";
        string projectPath = "";
        string projName = "";
        int interval = 0;
        const string REG_PATH_EVENTLOG = @"SYSTEM\CurrentControlSet\Services\Eventlog\";
        static readonly string[] logsNames = new string[] { "System", "Application", "Pulse" };

        private void BrowserBtn_Click(object sender, RoutedEventArgs e)
        {

            projectPath = Globals.ReportTool.GetPathProject();
            //string.IsNullOrWhiteSpace()
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                prjName.Visibility = Visibility.Visible;
                string[] pathArr = projectPath.Split('\\');
                string innerDir = pathArr[pathArr.Length - 1];
                projName = innerDir;
                prjName.Content = innerDir;
            }
        }

        //Start insertion data by chosen interval to CSV
        private void StartRecBtn_Click(object sender, RoutedEventArgs e)
        {
            if (oneMinRadio.IsChecked == false && fiveMinRadio.IsChecked == false && tenMinRadio.IsChecked == false && sixtyMinRadio.IsChecked == false)
            {
                MessageBox.Show("Please choose Interval before you start ro record data from Task Manager.", "Error");
                return;
            }
            if (oneMinRadio.IsChecked == true)
                interval = (int)TimeInterval.OneMin;
            if (fiveMinRadio.IsChecked == true)
                interval = (int)TimeInterval.FiveMin;
            if (tenMinRadio.IsChecked == true)
                interval = (int)TimeInterval.TenMin;
            if (sixtyMinRadio.IsChecked == true)
                interval = (int)TimeInterval.Hour;
            Globals.CapturePulseProcess.CreateCSVFile(targetPath);
            Globals.CapturePulseProcess.InsertEachInterval(interval);
            startRecordBtn.IsEnabled = false;
            saveStopBtn.IsEnabled = true;
        }

        //Stop insertion data to CSV
        private void SaveStop_Click(object sender, RoutedEventArgs e)
        {
            Globals.CapturePulseProcess.StopRecord();
            MessageBox.Show("Record data stopped, The CSV saved on C: --> AFCON --> Support", "Success");
            Process.Start(targetPath);
            startRecordBtn.IsEnabled = true;
            saveStopBtn.IsEnabled = false;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(1);
        }

        //Increase the maximum log size of System, Application and Pulse 
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            saveStopBtn.IsEnabled = false;
            Globals.ReportTool.SetLogsMaxSize(REG_PATH_EVENTLOG, logsNames);
        }

        //Refersh the progress bar during the click on the create button
        public void DoEvents()
        {
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }

        //Start button to copy data to support folder 
        private void CreateRepBtn_Click(object sender, RoutedEventArgs e)
        {
            createRepBtn.IsEnabled = false;
            DateTime localDate = DateTime.Now;
            var date = localDate.Date;
            string dateNowFolder = date.ToString("dd-MM-yyyy");
            if (projectPath.Equals(""))
            {
                MessageBox.Show("Not chosen folder project to create logs", "Error");
                return;
            }
            if (
                comCheckBox.IsChecked.Equals(false)
                && dailyLogCheckBox.IsChecked.Equals(false)
                && dumpCheckBox.IsChecked.Equals(false)
                && eventlogsCheckBox.Equals(false)
                )
            {
                MessageBox.Show("Not chosen report to create", "Error");
                return;
            }
            string reportDistPath = targetPath + dateNowFolder + "_" + projName;
            if (File.Exists(reportDistPath))
            {
                File.Delete(reportDistPath);
                Directory.CreateDirectory(reportDistPath);
            }

            if (File.Exists(reportDistPath + ".zip"))
                File.Delete(reportDistPath + ".zip");

            pbar.Value += 10;
            DoEvents();
            if (comCheckBox.IsChecked.Equals(true))
            {
                string comFolder = reportDistPath + @"\Communication";
                if (!Directory.Exists(comFolder))
                    Directory.CreateDirectory(comFolder);
                Globals.ReportTool.DirectoryCopy(projectPath + @"\Communication", comFolder, true);
            }
            pbar.Value += 10;
            DoEvents();
            if (dailyLogCheckBox.IsChecked.Equals(true))
            {
                string comFolder = reportDistPath + @"\DailyLog";
                if (!Directory.Exists(comFolder))
                    Directory.CreateDirectory(comFolder);
                Globals.ReportTool.DirectoryCopy(projectPath + @"\DailyLog", comFolder, true);
            }
            pbar.Value += 10;
            DoEvents();
            if (eventlogsCheckBox.IsChecked.Equals(true))
            {
                string comFolder = reportDistPath + @"\EventLogs";
                if (!Directory.Exists(comFolder))
                    Directory.CreateDirectory(comFolder);
                PowerShell ps = PowerShell.Create();
                string strSystem = $@"(Get-WmiObject -Class Win32_NTEventlogFile | Where-Object LogfileName -EQ 'System').BackupEventlog('{comFolder}\System.evtx')";
                string strPulse = $@"(Get-WmiObject -Class Win32_NTEventlogFile | Where-Object LogfileName -EQ 'Pulse').BackupEventlog('{comFolder}\Pulse.evtx')";
                string strApp = $@"(Get-WmiObject -Class Win32_NTEventlogFile | Where-Object LogfileName -EQ 'Application').BackupEventlog('{comFolder}\Application.evtx')";
                ps.AddScript(strSystem).Invoke();
                ps.AddScript(strPulse).Invoke();
                ps.AddScript(strApp).Invoke();
            }
            pbar.Value += 25;
            DoEvents();
            if (dumpCheckBox.IsChecked.Equals(true))
            {
                createRepBtn.IsEnabled = true;
                string docPulse = @"%UserProfile%\Documents\Pulse";
                string localAppData = @"%localappdata%\CrashDumps\";
                string WERReportQueuePath = @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue";
                string docPulsePath = Environment.ExpandEnvironmentVariables(docPulse);
                string localAppDataPath = Environment.ExpandEnvironmentVariables(localAppData);

                string clientUserProfileFolder = reportDistPath + @"\Dumps\ClientModules\UserProfile";
                string clientlocalAppDataFolder = reportDistPath + @"\Dumps\ClientModules\localAppData";

                string serverDumpFolder = reportDistPath + @"\Dumps\ServerModules";
                if (!Directory.Exists(clientUserProfileFolder) && !Directory.Exists(clientlocalAppDataFolder))
                {
                    Directory.CreateDirectory(clientUserProfileFolder);
                    Directory.CreateDirectory(clientlocalAppDataFolder);
                    Directory.CreateDirectory(serverDumpFolder);
                }
                Globals.ReportTool.DirectoryCopy(docPulsePath, clientUserProfileFolder, false);
                Globals.ReportTool.DirectoryCopy(localAppDataPath, clientlocalAppDataFolder, false);
                Globals.ReportTool.DirectoryCopy(WERReportQueuePath, serverDumpFolder, false);
            }
            pbar.Value += 35;
            DoEvents();

            //Create a zip with the files and save it on same path
            ZipFile.CreateFromDirectory(reportDistPath, reportDistPath + ".zip");

            pbar.Value += 10;
            DoEvents();

            MessageBox.Show("Done to create reports, the files saved on 'C:' --> Afcon --> Support  !", "Success");
            Process.Start(targetPath);
            //Reset checkboxs, prgress bar and project path 
            comCheckBox.IsChecked = false;
            dailyLogCheckBox.IsChecked = false;
            dailyLogCheckBox.IsChecked = false;
            eventlogsCheckBox.IsChecked = false;
            dumpCheckBox.IsChecked = false;
            pbar.Value = 0;
            projectPath = "";
        }
    }
}
