using System;
using System.IO;

namespace Wingman.Services
{
    public static class LoggingService
    {
        private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        private static readonly object LogLock = new object();

        public static void WriteLog(string message, string category = "INFO")
        {
            try
            {
                lock (LogLock)
                {
                    if (!Directory.Exists(LogDir))
                    {
                        Directory.CreateDirectory(LogDir);
                    }

                    string filename = $"dashboard_log_{DateTime.Now:ddMMyyyy}.txt";
                    string filepath = Path.Combine(LogDir, filename);

                    string timestamp = DateTime.Now.ToString("HH:mm:ss");
                    string line = $"[{timestamp}] [{category.ToUpper()}] {message}\n";

                    File.AppendAllText(filepath, line);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging Error: {ex.Message}");
            }
        }

        public static string ReadCurrentLog()
        {
            try
            {
                lock (LogLock)
                {
                    string filename = $"dashboard_log_{DateTime.Now:ddMMyyyy}.txt";
                    string filepath = Path.Combine(LogDir, filename);

                    if (File.Exists(filepath))
                    {
                        return File.ReadAllText(filepath);
                    }
                    return $"Log file not found: {filepath}";
                }
            }
            catch (Exception ex)
            {
                return $"Error reading log: {ex.Message}";
            }
        }
    }
}
