using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyReportTool.Models
{
    public class PulseProcess
    {
        string processName;
        string processFullName;

        public string ProcessDisplayName { get => processName; set => processName = value; }
        public string ProcessFullName { get => processFullName; set => processFullName = value; }

        public PulseProcess(string processName, string processFullName)
        {
            ProcessDisplayName = processName;
            ProcessFullName = processFullName;
        }
    }
}
