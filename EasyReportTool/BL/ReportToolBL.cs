using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace EasyReportTool.BL
{
    public class ReportToolBL
    {
        const int MaxLogSize = 769999999;

        public void SetLogsMaxSize(string REG_PATH_EVENTLOG, string[] logsNames)
        {
            foreach (var item in logsNames)
            {
                SetRegKey(REG_PATH_EVENTLOG + "\\" + item, "MaxLogSize", MaxLogSize.ToString());
           }
        }

        public bool SetRegKey(string path, string key, string value)
        {
            try
            {
                RegistryKey keySystem = Registry.LocalMachine.OpenSubKey(path, true);
                if (keySystem == null)
                {
                    Debug.WriteLine("Registry key for this Event Log does not exist.");
                    return false;
                }
                else
                {
                    keySystem.SetValue(key, value);
                    Registry.LocalMachine.Close();
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
                    System.Windows.MessageBox.Show("It's seems that you not choose project from Pulse folder.\nChoose project from Pulse only ! .", "Warning");
                    return string.Empty;
                }
                if (!pulsePath.Equals(""))
                    return pulsePath;
                return string.Empty;
            }
        }

        //Function to direct copy files and save them to distantion folder
        public void DirectoryCopy(string src, string dist, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dirTarget = new DirectoryInfo(dist);

            DirectoryInfo dirSrc = new DirectoryInfo(src);

            if (!dirSrc.Exists)
                return;

            DirectoryInfo[] dirs = dirSrc.GetDirectories();
            FileInfo[] files = dirSrc.GetFiles();

            DirectoryInfo[] dirsTarget = dirTarget.GetDirectories();
            FileInfo[] filesTarget = dirTarget.GetFiles();

            if (src.Equals(src) && files.Length > 10)
            {
                if (filesTarget.Length > 1 || dirsTarget.Length > 1)
                {
                    int countFiles = 0;
                    foreach (var file in Directory.GetFiles(src))
                    {
                        if (countFiles == 11)
                        {
                            countFiles = 0;
                            return;
                        }
                        File.Copy(file, Path.Combine(dist, Path.GetFileName(file)), true);
                    }
                }
                else
                {
                    //Get the 10 first daily log files in the directory and copy them to the new location.
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (i == 11)
                            return;
                        string tempPath = Path.Combine(dist, files[i].Name);
                        files[i].CopyTo(tempPath, false);
                    }
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

                if (filesTarget.Length > 1 || dirsTarget.Length > 1)
                {
                    foreach (var file in Directory.GetFiles(src))
                        File.Copy(file, Path.Combine(dist, Path.GetFileName(file)), true);
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
