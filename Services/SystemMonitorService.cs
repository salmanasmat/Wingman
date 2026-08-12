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
using System.Windows;
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

        private int _pollCount = 0;
        private bool _fetchingNetInfo = false;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        public static void TrimWorkingSet()
        {
            try
            {
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, new IntPtr(-1), new IntPtr(-1));
            }
            catch { }
        }

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

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);

        public SystemMonitorService(SystemState state, ConfigService configService)
        {
            _state = state;
            _configService = configService;
            _netIntelService = new NetworkIntelService();
            _pingService = new PingService();

            // Run static specs initialization asynchronously in background
            Task.Run(() => InitStaticSpecs());
        }

        private void InitStaticSpecs()
        {
            try
            {
                string osName = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            osName = obj["Caption"]?.ToString() ?? osName;
                            break;
                        }
                    }
                }
                catch { }

                double totalStorage = 0;
                try
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                        {
                            totalStorage += (double)drive.TotalSize / (1024 * 1024 * 1024);
                        }
                    }
                }
                catch { }

                string gpuName = "Integrated Graphics";
                double vramMb = 0;
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController"))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            gpuName = obj["Name"]?.ToString() ?? gpuName;
                            if (obj["AdapterRAM"] != null && double.TryParse(obj["AdapterRAM"].ToString(), out var bytes))
                            {
                                vramMb = bytes / (1024 * 1024);
                            }
                            break;
                        }
                    }
                }
                catch { }

                string primaryRes = "1920x1080";
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        primaryRes = $"{SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}";
                    });
                }
                catch { }

                lock (_state.Lock)
                {
                    _state.SysOs = osName;
                    _state.SysUser = Environment.UserName;
                    _state.SysCores = $"{Environment.ProcessorCount} Threads";
                    _state.TotalStorageGb = totalStorage;
                    _state.GpuName = gpuName;
                    _state.VramTotalMb = vramMb;
                    _state.DisplayRes = primaryRes;
                }
            }
            catch (Exception ex)
            {
                LoggingService.WriteLog($"Static Specs Init Error: {ex.Message}", "ERROR");
            }
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoopAsync(_cts.Token));
            LoggingService.WriteLog("System Monitor Polling Engine Started.", "SYSTEM");
        }

        public void Stop()
        {
            _cts?.Cancel();
            try
            {
                _monitorTask?.Wait(1000);
            }
            catch { }
            LoggingService.WriteLog("System Monitor Polling Engine Stopped.", "SYSTEM");
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            int targetIndex = 0;
            _pingService.SyncTargets(_state, _configService.Current.Targets);

            PerformanceCounter? cpuCounter = null;
            PerformanceCounter? diskTimeCounter = null;
            PerformanceCounter? diskReadCounter = null;
            PerformanceCounter? diskWriteCounter = null;

            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue(); // First dummy read
            }
            catch { }

            try
            {
                diskTimeCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                diskTimeCounter.NextValue();
            }
            catch { }

            try
            {
                diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Reads/sec", "_Total");
                diskReadCounter.NextValue();
                diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Writes/sec", "_Total");
                diskWriteCounter.NextValue();
            }
            catch { }

            // Allow initial sampling interval before entering poll loop
            await Task.Delay(250, token);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    PollResources(cpuCounter, diskTimeCounter, diskReadCounter, diskWriteCounter);

                    if (_pollCount % 15 == 0 && !_fetchingNetInfo)
                    {
                        _fetchingNetInfo = true;
                        _ = Task.Run(async () =>
                        {
                            await _netIntelService.FetchNetworkDetailsAsync(_state);
                            _fetchingNetInfo = false;
                        }, token);
                    }

                    // Perform working set trim every 30 seconds
                    if (_pollCount % 60 == 0)
                    {
                        TrimWorkingSet();
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
                    LoggingService.WriteLog($"Monitor Loop Exception: {ex.Message}", "ERROR");
                }

                int interval = Math.Max(250, _configService.Current.UpdateIntervalDataMs);
                await Task.Delay(interval, token);
            }

            cpuCounter?.Dispose();
            diskTimeCounter?.Dispose();
            diskReadCounter?.Dispose();
            diskWriteCounter?.Dispose();
        }

        private void PollResources(PerformanceCounter? cpuCounter, PerformanceCounter? diskTimeCounter, PerformanceCounter? diskReadCounter, PerformanceCounter? diskWriteCounter)
        {
            // 1. CPU
            float cpu = 0;
            if (cpuCounter != null)
            {
                try { cpu = cpuCounter.NextValue(); } catch { }
            }

            // 2. Real-Time Physical Disk Load % (Active Time)
            float diskLoad = 0;
            if (diskTimeCounter != null)
            {
                try { diskLoad = Math.Min(100.0f, diskTimeCounter.NextValue()); } catch { }
            }

            // 3. RAM
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

            // 4. Process Count
            int procCount = _state.ProcCount;
            if (_pollCount % 10 == 0)
            {
                try
                {
                    var procs = Process.GetProcesses();
                    procCount = procs.Length;
                    foreach (var p in procs) p.Dispose();
                }
                catch { }
            }

            // 5. Uptime
            long tickMs = Environment.TickCount64;
            var uptimeSpan = TimeSpan.FromMilliseconds(tickMs);
            string uptimeStr = $"System Up Time: {uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m";

            // 6. Battery
            bool hasBat = false;
            double batPercent = 100;
            bool batPlugged = true;
            if (GetSystemPowerStatus(out var sps))
            {
                hasBat = sps.BatteryFlag != 128 && sps.ACLineStatus != 255;
                batPercent = sps.BatteryLifePercent != 255 ? sps.BatteryLifePercent : 100;
                batPlugged = sps.ACLineStatus == 1;
            }

            // 7. OS Drive & Multi-Drive Monitor (Every 5s)
            double diskCapacityPercent = _state.DiskCapacityPercent;
            double diskUsedGb = _state.DiskUsedGb;
            double diskTotalGb = _state.DiskTotalGb;
            double totalStorageGb = _state.TotalStorageGb;
            List<DriveInfoModel> drivesList = _state.Drives;

            if (_pollCount % 10 == 0 || drivesList.Count == 0)
            {
                try
                {
                    var updatedDrives = new List<DriveInfoModel>();
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                        {
                            double total = (double)drive.TotalSize / (1024 * 1024 * 1024);
                            double free = (double)drive.AvailableFreeSpace / (1024 * 1024 * 1024);
                            double used = total - free;
                            double pct = (used / total) * 100;

                            string sysDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                            if (drive.Name.Equals(sysDrive, StringComparison.OrdinalIgnoreCase))
                            {
                                diskTotalGb = total;
                                diskUsedGb = used;
                                diskCapacityPercent = pct;
                            }

                            updatedDrives.Add(new DriveInfoModel
                            {
                                Name = drive.Name.TrimEnd('\\'),
                                VolumeLabel = string.IsNullOrEmpty(drive.VolumeLabel) ? drive.DriveType.ToString() : drive.VolumeLabel,
                                Percent = Math.Round(pct, 1),
                                UsedGb = Math.Round(used, 1),
                                TotalGb = Math.Round(total, 1),
                                DriveType = drive.DriveType.ToString()
                            });
                        }
                    }
                    drivesList = updatedDrives;
                }
                catch { }
            }

            // 8. Disk IO Activity
            bool isRead = false;
            bool isWrite = false;
            try
            {
                if (diskReadCounter != null)
                {
                    float reads = diskReadCounter.NextValue();
                    isRead = reads > 0.5f;
                }
                if (diskWriteCounter != null)
                {
                    float writes = diskWriteCounter.NextValue();
                    isWrite = writes > 0.5f;
                }
            }
            catch { }

            // 9. Top 10 CPU & RAM Processes Detailed (Every 3s, disposing process handles)
            List<string> topProcs = _state.TopProcs;
            List<ProcessInfoModel> topProcDetails = _state.TopProcDetails;

            if (_pollCount % 12 == 0)
            {
                Process[]? allProcs = null;
                try
                {
                    allProcs = Process.GetProcesses();
                    var list = new List<ProcessInfoModel>();

                    foreach (var p in allProcs)
                    {
                        try
                        {
                            if (p.ProcessName != "Idle" && p.ProcessName != "System")
                            {
                                list.Add(new ProcessInfoModel
                                {
                                    Pid = p.Id,
                                    Name = p.ProcessName,
                                    RamMb = Math.Round((double)p.WorkingSet64 / (1024 * 1024), 1)
                                });
                            }
                        }
                        catch { }
                        finally
                        {
                            p.Dispose(); // Free native handle!
                        }
                    }

                    var top10 = list.OrderByDescending(x => x.RamMb).Take(10).ToList();
                    if (top10.Count > 0)
                    {
                        topProcDetails = top10;
                        topProcs = top10.Select(x => $"{x.Name} ({x.RamMb:F0}MB)").ToList();
                    }
                }
                catch { }
            }

            // 10. Active Network Connections & Open Ports (Every 10s)
            List<NetConnInfoModel> netConns = _state.ActiveConnections;
            if (_pollCount % 20 == 0)
            {
                try
                {
                    var properties = IPGlobalProperties.GetIPGlobalProperties();
                    var connections = properties.GetActiveTcpConnections()
                        .Take(10)
                        .Select(c => new NetConnInfoModel
                        {
                            Protocol = "TCP",
                            LocalPort = c.LocalEndPoint.Port,
                            RemoteIp = c.RemoteEndPoint.Address.ToString(),
                            State = c.State.ToString()
                        })
                        .ToList();

                    netConns = connections;
                }
                catch { }
            }

            // 11. Network Speed
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

                _state.DiskPercent = Math.Round(diskLoad, 1);                     // Real-Time Physical Disk Active Load %
                _state.DiskCapacityPercent = Math.Round(diskCapacityPercent, 1); // OS Storage Capacity Used %

                _state.DiskUsedGb = Math.Round(diskUsedGb, 1);
                _state.DiskTotalGb = Math.Round(diskTotalGb, 1);
                _state.TotalStorageGb = Math.Round(totalStorageGb, 1);
                _state.NetSentMb = Math.Round(sentSpeed, 1);
                _state.NetRecvMb = Math.Round(recvSpeed, 1);
                _state.DiskRead = isRead;
                _state.DiskWrite = isWrite;
                _state.TopProcs = topProcs;
                _state.TopProcDetails = topProcDetails;
                _state.Drives = drivesList;
                _state.ActiveConnections = netConns;
                _state.Uptime = uptimeStr;
                _state.BatteryPercent = batPercent;
                _state.BatteryPlugged = batPlugged;
                _state.HasBattery = hasBat;
                _state.ProcCount = procCount;

                // Dynamically evaluate system status alerts
                _state.Alerts.Clear();
                if (cpu > 90) _state.AddAlert($"High CPU Load ({cpu:F0}%)", "CRIT");
                if (ramPercent > 90) _state.AddAlert($"High Memory Usage ({ramPercent:F0}%)", "CRIT");
                if (diskLoad > 90) _state.AddAlert($"High Disk Activity ({diskLoad:F0}%)", "WARN");
            }
        }
    }
}
