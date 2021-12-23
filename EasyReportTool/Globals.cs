using EasyReportTool.BL;
using EasyReportTool.Models;

namespace EasyReportTool
{
    public static class Globals
    {
        static Globals()
        {
            CapturePulseProcess = new CapturePulseProcessBL();
            ReportTool = new ReportToolBL();
        }

        #region BL
        public static ReportToolBL ReportTool { get; set; }
        public static CapturePulseProcessBL CapturePulseProcess { get; set; }
        #endregion

        #region Model
        public static DataTemplate DataTemplate { get; set; }

        public static PulseProcess PulseProcess { get; set; }

        #endregion


    }
}
