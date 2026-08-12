using System;
using Wingman.Services;

namespace Wingman.ViewModels
{
    public class LogViewerViewModel : ObservableObject
    {
        private string _logTitle = string.Empty;
        private string _logContent = string.Empty;

        public string LogTitle
        {
            get => _logTitle;
            set => SetProperty(ref _logTitle, value);
        }

        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        public LogViewerViewModel()
        {
            LogTitle = $"LOGS: {DateTime.Now:dd-MM-yyyy}";
            LogContent = LoggingService.ReadCurrentLog();
        }
    }
}
