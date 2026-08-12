using System;
using System.Runtime.InteropServices;

namespace Wingman.Services
{
    public static class PowerManagementService
    {
        [Flags]
        private enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        public static void SetKeepAwake(bool enable)
        {
            try
            {
                if (enable)
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
                    LoggingService.WriteLog("Screen Wake Lock: ENABLED (Fullscreen)", "SYSTEM");
                }
                else
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
                    LoggingService.WriteLog("Screen Wake Lock: DISABLED", "SYSTEM");
                }
            }
            catch (Exception ex)
            {
                LoggingService.WriteLog($"Wake Lock Error: {ex.Message}", "ERROR");
            }
        }
    }
}
