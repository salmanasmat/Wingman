using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Wingman.Models;

namespace Wingman.Services
{
    public class ConfigService
    {
        private static readonly string ConfigFile = Path.Combine(AppContext.BaseDirectory, "dashboard_config.json");
        public DashboardConfig Current { get; private set; } = new DashboardConfig();

        public event EventHandler? ConfigChanged;

        public ConfigService()
        {
            LoadConfig();
        }

        public DashboardConfig LoadConfig()
        {
            if (File.Exists(ConfigFile))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFile);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };

                    var loaded = JsonSerializer.Deserialize<DashboardConfig>(json, options);
                    if (loaded != null)
                    {
                        EnsureDefaults(loaded);
                        Current = loaded;
                        ApplyThemeResources(Current.Theme);
                        return Current;
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.WriteLog($"Config load failed, using defaults: {ex.Message}", "ERROR");
                }
            }

            Current = CreateDefaultConfig();
            SaveConfig();
            return Current;
        }

        public bool SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(Current, options);
                File.WriteAllText(ConfigFile, json);
                ApplyThemeResources(Current.Theme);
                ConfigChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.WriteLog($"Failed to save config: {ex.Message}", "ERROR");
                return false;
            }
        }

        public static void ApplyThemeResources(ThemeConfig? theme)
        {
            if (theme == null) return;

            try
            {
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        string themeSource = theme.IsDarkMode ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
                        var newDict = new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(themeSource, UriKind.Relative)
                        };

                        var appDicts = System.Windows.Application.Current.Resources.MergedDictionaries;
                        appDicts.Clear();
                        appDicts.Add(newDict);

                        if (!string.IsNullOrWhiteSpace(theme.AccentCyan))
                        {
                            try
                            {
                                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.AccentCyan);
                                var brush = new System.Windows.Media.SolidColorBrush(color);
                                brush.Freeze();

                                System.Windows.Application.Current.Resources["AccentCyanBrush"] = brush;
                                System.Windows.Application.Current.Resources["AccentCyanColor"] = color;
                            }
                            catch { }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.WriteLog($"Failed to apply theme resources: {ex.Message}", "ERROR");
            }
        }

        private void EnsureDefaults(DashboardConfig cfg)
        {
            cfg.Theme ??= new ThemeConfig();
            cfg.Targets ??= new List<TargetItem>();
            cfg.Launcher ??= new Dictionary<string, List<LaunchItem>>();

            if (!cfg.Launcher.ContainsKey("Utilities"))
                cfg.Launcher["Utilities"] = GetDefaultUtilities();

            if (!cfg.Launcher.ContainsKey("Apps"))
                cfg.Launcher["Apps"] = GetDefaultApps();

            if (!cfg.Launcher.ContainsKey("Scripts"))
                cfg.Launcher["Scripts"] = new List<LaunchItem>();

            if (cfg.Targets.Count == 0)
            {
                cfg.Targets.Add(new TargetItem { Name = "Internet", Host = "8.8.8.8" });
                cfg.Targets.Add(new TargetItem { Name = "Localhost", Host = "127.0.0.1" });
            }
        }

        private DashboardConfig CreateDefaultConfig()
        {
            return new DashboardConfig
            {
                UpdateIntervalDataMs = 1000,
                UpdateIntervalUiMs = 100,
                PreventSleep = true,
                Theme = new ThemeConfig(),
                Targets = new List<TargetItem>
                {
                    new TargetItem { Name = "Internet", Host = "8.8.8.8" },
                    new TargetItem { Name = "Localhost", Host = "127.0.0.1" }
                },
                Launcher = new Dictionary<string, List<LaunchItem>>
                {
                    { "Utilities", GetDefaultUtilities() },
                    { "Apps", GetDefaultApps() },
                    { "Scripts", new List<LaunchItem>() }
                }
            };
        }

        private List<LaunchItem> GetDefaultUtilities() => new List<LaunchItem>
        {
            new LaunchItem { Label = "CMD", Cmd = "cmd.exe /k" },
            new LaunchItem { Label = "PowerShell", Cmd = "powershell.exe" },
            new LaunchItem { Label = "Task Mgr", Cmd = "taskmgr" },
            new LaunchItem { Label = "Task Scheduler", Cmd = "taskschd.msc" }
        };

        private List<LaunchItem> GetDefaultApps() => new List<LaunchItem>
        {
            new LaunchItem { Label = "Notepad", Cmd = "notepad" },
            new LaunchItem { Label = "Calc", Cmd = "calc" }
        };
    }
}
