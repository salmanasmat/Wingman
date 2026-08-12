using System;
using System.Collections.Generic;

namespace Wingman.Models
{
    public class PingTargetStatus
    {
        public string Host { get; set; } = string.Empty;
        public List<double> History { get; set; } = new List<double>();
        public string Status { get; set; } = "init"; // ok, warn, crit, init
        public int LastMs { get; set; } = 0;
    }

    public class SystemState
    {
        public object Lock { get; } = new object();

        // Reactor Metrics
        public double CpuPercent { get; set; }
        public double RamPercent { get; set; }
        public double RamUsedGb { get; set; }
        public double RamTotalGb { get; set; }
        public double NetSentMb { get; set; }
        public double NetRecvMb { get; set; }
        public double DiskPercent { get; set; }
        public double DiskUsedGb { get; set; }
        public double DiskTotalGb { get; set; }
        public double TotalStorageGb { get; set; }

        public List<string> TopProcs { get; set; } = new List<string>();
        public bool DiskRead { get; set; }
        public bool DiskWrite { get; set; }

        // Power & Uptime
        public string Uptime { get; set; } = "...";
        public double BatteryPercent { get; set; } = 100;
        public bool BatteryPlugged { get; set; } = true;
        public bool HasBattery { get; set; } = false;

        // Specs
        public string SysOs { get; set; } = string.Empty;
        public string SysUser { get; set; } = string.Empty;
        public string SysCores { get; set; } = string.Empty;
        public string SysRamTotal { get; set; } = string.Empty;
        public int ProcCount { get; set; }

        // Network Intel
        public string LocalIp { get; set; } = "Detecting...";
        public string PublicIp { get; set; } = "...";
        public string Mac { get; set; } = "...";
        public string Gateway { get; set; } = "...";
        public string WifiSsid { get; set; } = "N/A";
        public string WifiSignal { get; set; } = "0%";
        public string WifiRadio { get; set; } = "...";
        public string WifiAuth { get; set; } = "...";

        // Pings
        public Dictionary<string, PingTargetStatus> Pings { get; set; } = new Dictionary<string, PingTargetStatus>();

        // Alerts
        public List<string> Alerts { get; set; } = new List<string>();

        public void AddAlert(string msg, string level = "info")
        {
            lock (Lock)
            {
                string ts = DateTime.Now.ToString("HH:mm:ss");
                Alerts.Insert(0, $"[{ts}] {msg}");
                if (Alerts.Count > 5)
                {
                    Alerts.RemoveAt(Alerts.Count - 1);
                }
            }
        }
    }
}
