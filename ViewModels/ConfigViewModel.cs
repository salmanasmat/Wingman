using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Wingman.Models;
using Wingman.Services;

namespace Wingman.ViewModels
{
    public class ConfigViewModel : ObservableObject
    {
        private readonly ConfigService _configService;
        private readonly Action _closeWindowAction;

        private TargetItem? _selectedTarget;
        private string _targetName = string.Empty;
        private string _targetHost = string.Empty;
        private int? _editingTargetIndex = null;
        private string _targetAddButtonText = "ADD TARGET";

        private string _selectedCategory = "Utilities";
        private string _launchLabel = string.Empty;
        private string _launchCmd = string.Empty;
        private string? _editingLaunchCategory = null;
        private int? _editingLaunchIndex = null;
        private string _launchAddButtonText = "ADD APP";
        private string? _selectedLaunchpadItemRaw;

        private string _selectedThemeName = "Cyber Cyan";
        private bool _preventSleep;
        private bool _isDarkMode;

        public ObservableCollection<string> TargetListItems { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> LaunchpadListItems { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ThemeNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string> { "Utilities", "Apps", "Scripts" };

        public Dictionary<string, string> Themes { get; } = new Dictionary<string, string>
        {
            { "Cyber Cyan", "#0284C7" },
            { "Matrix Green", "#10B981" },
            { "Warning Amber", "#F59E0B" },
            { "Crimson Red", "#EF4444" },
            { "Neon Purple", "#8B5CF6" },
            { "Clean White", "#0F172A" }
        };

        public TargetItem? SelectedTarget
        {
            get => _selectedTarget;
            set => SetProperty(ref _selectedTarget, value);
        }

        public string TargetName { get => _targetName; set => SetProperty(ref _targetName, value); }
        public string TargetHost { get => _targetHost; set => SetProperty(ref _targetHost, value); }
        public string TargetAddButtonText { get => _targetAddButtonText; set => SetProperty(ref _targetAddButtonText, value); }

        public string SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }
        public string LaunchLabel { get => _launchLabel; set => SetProperty(ref _launchLabel, value); }
        public string LaunchCmd { get => _launchCmd; set => SetProperty(ref _launchCmd, value); }
        public string LaunchAddButtonText { get => _launchAddButtonText; set => SetProperty(ref _launchAddButtonText, value); }
        public string? SelectedLaunchpadItemRaw { get => _selectedLaunchpadItemRaw; set => SetProperty(ref _selectedLaunchpadItemRaw, value); }

        public string SelectedThemeName { get => _selectedThemeName; set => SetProperty(ref _selectedThemeName, value); }
        public bool PreventSleep { get => _preventSleep; set => SetProperty(ref _preventSleep, value); }
        public bool IsDarkMode { get => _isDarkMode; set => SetProperty(ref _isDarkMode, value); }

        public ICommand AddTargetCommand { get; }
        public ICommand EditTargetCommand { get; }
        public ICommand DeleteTargetCommand { get; }

        public ICommand BrowseFileCommand { get; }
        public ICommand AddLaunchCommand { get; }
        public ICommand EditLaunchCommand { get; }
        public ICommand DeleteLaunchCommand { get; }

        public ICommand ApplySettingsCommand { get; }

        public ConfigViewModel(ConfigService configService, Action closeWindowAction)
        {
            _configService = configService;
            _closeWindowAction = closeWindowAction;

            foreach (var tName in Themes.Keys) ThemeNames.Add(tName);

            AddTargetCommand = new RelayCommand(AddTarget);
            EditTargetCommand = new RelayCommand(EditTarget);
            DeleteTargetCommand = new RelayCommand(DeleteTarget);

            BrowseFileCommand = new RelayCommand(BrowseFile);
            AddLaunchCommand = new RelayCommand(AddLaunch);
            EditLaunchCommand = new RelayCommand(EditLaunch);
            DeleteLaunchCommand = new RelayCommand(DeleteLaunch);

            ApplySettingsCommand = new RelayCommand(ApplySettings);

            LoadData();
        }

        private void LoadData()
        {
            var cfg = _configService.Current;
            PreventSleep = cfg.PreventSleep;
            IsDarkMode = cfg.Theme.IsDarkMode;

            string currentHex = cfg.Theme.AccentCyan.ToLower();
            SelectedThemeName = Themes.FirstOrDefault(x => x.Value.ToLower() == currentHex).Key ?? "Cyber Cyan";

            RefreshTargetList();
            RefreshLaunchList();
        }

        private void RefreshTargetList()
        {
            TargetListItems.Clear();
            foreach (var t in _configService.Current.Targets)
            {
                TargetListItems.Add($"{t.Name} ({t.Host})");
            }
        }

        private void AddTarget()
        {
            string name = TargetName.Trim();
            string host = TargetHost.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(host)) return;

            if (_editingTargetIndex.HasValue)
            {
                _configService.Current.Targets[_editingTargetIndex.Value] = new TargetItem { Name = name, Host = host };
                _editingTargetIndex = null;
                TargetAddButtonText = "ADD TARGET";
            }
            else
            {
                _configService.Current.Targets.Add(new TargetItem { Name = name, Host = host });
            }

            _configService.SaveConfig();
            TargetName = string.Empty;
            TargetHost = string.Empty;
            RefreshTargetList();
        }

