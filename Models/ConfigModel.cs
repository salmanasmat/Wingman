using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Wingman.Models
{
    public class DashboardConfig
    {
        [JsonPropertyName("update_interval_data_ms")]
        public int UpdateIntervalDataMs { get; set; } = 1000;

        [JsonPropertyName("update_interval_ui_ms")]
        public int UpdateIntervalUiMs { get; set; } = 100;

        [JsonPropertyName("prevent_sleep")]
        public bool PreventSleep { get; set; } = true;

        [JsonPropertyName("theme")]
        public ThemeConfig Theme { get; set; } = new ThemeConfig();

        [JsonPropertyName("targets")]
        public List<TargetItem> Targets { get; set; } = new List<TargetItem>();

        [JsonPropertyName("launcher")]
        public Dictionary<string, List<LaunchItem>> Launcher { get; set; } = new Dictionary<string, List<LaunchItem>>();
    }

    public class ThemeConfig
    {
        [JsonPropertyName("bg_main")]
        public string BgMain { get; set; } = "#F8FAFC";

        [JsonPropertyName("bg_card")]
        public string BgCard { get; set; } = "#FFFFFF";

        [JsonPropertyName("fg_primary")]
        public string FgPrimary { get; set; } = "#0F172A";

        [JsonPropertyName("accent_cyan")]
        public string AccentCyan { get; set; } = "#0284C7";

        [JsonPropertyName("accent_green")]
        public string AccentGreen { get; set; } = "#10B981";

        [JsonPropertyName("accent_warn")]
        public string AccentWarn { get; set; } = "#F59E0B";

        [JsonPropertyName("accent_crit")]
        public string AccentCrit { get; set; } = "#EF4444";

        [JsonPropertyName("is_dark_mode")]
        public bool IsDarkMode { get; set; } = false;

        [JsonPropertyName("font_family")]
        public string FontFamily { get; set; } = "Segoe UI";

        [JsonPropertyName("font_mono")]
        public string FontMono { get; set; } = "Consolas";
    }

    public class TargetItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("host")]
        public string Host { get; set; } = string.Empty;
    }

    public class LaunchItem
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("cmd")]
        public string Cmd { get; set; } = string.Empty;
    }
}
