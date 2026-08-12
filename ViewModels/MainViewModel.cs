using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wingman.Models;
using Wingman.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Wingman.ViewModels
{
    public class DriveItemViewModel : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string VolumeLabel { get; set; } = string.Empty;
        public double Percent { get; set; }
        public string Label { get; set; } = string.Empty;
        public string DriveType { get; set; } = "Fixed";
        public ICommand OpenDriveCommand { get; set; } = null!;
    }

    public class ProcessItemViewModel : ObservableObject
    {
        public int Pid { get; set; }
        public string Name { get; set; } = string.Empty;
        public double RamMb { get; set; }
        public ICommand KillCommand { get; set; } = null!;
    }

    public class NetConnItemViewModel : ObservableObject
    {
        public string Protocol { get; set; } = "TCP";
        public int LocalPort { get; set; }
        public string RemoteIp { get; set; } = string.Empty;
        public string State { get; set; } = "ESTABLISHED";
    }

    public class WatchtowerItemViewModel : ObservableObject
    {
        private string _name = string.Empty;
        private string _host = string.Empty;
        private int _lastMs;
        private string _status = "init";
        private List<double> _history = new List<double>();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Host
        {
            get => _host;
            set => SetProperty(ref _host, value);
        }

        public int LastMs
        {
            get => _lastMs;
            set => SetProperty(ref _lastMs, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public List<double> History
        {
            get => _history;
            set => SetProperty(ref _history, value);
        }
    }

    public class LaunchpadCategoryViewModel
    {
        public string Category { get; set; } = string.Empty;
        public ObservableCollection<LaunchpadItemViewModel> Items { get; set; } = new ObservableCollection<LaunchpadItemViewModel>();
    }

    public class LaunchpadItemViewModel
    {
        public string Label { get; set; } = string.Empty;
        public string Cmd { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ICommand LaunchCommand { get; set; } = null!;
        public ICommand LaunchAdminCommand { get; set; } = null!;
    }

    public class MainViewModel : ObservableObject
    {
        private readonly SystemState _state;
        private readonly ConfigService _configService;
        private readonly SystemMonitorService _monitorService;
        private readonly DispatcherTimer _uiTimer;
        private readonly Dictionary<string, Process> _runningProcesses = new Dictionary<string, Process>();
        private readonly Dictionary<string, DateTime> _launchCooldowns = new Dictionary<string, DateTime>();

        private string _clockText = string.Empty;
        private string _hostnameText = string.Empty;
        private string _uptimeText = string.Empty;
        private string _statusText = "SYSTEM ONLINE";
        private Brush _statusBrush = Brushes.Gray;

        private double _cpuPercent;
        private double _ramPercent;
        private double _diskPercent;
        private double _diskUsedGb;
        private double _diskTotalGb;
        private string _diskLabel = "C: 0GB / 0GB";
        private string _netUpText = "UP: 0.0 MB/s";
        private string _netDownText = "DN: 0.0 MB/s";
        private bool _diskReadActive;
        private bool _diskWriteActive;
        private string _batteryText = "POWER: AC";
        private Brush _batteryBrush = Brushes.Gray;

        private string _localIp = "...";
        private string _publicIp = "...";
        private string _mac = "...";
        private string _gateway = "...";
        private string _wifiSsid = "N/A";
        private string _wifiSignal = "0%";
        private string _wifiRadio = "...";
        private string _wifiAuth = "...";

        private string _sysOs = "...";
        private string _sysUser = "...";
        private string _sysCores = "...";
        private string _sysRamTotal = "...";
        private string _sysDiskCap = "...";
        private string _sysProcsTotal = "...";

        private string _gpuName = "Integrated Graphics";
        private string _vramText = "N/A";
        private string _displayRes = "1920x1080";

        private string _notesText = string.Empty;
        private string _terminalInput = string.Empty;
        private string _terminalOutput = "Wingman Power Terminal v1.6 ready.\nType a command (e.g. ping 8.8.8.8) and press Run.\n";

        public ObservableCollection<WatchtowerItemViewModel> WatchtowerItems { get; } = new ObservableCollection<WatchtowerItemViewModel>();
        public ObservableCollection<LaunchpadCategoryViewModel> LaunchpadCategories { get; } = new ObservableCollection<LaunchpadCategoryViewModel>();
        public ObservableCollection<string> TopProcesses { get; } = new ObservableCollection<string>();
        public ObservableCollection<DriveItemViewModel> DrivesList { get; } = new ObservableCollection<DriveItemViewModel>();
        public ObservableCollection<ProcessItemViewModel> DetailedProcesses { get; } = new ObservableCollection<ProcessItemViewModel>();
        public ObservableCollection<NetConnItemViewModel> NetworkConnections { get; } = new ObservableCollection<NetConnItemViewModel>();

        public ICommand OpenConfigCommand { get; }
        public ICommand ShowLogsCommand { get; }
        public ICommand LaunchThisPcCommand { get; }
        public ICommand LaunchDiskCleanupCommand { get; }
        public ICommand EmptyRecycleBinCommand { get; }
        public ICommand LockWorkstationCommand { get; }
        public ICommand RunTerminalCommand { get; }

        public string ClockText { get => _clockText; set => SetProperty(ref _clockText, value); }
        public string HostnameText { get => _hostnameText; set => SetProperty(ref _hostnameText, value); }
        public string UptimeText { get => _uptimeText; set => SetProperty(ref _uptimeText, value); }
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        public Brush StatusBrush { get => _statusBrush; set => SetProperty(ref _statusBrush, value); }

        public double CpuPercent { get => _cpuPercent; set => SetProperty(ref _cpuPercent, value); }
        public double RamPercent { get => _ramPercent; set => SetProperty(ref _ramPercent, value); }
        public double DiskPercent { get => _diskPercent; set => SetProperty(ref _diskPercent, value); }
        public double DiskUsedGb { get => _diskUsedGb; set => SetProperty(ref _diskUsedGb, value); }
        public double DiskTotalGb { get => _diskTotalGb; set => SetProperty(ref _diskTotalGb, value); }
        public string DiskLabel { get => _diskLabel; set => SetProperty(ref _diskLabel, value); }
        public string NetUpText { get => _netUpText; set => SetProperty(ref _netUpText, value); }
        public string NetDownText { get => _netDownText; set => SetProperty(ref _netDownText, value); }
        public bool DiskReadActive { get => _diskReadActive; set => SetProperty(ref _diskReadActive, value); }
        public bool DiskWriteActive { get => _diskWriteActive; set => SetProperty(ref _diskWriteActive, value); }
        public string BatteryText { get => _batteryText; set => SetProperty(ref _batteryText, value); }
        public Brush BatteryBrush { get => _batteryBrush; set => SetProperty(ref _batteryBrush, value); }

        public string LocalIp { get => _localIp; set => SetProperty(ref _localIp, value); }
        public string PublicIp { get => _publicIp; set => SetProperty(ref _publicIp, value); }
        public string Mac { get => _mac; set => SetProperty(ref _mac, value); }
        public string Gateway { get => _gateway; set => SetProperty(ref _gateway, value); }
        public string WifiSsid { get => _wifiSsid; set => SetProperty(ref _wifiSsid, value); }
        public string WifiSignal { get => _wifiSignal; set => SetProperty(ref _wifiSignal, value); }
        public string WifiRadio { get => _wifiRadio; set => SetProperty(ref _wifiRadio, value); }
        public string WifiAuth { get => _wifiAuth; set => SetProperty(ref _wifiAuth, value); }

        public string SysOs { get => _sysOs; set => SetProperty(ref _sysOs, value); }
        public string SysUser { get => _sysUser; set => SetProperty(ref _sysUser, value); }
        public string SysCores { get => _sysCores; set => SetProperty(ref _sysCores, value); }
        public string SysRamTotal { get => _sysRamTotal; set => SetProperty(ref _sysRamTotal, value); }
        public string SysDiskCap { get => _sysDiskCap; set => SetProperty(ref _sysDiskCap, value); }
        public string SysProcsTotal { get => _sysProcsTotal; set => SetProperty(ref _sysProcsTotal, value); }

        public string GpuName { get => _gpuName; set => SetProperty(ref _gpuName, value); }
        public string VramText { get => _vramText; set => SetProperty(ref _vramText, value); }
        public string DisplayRes { get => _displayRes; set => SetProperty(ref _displayRes, value); }

        public string TerminalInput { get => _terminalInput; set => SetProperty(ref _terminalInput, value); }
        public string TerminalOutput { get => _terminalOutput; set => SetProperty(ref _terminalOutput, value); }

        public string NotesText
        {
            get => _notesText;
            set
            {
                if (SetProperty(ref _notesText, value))
                {
                    SaveNotes();
                }
            }
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        public MainViewModel(ConfigService configService, Action openConfigDialogAction, Action showLogsDialogAction)
        {
            _configService = configService;
            _state = new SystemState();
            _monitorService = new SystemMonitorService(_state, _configService);

            HostnameText = $"HOST: {Environment.MachineName.ToUpper()}";

            OpenConfigCommand = new RelayCommand(openConfigDialogAction);
            ShowLogsCommand = new RelayCommand(showLogsDialogAction);

            LaunchThisPcCommand = new RelayCommand(ExecuteLaunchThisPc);
            LaunchDiskCleanupCommand = new RelayCommand(ExecuteLaunchDiskCleanup);
            EmptyRecycleBinCommand = new RelayCommand(ExecuteEmptyRecycleBin);
            LockWorkstationCommand = new RelayCommand(ExecuteLockWorkstation);
            RunTerminalCommand = new RelayCommand(ExecuteTerminalCommand);

            LoadNotes();
            RefreshLaunchpad();
            RefreshWatchtower();

            _configService.ConfigChanged += (s, e) =>
            {
                RefreshWatchtower();
                RefreshLaunchpad();
            };

            _monitorService.Start();

            int uiInterval = Math.Max(250, _configService.Current.UpdateIntervalUiMs);
            _uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(uiInterval)
            };
            _uiTimer.Tick += OnUiTimerTick;
            _uiTimer.Start();

            LoggingService.WriteLog("=== WINGMAN DASHBOARD STARTED ===", "SYSTEM");
        }

        private void OnUiTimerTick(object? sender, EventArgs e)
        {
            ClockText = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");

            lock (_state.Lock)
            {
                CpuPercent = _state.CpuPercent;
                RamPercent = _state.RamPercent;
                DiskPercent = _state.DiskPercent;
                DiskUsedGb = _state.DiskUsedGb;
                DiskTotalGb = _state.DiskTotalGb;

                string sysDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:";
                if (sysDrive.EndsWith("\\")) sysDrive = sysDrive.TrimEnd('\\');
                DiskLabel = $"{sysDrive} {(int)DiskUsedGb}GB / {(int)DiskTotalGb}GB";

                NetUpText = $"UP: {_state.NetSentMb:F1} MB/s";
                NetDownText = $"DN: {_state.NetRecvMb:F1} MB/s";

                DiskReadActive = _state.DiskRead;
                DiskWriteActive = _state.DiskWrite;

                UptimeText = _state.Uptime;

                if (_state.HasBattery)
                {
                    string pwrSrc = _state.BatteryPlugged ? "AC" : "Battery";
                    BatteryText = $"POWER: {pwrSrc} ({(int)_state.BatteryPercent}%)";
                    BatteryBrush = _state.BatteryPlugged
                        ? (Brush)Application.Current.FindResource("AccentCyanBrush")
                        : (_state.BatteryPercent < 20 ? Brushes.Red : Brushes.Orange);
                }
                else
                {
                    BatteryText = "POWER: AC (NO Battery)";
                    BatteryBrush = Brushes.Gray;
                }

                if (!TopProcesses.SequenceEqual(_state.TopProcs))
                {
                    TopProcesses.Clear();
                    foreach (var p in _state.TopProcs)
                    {
                        TopProcesses.Add(p);
                    }
                }

                // Update Multi-Drive List with OpenDriveCommand
                DrivesList.Clear();
                foreach (var drive in _state.Drives)
                {
                    string driveName = drive.Name;
                    var driveVm = new DriveItemViewModel
                    {
                        Name = drive.Name,
                        VolumeLabel = drive.VolumeLabel,
                        Percent = drive.Percent,
                        Label = $"{drive.Name} {(int)drive.UsedGb}GB / {(int)drive.TotalGb}GB",
                        DriveType = drive.DriveType,
                    };
                    driveVm.OpenDriveCommand = new RelayCommand(() => OpenDrive(driveName));
                    DrivesList.Add(driveVm);
                }

                // Update Detailed Process List with Kill Command
                DetailedProcesses.Clear();
                foreach (var proc in _state.TopProcDetails)
                {
                    var pItem = new ProcessItemViewModel
                    {
                        Pid = proc.Pid,
                        Name = proc.Name,
                        RamMb = proc.RamMb
                    };
                    pItem.KillCommand = new RelayCommand(() => KillProcess(pItem.Pid, pItem.Name));
                    DetailedProcesses.Add(pItem);
                }

                // Update Network Connections
                NetworkConnections.Clear();
                foreach (var conn in _state.ActiveConnections)
                {
                    NetworkConnections.Add(new NetConnItemViewModel
                    {
                        Protocol = conn.Protocol,
                        LocalPort = conn.LocalPort,
                        RemoteIp = conn.RemoteIp,
                        State = conn.State
                    });
                }

                LocalIp = _state.LocalIp;
                PublicIp = _state.PublicIp;
                Mac = _state.Mac;
                Gateway = _state.Gateway;
                WifiSsid = _state.WifiSsid;
                WifiSignal = _state.WifiSignal;
                WifiRadio = _state.WifiRadio;
                WifiAuth = _state.WifiAuth;

                SysOs = _state.SysOs;
                SysUser = _state.SysUser;
                SysCores = _state.SysCores;
                SysRamTotal = _state.SysRamTotal;
                SysDiskCap = $"{ (int)_state.TotalStorageGb } GB";
                SysProcsTotal = _state.ProcCount.ToString();

                GpuName = _state.GpuName;
                VramText = _state.VramTotalMb > 0 ? $"{ (int)_state.VramTotalMb } MB" : "Dynamic";
                DisplayRes = _state.DisplayRes;

                UpdateWatchtowerData();

                if (_state.Alerts.Count > 0)
                {
                    StatusText = _state.Alerts[0];
                    StatusBrush = Brushes.Red;
                }
                else
                {
                    StatusText = "SYSTEM NOMINAL";
                    StatusBrush = (Brush)Application.Current.FindResource("FgMutedBrush");
                }
            }
        }

        private void OpenDrive(string driveName)
        {
            try
            {
                string targetPath = driveName.EndsWith("\\") ? driveName : driveName + "\\";
                Process.Start("explorer.exe", targetPath);
                LoggingService.WriteLog($"Opened Drive in Explorer: {targetPath}", "DRIVE");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open drive: {ex.Message}", "Drive Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KillProcess(int pid, string name)
        {
            var res = MessageBox.Show($"Are you sure you want to terminate process '{name}' (PID: {pid})?",
                "CONFIRM PROCESS KILL", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
                LoggingService.WriteLog($"Killed Process: {name} (PID: {pid})", "WARN");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to kill process: {ex.Message}", "Kill Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteLaunchThisPc()
        {
            try
            {
                Process.Start("explorer.exe", "shell:MyComputerFolder");
                LoggingService.WriteLog("Launched Windows This PC Folder", "UTIL");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"This PC launch error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteLaunchDiskCleanup()
        {
            try
            {
                Process.Start("cleanmgr.exe");
                LoggingService.WriteLog("Launched Windows Disk Cleanup", "UTIL");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Disk Cleanup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEmptyRecycleBin()
        {
            try
            {
                SHEmptyRecycleBin(IntPtr.Zero, null, 7); // SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND
                MessageBox.Show("Recycle Bin emptied.", "Empty Recycle Bin", MessageBoxButton.OK, MessageBoxImage.Information);
                LoggingService.WriteLog("Emptied Recycle Bin", "UTIL");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Empty Recycle Bin error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteLockWorkstation()
        {
            try
            {
                LockWorkStation();
                LoggingService.WriteLog("Locked Workstation", "UTIL");
            }
            catch { }
        }

        private void ExecuteTerminalCommand()
        {
            if (string.IsNullOrWhiteSpace(TerminalInput)) return;

            string cmd = TerminalInput.Trim();
            TerminalOutput += $"\n> {cmd}\n";
            TerminalInput = string.Empty;

            Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {cmd}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        string outStr = proc.StandardOutput.ReadToEnd();
                        string errStr = proc.StandardError.ReadToEnd();
                        proc.WaitForExit();

                        string result = !string.IsNullOrEmpty(outStr) ? outStr : errStr;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            TerminalOutput += result + "\n";
                        });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TerminalOutput += $"Error: {ex.Message}\n";
                    });
                }
            });
        }

        public void RefreshWatchtower()
        {
            WatchtowerItems.Clear();
            foreach (var t in _configService.Current.Targets)
            {
                WatchtowerItems.Add(new WatchtowerItemViewModel
                {
                    Name = t.Name,
                    Host = t.Host,
                    LastMs = 0,
                    Status = "init",
                    History = new List<double>(Enumerable.Repeat(0.0, 20))
                });
            }
        }

        private void UpdateWatchtowerData()
        {
            foreach (var item in WatchtowerItems)
            {
                if (_state.Pings.TryGetValue(item.Name, out var status))
                {
                    if (item.LastMs != status.LastMs || item.Status != status.Status)
                    {
                        item.LastMs = status.LastMs;
                        item.Status = status.Status;
                        item.History = new List<double>(status.History);
                    }
                }
            }
        }

        public void RefreshLaunchpad()
        {
            LaunchpadCategories.Clear();
            var categories = new[] { "Utilities", "Apps", "Scripts" };

            foreach (var cat in categories)
            {
                if (_configService.Current.Launcher.TryGetValue(cat, out var items) && items.Count > 0)
                {
                    var catVm = new LaunchpadCategoryViewModel { Category = cat.ToUpper() };
                    foreach (var item in items)
                    {
                        var itemVm = new LaunchpadItemViewModel
                        {
                            Label = item.Label,
                            Cmd = item.Cmd,
                            Category = cat,
                        };
                        itemVm.LaunchCommand = new RelayCommand(() => LaunchApp(itemVm.Cmd, itemVm.Category));
                        itemVm.LaunchAdminCommand = new RelayCommand(() => LaunchAppAdmin(itemVm.Cmd, itemVm.Category));
                        catVm.Items.Add(itemVm);
                    }
                    LaunchpadCategories.Add(catVm);
                }
            }
        }

        public void LaunchApp(string cmd, string category)
        {
            if (category == "Scripts")
            {
                var result = MessageBox.Show($"Are you sure you want to execute this script?\n\nCommand:\n{cmd}",
                    "CONFIRM SCRIPT LAUNCH", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            if (_launchCooldowns.TryGetValue(cmd, out var lastTime) && (DateTime.Now - lastTime).TotalSeconds < 1.0)
            {
                return;
            }
            _launchCooldowns[cmd] = DateTime.Now;

            if (_runningProcesses.TryGetValue(cmd, out var proc) && !proc.HasExited)
            {
                MessageBox.Show($"This application is already running.\n\nCommand: {cmd}", "Launchpad", MessageBoxButton.OK, MessageBoxImage.Information);
                LoggingService.WriteLog($"Blocked duplicate launch: {cmd}", "WARN");
                return;
            }

            string cwd = DetermineCwd(cmd);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {cmd}",
                    WorkingDirectory = cwd,
                    UseShellExecute = true
                };

                var newProc = Process.Start(psi);
                if (newProc != null)
                {
                    _runningProcesses[cmd] = newProc;
                }
                LoggingService.WriteLog($"Launched App: {cmd}", "LAUNCH");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Launch error: {ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoggingService.WriteLog($"Launch Failed: {cmd} | Error: {ex.Message}", "ERROR");
            }
        }

        public void LaunchAppAdmin(string cmd, string category)
        {
            if (category == "Scripts")
            {
                var result = MessageBox.Show($"Are you sure you want to RUN AS ADMINISTRATOR?\n\nCommand:\n{cmd}",
                    "CONFIRM ELEVATED LAUNCH", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            string cwd = DetermineCwd(cmd);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {cmd}",
                    WorkingDirectory = cwd,
                    Verb = "runas",
                    UseShellExecute = true
                };

                Process.Start(psi);
                LoggingService.WriteLog($"Launched App (Admin): {cmd}", "LAUNCH");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Elevated Launch Error: {ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoggingService.WriteLog($"Launch Failed (Admin): {cmd} | Error: {ex.Message}", "ERROR");
            }
        }

        private string DetermineCwd(string cmd)
        {
            string cwd = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            try
            {
                string trimmed = cmd.Trim();
                if (trimmed.StartsWith("\""))
                {
                    int endQuote = trimmed.IndexOf('"', 1);
                    if (endQuote != -1)
                    {
                        string cleanPath = trimmed.Substring(1, endQuote - 1);
                        if (File.Exists(cleanPath)) return Path.GetDirectoryName(cleanPath) ?? cwd;
                    }
                }

                if (File.Exists(trimmed)) return Path.GetDirectoryName(trimmed) ?? cwd;

                var parts = trimmed.Split(' ', 2);
                if (File.Exists(parts[0])) return Path.GetDirectoryName(parts[0]) ?? cwd;
            }
            catch { }
            return cwd;
        }

        private void LoadNotes()
        {
            if (File.Exists("notes.txt"))
            {
                try
                {
                    _notesText = File.ReadAllText("notes.txt");
                }
                catch { }
            }
        }

        private void SaveNotes()
        {
            try
            {
                File.WriteAllText("notes.txt", _notesText);
            }
            catch { }
        }

        public void StopMonitoring()
        {
            _uiTimer.Stop();
            _monitorService.Stop();
            LoggingService.WriteLog("=== WINGMAN DASHBOARD STOPPED ===", "SYSTEM");
        }
    }
}