        private void EditTarget()
        {
            if (SelectedTargetIndex < 0 || SelectedTargetIndex >= _configService.Current.Targets.Count) return;
            var t = _configService.Current.Targets[SelectedTargetIndex];
            TargetName = t.Name;
            TargetHost = t.Host;
            _editingTargetIndex = SelectedTargetIndex;
            TargetAddButtonText = "UPDATE TARGET";
        }

        private void DeleteTarget()
        {
            if (SelectedTargetIndex < 0 || SelectedTargetIndex >= _configService.Current.Targets.Count) return;
            _configService.Current.Targets.RemoveAt(SelectedTargetIndex);
            _configService.SaveConfig();
            _editingTargetIndex = null;
            TargetAddButtonText = "ADD TARGET";
            TargetName = string.Empty;
            TargetHost = string.Empty;
            RefreshTargetList();
        }

        public int SelectedTargetIndex { get; set; } = -1;
        public int SelectedLaunchIndex { get; set; } = -1;

        private void RefreshLaunchList()
        {
            LaunchpadListItems.Clear();
            foreach (var cat in _configService.Current.Launcher)
            {
                foreach (var item in cat.Value)
                {
                    LaunchpadListItems.Add($"[{cat.Key}] {item.Label}");
                }
            }
        }

        private void BrowseFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Application",
                Filter = "Executables (*.exe)|*.exe|Scripts (*.bat;*.cmd;*.ps1;*.py)|*.bat;*.cmd;*.ps1;*.py|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.FileName;
                if (path.Contains(" ")) path = $"\"{path}\"";
                LaunchCmd = path;

                if (string.IsNullOrEmpty(LaunchLabel))
                {
                    string filename = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                    LaunchLabel = char.ToUpper(filename[0]) + filename.Substring(1);
                }
            }
        }

        private void AddLaunch()
        {
            string label = LaunchLabel.Trim();
            string cmd = LaunchCmd.Trim();
            string cat = SelectedCategory;

            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(cmd) || string.IsNullOrEmpty(cat)) return;

            if (!_configService.Current.Launcher.ContainsKey(cat))
                _configService.Current.Launcher[cat] = new List<LaunchItem>();

            if (_editingLaunchIndex.HasValue && _editingLaunchCategory != null)
            {
                if (_editingLaunchCategory != cat)
                {
                    _configService.Current.Launcher[_editingLaunchCategory].RemoveAt(_editingLaunchIndex.Value);
                    _configService.Current.Launcher[cat].Add(new LaunchItem { Label = label, Cmd = cmd });
                }
                else
                {
                    _configService.Current.Launcher[cat][_editingLaunchIndex.Value] = new LaunchItem { Label = label, Cmd = cmd };
                }

                _editingLaunchIndex = null;
                _editingLaunchCategory = null;
                LaunchAddButtonText = "ADD APP";
            }
            else
            {
                _configService.Current.Launcher[cat].Add(new LaunchItem { Label = label, Cmd = cmd });
            }

            _configService.SaveConfig();
            LaunchLabel = string.Empty;
            LaunchCmd = string.Empty;
            RefreshLaunchList();
        }

        private void EditLaunch()
        {
            if (SelectedLaunchpadItemRaw == null) return;
            string raw = SelectedLaunchpadItemRaw;
            int endBracket = raw.IndexOf(']');
            if (endBracket == -1) return;

            string cat = raw.Substring(1, endBracket - 1);
            string label = raw.Substring(endBracket + 1).Trim();

            if (_configService.Current.Launcher.TryGetValue(cat, out var items))
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Label == label)
                    {
                        LaunchLabel = items[i].Label;
                        LaunchCmd = items[i].Cmd;
                        SelectedCategory = cat;
                        _editingLaunchIndex = i;
                        _editingLaunchCategory = cat;
                        LaunchAddButtonText = "UPDATE APP";
                        break;
                    }
                }
            }
        }

        private void DeleteLaunch()
        {
            if (SelectedLaunchpadItemRaw == null) return;
            string raw = SelectedLaunchpadItemRaw;
            int endBracket = raw.IndexOf(']');
            if (endBracket == -1) return;

            string cat = raw.Substring(1, endBracket - 1);
            string label = raw.Substring(endBracket + 1).Trim();

            if (_configService.Current.Launcher.TryGetValue(cat, out var items))
            {
                items.RemoveAll(x => x.Label == label);
                _configService.SaveConfig();
                _editingLaunchIndex = null;
                _editingLaunchCategory = null;
                LaunchAddButtonText = "ADD APP";
                LaunchLabel = string.Empty;
                LaunchCmd = string.Empty;
                RefreshLaunchList();
            }
        }

        private void ApplySettings()
        {
            if (Themes.TryGetValue(SelectedThemeName, out string? hex))
            {
                _configService.Current.Theme.AccentCyan = hex;
            }

            _configService.Current.Theme.IsDarkMode = IsDarkMode;
            _configService.Current.PreventSleep = PreventSleep;
            _configService.SaveConfig();

            _closeWindowAction();
        }
    }
}
