using EasyReportTool;
using EasyReportTool.Enums;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Management.Automation;
using System.Windows;
using System.Windows.Forms;
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
            m_notifyIcon.BalloonTipText = "Easy Report Tool has been minimised. Click the tray icon to show.";
            m_notifyIcon.BalloonTipTitle = "Easy Report Tool";
            m_notifyIcon.Text = "Easy Report Tool";
            m_notifyIcon.Icon = new System.Drawing.Icon("report_check.ico");
            m_notifyIcon.Click += new EventHandler(NotifyIcon_Click);
        }


        string targetPath = @"C:\AFCON\Support\";
        string projectPath = "";
        string projName = "";
        int interval = 0;
        const string REG_PATH_EVENTLOG = @"SYSTEM\CurrentControlSet\Services\Eventlog";
        static readonly string[] logsNames = new string[] { "System", "Application", "Pulse" };
        NotifyIcon m_notifyIcon = new NotifyIcon();


        public void OnClose(object sender, CancelEventArgs args)
        {
            m_notifyIcon.Dispose();
            m_notifyIcon = null;
        }

        private WindowState m_storedWindowState = WindowState.Normal;
        public void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                if (m_notifyIcon != null)
                    m_notifyIcon.ShowBalloonTip(2000);
            }
            else
                m_storedWindowState = WindowState;
        }
        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }

        void NotifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = m_storedWindowState;
        }
        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }

        private void BrowserBtn_Click(object sender, RoutedEventArgs e)
        {

            projectPath = Globals.ReportTool.GetPathProject();
            if (!string.IsNullOrEmpty(projectPath))
            {
                prjName.Visibility = Visibility.Visible;
                string[] pathArr = projectPath.Split('\\');
                string innerDir = pathArr[pathArr.Length - 1];
                projName = innerDir;
                prjName.Content = innerDir;
            }
            else
            {
                prjName.Visibility = Visibility.Hidden;
                projName = string.Empty;
                prjName.Content = string.Empty;
            }
        }

        //Start insertion data by chosen interval to CSV
        private void StartRecBtn_Click(object sender, RoutedEventArgs e)
        {
            if (oneMinRadio.IsChecked == false && fiveMinRadio.IsChecked == false && tenMinRadio.IsChecked == false && sixtyMinRadio.IsChecked == false)
            {
                System.Windows.MessageBox.Show("Choose the Interval before you start recording.", "Error");
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
            start_rec.Visibility = Visibility.Visible;
            Globals.CapturePulseProcess.CreateCSVFile(targetPath);
            Globals.CapturePulseProcess.InsertEachInterval(interval);
            System.Windows.MessageBox.Show("Record data started, you can miminize the program during the recording.", "Success");
            startRecordBtn.IsEnabled = false;
            startRecordBtn.Opacity = 40;
            saveStopBtn.IsEnabled = true;
            saveStopBtn.Opacity = 100;
            SysTrayBtn.IsEnabled = true;
            SysTrayBtn.Opacity = 100;
        }

        //Stop insertion data to CSV
        private void SaveStop_Click(object sender, RoutedEventArgs e)
        {
            Globals.CapturePulseProcess.StopRecord();
            System.Windows.MessageBox.Show("Recording data stopped, The CSV saved on C: --> AFCON --> Support", "Success");
            Process.Start(targetPath);
            startRecordBtn.IsEnabled = true;
            startRecordBtn.Opacity = 100;
            saveStopBtn.IsEnabled = false;
            saveStopBtn.Opacity = 40;
            SysTrayBtn.IsEnabled = false;
            SysTrayBtn.Opacity = 40;
            start_rec.Visibility = Visibility.Hidden;
        }

        //Increase the maximum log size of System, Application and Pulse 
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            start_rec.Visibility = Visibility.Hidden;
            saveStopBtn.IsEnabled = false;
            saveStopBtn.Opacity = 30;
            SysTrayBtn.IsEnabled = false;
            SysTrayBtn.Opacity = 40;
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
            createRepBtn.Opacity = 40;
            DateTime localDate = DateTime.Now;
            var date = localDate.Date;
            string dateNowFolder = date.ToString("dd-MM-yyyy");
            if (string.IsNullOrEmpty(projectPath))
            {
                System.Windows.MessageBox.Show("Project folder not selected.", "Error");
                return;
            }
            if (comCheckBox.IsChecked == false && dailyLogCheckBox.IsChecked == false && dumpCheckBox.IsChecked == false && eventlogsCheckBox.IsChecked == false)
            {
                System.Windows.MessageBox.Show("Please mark at least one log type.", "Error");
                return;
            }
            createRepBtn.IsEnabled = false;
            string reportDistPath = targetPath + dateNowFolder + "_" + projName;
            if (Directory.Exists(reportDistPath))
            {
                Directory.Delete(reportDistPath, true);
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
                Globals.ReportTool.DirectoryCopy(projectPath + @"\Communication", comFolder, false);
            }
            pbar.Value += 10;
            DoEvents();
            if (dailyLogCheckBox.IsChecked.Equals(true))
            {
                string comFolder = reportDistPath + @"\DailyLog";
                if (!Directory.Exists(comFolder))
                    Directory.CreateDirectory(comFolder);
                Globals.ReportTool.DirectoryCopy(projectPath + @"\DailyLog", comFolder, false);
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
                string localAppData = @"%localappdata%\CrashDumps";
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

            System.Windows.MessageBox.Show("Done to create reports, the files are saved in 'C:' --> Afcon --> Support.", "Success");
            Process.Start(targetPath);

            //Reset checkboxs, prgress bar and project path 
            createRepBtn.IsEnabled = true;
            comCheckBox.IsChecked = false;
            dailyLogCheckBox.IsChecked = false;
            dailyLogCheckBox.IsChecked = false;
            eventlogsCheckBox.IsChecked = false;
            dumpCheckBox.IsChecked = false;
            pbar.Value = 0;
            projectPath = "";
        }

        private void SysTrayBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (startRecordBtn.IsEnabled == false)
            {
                DialogResult windowStopRec = System.Windows.Forms.MessageBox.Show(
                    "Stop recording, Are you sure ?",
                    "Recording in progress",
                    MessageBoxButtons.OKCancel);
                e.Cancel = (windowStopRec == System.Windows.Forms.DialogResult.Cancel);
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
