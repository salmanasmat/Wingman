using System.Windows;
using Wingman.ViewModels;

namespace Wingman.Views
{
    public partial class LogViewerWindow : Window
    {
        public LogViewerWindow()
        {
            InitializeComponent();
            DataContext = new LogViewerViewModel();
        }
    }
}
