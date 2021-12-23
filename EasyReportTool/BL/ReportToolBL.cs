using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using log4net;
using System.Linq;

namespace EasyReportTool.BL
{
    public class ReportToolBL
    {
        const int MaxLogSize = 769999999;
        private static readonly ILog mLogger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void SetLogsMaxSize(string REG_PATH_EVENTLOG, string[] logsNames)
        {
            foreach (var item in logsNames)
            {
                SetRegKey(REG_PATH_EVENTLOG + "\\" + item, "MaxLogSize", MaxLogSize);
            }
        }

        public bool SetRegKey(string path, string key, int value)
        {
            try
            {
                RegistryKey keySystem = Registry.LocalMachine.OpenSubKey(path, true);
                if (keySystem == null)
                {
                    Debug.WriteLine("Registry key for this Event Log does not exist.");
                    mLogger.ErrorFormat($"Registry key for this Event Log does not exist");
                    return false;
                }
                else
                {
                    keySystem.SetValue(key, value);
                    Registry.LocalMachine.Close();
                    mLogger.InfoFormat($"Registry key for this Event Log {path} updated");
                }
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }


        //Get the project path by choose on folder browser dialog
        public string GetPathProject()
        {
            string installionPulsePath = @"C:\AFCON\Pulse\";
            using (var fbd = new FolderBrowserDialog())
            {
                if (!Directory.Exists(installionPulsePath))
                    fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                fbd.SelectedPath = installionPulsePath;
                DialogResult result = fbd.ShowDialog();
                string pulsePath = fbd.SelectedPath;
                if (!pulsePath.Contains(@"Pulse\"))
                {
                    System.Windows.MessageBox.Show("It's seems that chosen project is not from Pulse folder.\nChoose project from Pulse folder only.", "Warning");
                    pulsePath = "";
                    return pulsePath;
                }
                if (!pulsePath.Equals(""))
                {
                    mLogger.InfoFormat($"Project path chosen successfully {pulsePath}");
                    return pulsePath;
                }
                return string.Empty;
            }
        }

        //Copy files and folders and save them to distantion folder
        public void DirectoryCopy(string src, string dist, bool copySubDirs)
        {
            int counterFile = 0;
            // Get the subdirectories for the specified directory.
            DirectoryInfo dirTarget = new DirectoryInfo(dist);

            DirectoryInfo dirSrc = new DirectoryInfo(src);

            if (!dirSrc.Exists)
            {
                mLogger.ErrorFormat($"Path {dirSrc} not exist");
                return;
            }


            DirectoryInfo[] dirs = dirSrc.GetDirectories();
            if (dirs.Length == 0)
            {
                mLogger.InfoFormat($"No Folders on the path {src}");
            }

            FileInfo[] files = dirSrc.GetFiles().OrderBy(p => p.LastWriteTimeUtc).ToArray();
            if (files.Length == 0)
            {
                mLogger.InfoFormat($"No Files on the path {src}");
            }

            DirectoryInfo[] dirsTarget = dirTarget.GetDirectories();
            if (dirsTarget.Length == 0)
            {
                mLogger.InfoFormat($"No Folders on the target path {dist}");
            }

            FileInfo[] filesTarget = dirTarget.GetFiles();
            if (filesTarget.Length == 0)
            {
                mLogger.InfoFormat($"No Files on the target path {dist}");
            }

            if (files.Length > 10 && src.Contains("DailyLog"))
            {
                //Get the 10 first daily log files in the directory and copy them to the new location.
                for (int i = files.Length-1; i > 0; i--)
                {
                    if (counterFile == 10)
                        return;
                    string tempPath = Path.Combine(dist, files[i].Name);
                    files[i].CopyTo(tempPath, true);
                    counterFile++;
                }
            }
            else
            {
                // If copying subdirectories, copy them and their contents to new location.
                if (copySubDirs)
                {
                    foreach (DirectoryInfo subdir in dirs)
                    {
                        string tempPath = Path.Combine(dist, subdir.Name);
                        DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                    }
                }
                else
                {
                    //Get the files in the directory and copy them to the new location.
                    foreach (FileInfo file in files)
                    {
                        string tempPath = Path.Combine(dist, file.Name);
                        file.CopyTo(tempPath, false);
                    }
                }
            }
        }
    }
}