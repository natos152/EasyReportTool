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
        string memoryAmount;
        string cpuAmount;

        public string ProcessName { get => processName; set => processName = value; }
        public string RAMAmount { get => memoryAmount; set => memoryAmount = value; }
        public string CPUAmount { get => cpuAmount; set => cpuAmount = value; }

        public DataTemplate()
        {

        }

        public DataTemplate(string processName, string cpuAmount, string memoryAmount)
        {
            ProcessName = processName;
            CPUAmount = cpuAmount;
            RAMAmount = memoryAmount;
        }
    }
}
