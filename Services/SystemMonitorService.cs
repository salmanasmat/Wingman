using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Wingman.Models;

namespace Wingman.Services
{
    public class SystemMonitorService
    {
        private readonly SystemState _state;
        private readonly NetworkIntelService _netIntelService;
        private readonly PingService _pingService;
        private readonly ConfigService _configService;
        private CancellationTokenSource? _cts;
        private Task? _monitorTask;

        private long _lastNetBytesSent = 0;
        private long _lastNetBytesRecv = 0;
        private DateTime _lastNetTime = DateTime.Now;

        private long _lastDiskReadCount = 0;
        private long _lastDiskWriteCount = 0;

        private int _pollCount = 0;
        private bool _fetchingNetInfo = false;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public SystemMonitorService(SystemState state, ConfigService configService)
        {
            _state = state;
            _configService = configService;
            _netIntelService = new NetworkIntelService();
            _pingService = new PingService();

            InitStaticSpecs();
        }

        private void InitStaticSpecs()
        {
            _state.SysOs = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
                foreach (var os in searcher.Get())
                {
                    if (os["Caption"] != null)
                    {
                        _state.SysOs = os["Caption"].ToString() ?? _state.SysOs;
                        break;
                    }
                }
            }
            catch { }

            _state.SysUser = Environment.UserName;

            int logicalCores = Environment.ProcessorCount;
            int physicalCores = logicalCores / 2 > 0 ? logicalCores / 2 : logicalCores;
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor");
                int phys = 0;
                foreach (var item in searcher.Get())
                {
                    if (item["NumberOfCores"] != null)
                    {
                        phys += Convert.ToInt32(item["NumberOfCores"]);
                    }
                }
                if (phys > 0) physicalCores = phys;
            }
            catch { }

