using System;
using System.Windows;
using System.Windows.Threading;
using Wingman.Services;

namespace Wingman
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LoggingService.WriteLog($"UI Unhandled Exception: {e.Exception.Message}\n{e.Exception.StackTrace}", "CRASH");
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LoggingService.WriteLog($"Domain Unhandled Exception: {ex.Message}\n{ex.StackTrace}", "CRASH");
            }
        }
    }
}
