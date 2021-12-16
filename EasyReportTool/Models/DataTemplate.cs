using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyReportTool.Models
{
    public class DataTemplate
    {
        string processName;
        double memoryAmount;
        double cpuAmount;

        public string ProcessName { get => processName; set => processName = value; }
        public double MemoryAmount { get => memoryAmount; set => memoryAmount = value; }
        public double CpuAmount { get => cpuAmount; set => cpuAmount = value; }

        public DataTemplate()
        {

        }

        public DataTemplate(string processName, double cpuAmount, double memoryAmount)
        {
            ProcessName = processName;
            CpuAmount = cpuAmount;
            MemoryAmount = memoryAmount;
        }
    }
}