            _state.SysCores = $"{physicalCores}P / {logicalCores}L";

            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                double totalGb = (double)memStatus.ullTotalPhys / (1024 * 1024 * 1024);
                _state.SysRamTotal = $"{Math.Round(totalGb, 1)} GB";
            }
            else
            {
                _state.SysRamTotal = "N/A";
            }
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            int targetIndex = 0;
            _pingService.SyncTargets(_state, _configService.Current.Targets);

            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            try { cpuCounter.NextValue(); } catch { }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    PollResources(cpuCounter);

                    if (_pollCount % 15 == 0 && !_fetchingNetInfo)
                    {
                        _fetchingNetInfo = true;
                        _ = Task.Run(async () =>
                        {
                            await _netIntelService.FetchNetworkDetailsAsync(_state);
                            _fetchingNetInfo = false;
                        }, token);
                    }

                    if (_pollCount % 300 == 0)
                    {
                        GC.Collect();
                    }

                    (string name, string host)? targetData = null;
                    lock (_state.Lock)
                    {
                        var targetNames = _state.Pings.Keys.ToList();
                        if (targetNames.Count > 0)
                        {
                            int idx = targetIndex % targetNames.Count;
                            string name = targetNames[idx];
                            if (_state.Pings.TryGetValue(name, out var data))
                            {
                                targetData = (name, data.Host);
                            }
                        }
                    }

                    if (targetData.HasValue)
                    {
                        await _pingService.PingSingleHostAsync(_state, targetData.Value.name, targetData.Value.host);
                        targetIndex++;
                    }

                    _pollCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Monitor Loop Exception: {ex.Message}");
                }

                int interval = Math.Max(200, _configService.Current.UpdateIntervalDataMs);
                await Task.Delay(interval, token);
            }
        }

        private void PollResources(PerformanceCounter cpuCounter)
        {
            // 1. CPU
            float cpu = 0;
            try
            {
                cpu = cpuCounter.NextValue();
            }
            catch { }

            // 2. RAM
            double ramPercent = 0;
            double ramUsedGb = 0;
            double ramTotalGb = 0;
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                ramTotalGb = (double)memStatus.ullTotalPhys / (1024 * 1024 * 1024);
                ulong usedBytes = memStatus.ullTotalPhys - memStatus.ullAvailPhys;
                ramUsedGb = (double)usedBytes / (1024 * 1024 * 1024);
                ramPercent = memStatus.dwMemoryLoad;
            }

            // 3. Process Count
            int procCount = Process.GetProcesses().Length;

            // 4. Uptime
            long tickMs = Environment.TickCount64;
            var uptimeSpan = TimeSpan.FromMilliseconds(tickMs);
            string uptimeStr = $"System Up Time: {uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m";

            // 5. Battery
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
            bool hasBat = powerStatus.BatteryChargeStatus != System.Windows.Forms.BatteryChargeStatus.NoSystemBattery;
            double batPercent = powerStatus.BatteryLifePercent * 100;
            bool batPlugged = powerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;

            // 6. OS Drive Capacity (fast check every 10s)
            double diskPercent = _state.DiskPercent;
            double diskUsedGb = _state.DiskUsedGb;
            double diskTotalGb = _state.DiskTotalGb;
            double totalStorageGb = _state.TotalStorageGb;

            if (_pollCount % 10 == 0)
            {
                try
                {
                    string sysDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                    var driveInfo = new DriveInfo(sysDrive);
                    if (driveInfo.IsReady)
                    {
                        diskTotalGb = (double)driveInfo.TotalSize / (1024 * 1024 * 1024);
                        long freeBytes = driveInfo.AvailableFreeSpace;
                        long usedBytes = driveInfo.TotalSize - freeBytes;
                        diskUsedGb = (double)usedBytes / (1024 * 1024 * 1024);
                        diskPercent = (diskUsedGb / diskTotalGb) * 100;
                    }
                }
                catch { }
            }

            // 7. Aggregate Storage (slow check every 60s)
            if (_pollCount % 60 == 0)
            {
                try
                {
                    double sumTotal = 0;
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                        {
                            sumTotal += drive.TotalSize;
                        }
                    }
                    totalStorageGb = sumTotal / (1024 * 1024 * 1024);
                }
                catch { }
            }

            // 8. Disk IO Activity
            bool isRead = false;
            bool isWrite = false;
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT ReadsPerSec, WritesPerSec FROM Win32_PerfRawData_PerfDisk_PhysicalDisk WHERE Name='_Total'");
                foreach (var disk in searcher.Get())
                {
                    long reads = Convert.ToInt64(disk["ReadsPerSec"]);
                    long writes = Convert.ToInt64(disk["WritesPerSec"]);
                    isRead = reads > _lastDiskReadCount;
                    isWrite = writes > _lastDiskWriteCount;
                    _lastDiskReadCount = reads;
                    _lastDiskWriteCount = writes;
                }
            }
            catch { }

            // 9. Top CPU Processes
            List<string> topProcs = _state.TopProcs;
            if (_pollCount % 3 == 0)
            {
                try
                {
                    var procs = Process.GetProcesses()
                        .Where(p => p.ProcessName != "Idle" && p.ProcessName != "System")
                        .Take(15)
                        .Select(p =>
                        {
                            try
                            {
                                return new { Name = p.ProcessName, Cpu = p.WorkingSet64 };
                            }
                            catch { return null; }
                        })
                        .Where(x => x != null)
                        .OrderByDescending(x => x!.Cpu)
                        .Take(3)
                        .Select(x => $"{x!.Name.Substring(0, Math.Min(12, x.Name.Length))} (Active)")
                        .ToList();

                    if (procs.Count > 0) topProcs = procs;
                }
                catch { }
            }

            // 10. Network Speed
            double sentSpeed = 0;
            double recvSpeed = 0;
            try
            {
                long totalSent = 0;
                long totalRecv = 0;
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up)
                    {
                        var stats = nic.GetIPv4Statistics();
                        totalSent += stats.BytesSent;
                        totalRecv += stats.BytesReceived;
                    }
                }

                var now = DateTime.Now;
                double seconds = (now - _lastNetTime).TotalSeconds;
                if (seconds > 0 && _lastNetBytesSent > 0)
                {
                    sentSpeed = (double)(totalSent - _lastNetBytesSent) / seconds / (1024 * 1024);
                    recvSpeed = (double)(totalRecv - _lastNetBytesRecv) / seconds / (1024 * 1024);
                }

                _lastNetBytesSent = totalSent;
                _lastNetBytesRecv = totalRecv;
                _lastNetTime = now;
            }
            catch { }

            lock (_state.Lock)
            {
                _state.CpuPercent = Math.Round(cpu, 1);
                _state.RamPercent = Math.Round(ramPercent, 1);
                _state.RamUsedGb = Math.Round(ramUsedGb, 1);
                _state.RamTotalGb = Math.Round(ramTotalGb, 1);
                _state.DiskPercent = Math.Round(diskPercent, 1);
                _state.DiskUsedGb = Math.Round(diskUsedGb, 1);
                _state.DiskTotalGb = Math.Round(diskTotalGb, 1);
                _state.TotalStorageGb = Math.Round(totalStorageGb, 1);
                _state.NetSentMb = Math.Round(sentSpeed, 1);
                _state.NetRecvMb = Math.Round(recvSpeed, 1);
                _state.DiskRead = isRead;
                _state.DiskWrite = isWrite;
                _state.TopProcs = topProcs;
                _state.Uptime = uptimeStr;
                _state.BatteryPercent = batPercent;
                _state.BatteryPlugged = batPlugged;
                _state.HasBattery = hasBat;
                _state.ProcCount = procCount;

                if (cpu > 95) _state.AddAlert($"High CPU Load: {cpu:F1}%", "crit");
                if (ramPercent > 95) _state.AddAlert($"Memory Critical: {ramPercent:F1}%", "crit");
            }
        }
    }
}
