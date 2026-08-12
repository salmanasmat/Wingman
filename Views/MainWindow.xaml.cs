using System;
using System.Windows;
using System.Windows.Input;
using Wingman.Services;
using Wingman.ViewModels;

namespace Wingman.Views
{
    public partial class MainWindow : Window
    {
        private readonly ConfigService _configService;
        private readonly MainViewModel _viewModel;
        private bool _isFullscreen = false;

        public MainWindow()
        {
            InitializeComponent();

            _configService = new ConfigService();
            _viewModel = new MainViewModel(_configService, OpenConfigDialog, ShowLogsDialog);
            DataContext = _viewModel;

            Closed += MainWindow_Closed;
        }

        private void OpenConfigDialog()
        {
            var dialog = new ConfigWindow(_configService)
            {
                Owner = this
            };
            dialog.ShowDialog();
        }

        private void ShowLogsDialog()
        {
            var dialog = new LogViewerWindow()
            {
                Owner = this
            };
            dialog.ShowDialog();
        }

        private void BottomBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowLogsDialog();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                ToggleFullscreen();
            }
            else if (e.Key == Key.Escape)
            {
                ExitFullscreen();
            }
        }

        private void ToggleFullscreen()
        {
            if (_isFullscreen)
            {
                ExitFullscreen();
            }
            else
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                _isFullscreen = true;

                if (_configService.Current.PreventSleep)
                {
                    PowerManagementService.SetKeepAwake(true);
                }
            }
        }

        private void ExitFullscreen()
        {
            if (_isFullscreen)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Maximized;
                _isFullscreen = false;

                PowerManagementService.SetKeepAwake(false);
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _viewModel.StopMonitoring();
            PowerManagementService.SetKeepAwake(false);
        }
    }
}
