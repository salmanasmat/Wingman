using System.Windows;
using Wingman.Services;
using Wingman.ViewModels;

namespace Wingman.Views
{
    public partial class ConfigWindow : Window
    {
        public ConfigWindow(ConfigService configService)
        {
            InitializeComponent();
            DataContext = new ConfigViewModel(configService, () => Close());
        }
    }
}
