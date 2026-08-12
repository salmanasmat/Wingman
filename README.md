# WINGMAN v1.0 🚀

![Version](https://img.shields.io/badge/Version-1.0-0284C7?style=for-the-badge)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D6?style=for-the-badge&logo=windows)
![Theme](https://img.shields.io/badge/Theme-Light%20Mode-0284C7?style=for-the-badge)
![High DPI](https://img.shields.io/badge/DPI-PerMonitorV2-10B981?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Production%20Ready-059669?style=for-the-badge)

**Wingman** is a high-performance, high-DPI Windows desktop system monitoring dashboard built with **WPF .NET 8.0** and a **Light Mode default theme**. Re-engineered into C# MVVM architecture, Wingman provides real-time resource tracking, host latency monitoring, quick application launching, multi-drive explorer navigation, process management, security compliance checks, and an embedded administrative terminal.

---

## 🌟 Key Features

### ⚡ 1. Reactor Core (System Health)
- **3 Circular Arc Gauges**: Enlarged real-time WPF custom rendered circular gauges (145x145) for **CPU LOAD** (Cyan), **MEMORY** (Amber), and **DISK LOAD** (Green).
- **OS Drive Capacity Bar**: Linear progress bar displaying OS drive space utilization.
- **Network Speeds**: Real-time Upload (`UP: X.X MB/s`) and Download (`DN: X.X MB/s`) throughput.
- **Disk I/O & Power**: Real-time `READ` (Green) and `WRITE` (Amber) physical disk badges alongside battery/AC power telemetry.
- **Top 10 Active Processes**: Horizontal `WrapPanel` showing the top memory-consuming processes.

### 📡 2. Watchtower (Host Latency Monitor)
- **Multi-Port Host Pinger**: Async background host latency monitor (ICMP + TCP ports 53, 80, 443, 445).
- **Sparkline Graphs**: 20-sample polyline sparklines rendered per host with OK/Warn/Crit status color coding.

### 💾 3. Multi-Drive Health & Explorer Launcher
- **Auto Drive Detection**: Enumerates all connected fixed, removable, and external USB drives (C:, D:, E:, etc.).
- **Live Progress Bars**: Real-time capacity bar per drive with volume label, percent used, and GB readouts.
- **Double-Click Explorer**: Double-clicking any drive card (or right-clicking) opens that drive directly in Windows Explorer.

### ☠️ 4. Process Manager
- **RAM Top 10 Ranking**: Real-time list of top memory-consuming processes displaying PID, Name, and RAM MB.
- **One-Click Termination**: Red `[KILL]` action button with confirmation dialog to terminate unresponsive processes.

### 🚀 5. Launchpad (Quick Application Launcher)
- **Categorized Shortcuts**: Utilities, Apps, and Scripts launcher backed by `dashboard_config.json`.
- **Admin Elevation**: Context menu support for right-click *"Run as Administrator"*.
- **Smart Directory Detection**: Automatically sets Working Directory to application installation folder.
- **Single-Instance Protection**: Prevents launching duplicate process instances.

### 🛡️ 6. Security & GPU Intel
- **Security Compliance**: Real-time status badges for Windows Defender (`ACTIVE`), Firewall (`ENABLED`), and Windows Updates (`CURRENT`).
- **GPU & Display Intel**: Queries primary graphics hardware (`Win32_VideoController`), VRAM Total, and Screen Resolution.

### 🛠️ 7. Power Utilities & Embedded Terminal
- **Quick Admin Action Buttons**: One-click **THIS PC** (`shell:MyComputerFolder`), **DISK CLEAN** (`cleanmgr.exe`), **EMPTY BIN** (`SHEmptyRecycleBin`), and **LOCK PC** (`LockWorkStation`).
- **Light Slate Terminal**: Embedded CLI command drawer with a light slate-grey console box (`#F8FAFC`) executing background PowerShell/CMD commands asynchronously.

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
├── Wingman.csproj                  # .NET 8.0 WPF project definition (Version 1.0.0)
├── dashboard_config.json           # User configuration (targets, launchers, intervals)
├── notes.txt                       # Auto-saved scratchpad notes
├── Controls/
│   ├── CircularGauge.cs            # Custom rendered WPF circular arc meter control (145x145)
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
Internal / Proprietary Utility - **v1.0**
