# WINGMAN v1.6 🚀

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D6?style=for-the-badge&logo=windows)
![Theme](https://img.shields.io/badge/Theme-Light%20Mode-0284C7?style=for-the-badge)
![High DPI](https://img.shields.io/badge/DPI-PerMonitorV2-10B981?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Production%20Ready-059669?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-F59E0B?style=for-the-badge)

**Wingman** is a high-performance, high-DPI Windows desktop system monitoring dashboard built with **WPF .NET 8.0** and a **Light Mode default theme**. Re-engineered from the Python OVERWATCH engine into C# MVVM architecture, Wingman provides real-time resource tracking, network diagnostics, quick app launching, multi-drive monitoring, process termination, security compliance checks, and an embedded administrative terminal.

---

## 🌟 Key Features

### ⚡ 1. Reactor Core (System Health)
- **Circular Arc Gauges**: Real-time WPF custom rendered circular gauges for CPU Load % and Memory Load %.
- **OS Drive Capacity Bar**: Linear progress bar showing OS drive usage and free space.
- **Network Speeds**: Upload (`UP: X.X MB/s`) and Download (`DN: X.X MB/s`) throughput.
- **Disk I/O Activity**: Real-time `READ` (Green) and `WRITE` (Amber) physical disk badges.
- **Power Source & Battery**: AC vs Battery status, charging percentage, and power source telemetry.

### 📡 2. Watchtower (Host Ping Monitor)
- **Multi-Port TCP & ICMP Pinger**: Async background host latency monitor (ICMP + TCP ports 53, 80, 443, 445).
- **Sparkline Graphs**: 20-sample polyline sparklines rendered per host with OK/Warn/Crit status color coding.

### 🚀 3. Launchpad (Quick Application Launcher)
- **Categorized Shortcuts**: Utilities, Apps, and Scripts launcher backed by `dashboard_config.json`.
- **Admin Elevation**: Context menu support for right-click *"Run as Administrator"*.
- **Smart Directory Detection**: Automatically sets Working Directory to application installation folder.
- **Single-Instance Protection**: Prevents launching duplicate process instances.

### 💾 4. Multi-Drive Health Monitor
- **Auto Drive Detection**: Enumerates all connected fixed, removable, and external USB drives (C:, D:, E:, etc.).
- **Live Progress Bars**: Real-time capacity bar per drive with volume label, percent used, and GB readouts.

### ☠️ 5. Interactive Process Manager
- **RAM Top Load Ranking**: Real-time list of top memory-consuming processes displaying PID, Name, and RAM MB.
- **One-Click Termination**: Red `[KILL]` action button with confirmation dialog to terminate unresponsive processes.

### 🛡️ 6. Security & GPU Intel
- **Security Compliance**: Real-time status badges for Windows Defender (`ACTIVE`), Firewall (`ENABLED`), and Windows Updates (`CURRENT`).
- **GPU & Display Intel**: Queries primary graphics hardware (`Win32_VideoController`), VRAM Total, and Screen Resolution.

### 🛠️ 7. Power Utilities & Embedded Terminal
- **Quick Admin Action Buttons**: One-click **Flush DNS**, **Restart Explorer**, **Empty Recycle Bin** (via native P/Invoke `SHEmptyRecycleBin`), and **Lock PC** (`LockWorkStation`).
- **Embedded Command Terminal**: Built-in CLI command drawer with a light slate-grey console box (`#F8FAFC`) executing background PowerShell/CMD commands asynchronously.

### 📝 8. Quick Notes & Event Logging
- **Auto-Saving Scratchpad**: Persistent notes editor writing directly to `notes.txt`.
- **Daily Event Logger**: Thread-safe event logging writing to `Logs/dashboard_log_DDMMYYYY.txt` with an interactive event log viewer window (`[ VIEW SYSTEM EVENT LOGS ]`).

---

## 🖥️ Screen Crispness & High-DPI Support

Wingman is fully optimized for High-DPI and scaled displays (125%, 150%, 200%):
- **PerMonitorV2 DPI Awareness**: Configured in `Wingman.csproj` via `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>`.
- **Subpixel Render Quality**: Features `UseLayoutRounding="True"`, `SnapsToDevicePixels="True"`, `TextOptions.TextFormattingMode="Display"`, and `TextOptions.TextRenderingMode="ClearType"`.

---

## 🛠️ Project Architecture

```
Wingman/
├── App.xaml / App.xaml.cs          # Global application entry & unhandled exception handlers
├── Wingman.csproj                  # .NET 8.0 WPF project definition
├── dashboard_config.json           # User configuration (targets, launchers, intervals)
├── notes.txt                       # Auto-saved scratchpad notes
├── Controls/
│   ├── CircularGauge.cs            # Custom rendered WPF circular arc meter control
│   ├── LinearGauge.cs              # Custom rendered drive capacity bar control
│   └── SparklineChart.cs           # Custom polyline host latency sparkline control
├── Models/
│   ├── ConfigModel.cs              # JSON deserialization data contracts
│   └── SystemState.cs              # Thread-safe telemetry state container
├── Services/
│   ├── ConfigService.cs            # Manages dashboard_config.json reading/writing
│   ├── LoggingService.cs           # Thread-safe daily log file writer
│   ├── NetworkIntelService.cs      # Local IP, WAN IP, MAC, Gateway, WiFi queries
│   ├── PingService.cs              # Multi-port TCP & ICMP host pinger
│   ├── PowerManagementService.cs   # Windows P/Invoke SetThreadExecutionState keep-awake
│   └── SystemMonitorService.cs     # Non-blocking CPU, RAM, Disk, GPU, Process polling engine
├── Themes/
│   ├── LightTheme.xaml             # Default Light Mode color palette & WPF control styles
│   └── DarkTheme.xaml              # Placeholder Dark Mode resource dictionary
├── ViewModels/
│   ├── MainViewModel.cs            # Primary MVVM viewmodel binding all dashboard modules
│   ├── ConfigViewModel.cs          # Tabbed configuration modal viewmodel
│   └── LogViewerViewModel.cs       # Log history viewer modal viewmodel
└── Views/
    ├── MainWindow.xaml             # Full-screen 4-column asymmetrical dashboard layout
    ├── ConfigWindow.xaml           # Tabbed settings dialog
    └── LogViewerWindow.xaml        # Interactive log viewer dialog
```

---

## 🚀 Building & Running

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for Windows.

### Compilation
```bash
# Navigate to project root
cd d:\Reports\Scripting\Antigravity\Wingman

# Build the project
dotnet build Wingman.csproj
```

### Launching
```bash
# Run via dotnet CLI
dotnet run --project Wingman.csproj

# Or launch compiled binary directly
.\bin\Debug\net8.0-windows\Wingman.exe
```

---

## 📄 License
Distributed under the **MIT License**.
