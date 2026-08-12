using System;
using System.Collections.Generic;

namespace Wingman.Models
{
    public class PingTargetStatus
    {
        public string Host { get; set; } = string.Empty;
        public int LastMs { get; set; } = 0;
        public string Status { get; set; } = "init"; // ok, warn, crit, init
        public List<double> History { get; set; } = new List<double>();
    }

    public class DriveInfoModel
    {
        public string Name { get; set; } = string.Empty;
        public string VolumeLabel { get; set; } = string.Empty;
        public double Percent { get; set; } = 0;
        public double UsedGb { get; set; } = 0;
        public double TotalGb { get; set; } = 0;
        public string DriveType { get; set; } = "Fixed";
    }

    public class ProcessInfoModel
    {
        public int Pid { get; set; }
        public string Name { get; set; } = string.Empty;
        public double CpuPercent { get; set; }
        public double RamMb { get; set; }
    }

    public class NetConnInfoModel
    {
        public string Protocol { get; set; } = "TCP";
        public int LocalPort { get; set; }
        public string RemoteIp { get; set; } = string.Empty;
        public string State { get; set; } = "ESTABLISHED";
    }

    public class SystemState
    {
        public object Lock = new object();

        // Reactor Core
        public double CpuPercent { get; set; } = 0;
        public double RamPercent { get; set; } = 0;
        public double RamUsedGb { get; set; } = 0;
        public double RamTotalGb { get; set; } = 0;

        public double DiskPercent { get; set; } = 0;            // Real-time Physical Disk Active Load % (0-100%)
        public double DiskCapacityPercent { get; set; } = 0;    // OS Storage Capacity Used % (e.g. 69%)
        public double DiskUsedGb { get; set; } = 0;
        public double DiskTotalGb { get; set; } = 0;
        public double TotalStorageGb { get; set; } = 0;

        public double NetSentMb { get; set; } = 0;
        public double NetRecvMb { get; set; } = 0;
        public bool DiskRead { get; set; } = false;
        public bool DiskWrite { get; set; } = false;

        public string Uptime { get; set; } = "0d 0h 0m";
        public double BatteryPercent { get; set; } = 100;
        public bool BatteryPlugged { get; set; } = true;
        public bool HasBattery { get; set; } = false;
        public int ProcCount { get; set; } = 0;

        public List<string> TopProcs { get; set; } = new List<string>();
        public List<ProcessInfoModel> TopProcDetails { get; set; } = new List<ProcessInfoModel>();

        // GPU & Display Intel
        public string GpuName { get; set; } = "Integrated Graphics";
        public double GpuPercent { get; set; } = 0;
        public double VramUsedMb { get; set; } = 0;
        public double VramTotalMb { get; set; } = 0;
        public string DisplayRes { get; set; } = "1920x1080 @ 60Hz";

        // Multi-Drive Intel
        public List<DriveInfoModel> Drives { get; set; } = new List<DriveInfoModel>();

        // Network Ports & Connections
        public List<NetConnInfoModel> ActiveConnections { get; set; } = new List<NetConnInfoModel>();

        // Watchtower
        public Dictionary<string, PingTargetStatus> Pings { get; set; } = new Dictionary<string, PingTargetStatus>();

        // Network Intel
        public string LocalIp { get; set; } = "...";
        public string PublicIp { get; set; } = "...";
        public string Mac { get; set; } = "...";
        public string Gateway { get; set; } = "...";
        public string WifiSsid { get; set; } = "N/A";
        public string WifiSignal { get; set; } = "0%";
        public string WifiRadio { get; set; } = "...";
        public string WifiAuth { get; set; } = "...";

        // Hardware Specs
        public string SysOs { get; set; } = "...";
        public string SysUser { get; set; } = "...";
        public string SysCores { get; set; } = "...";
        public string SysRamTotal { get; set; } = "...";

        // Alerts
        public List<string> Alerts { get; set; } = new List<string>();

        public void AddAlert(string msg, string level)
        {
            if (!Alerts.Contains(msg))
            {
                Alerts.Add(msg);
            }
        }
    }
}
