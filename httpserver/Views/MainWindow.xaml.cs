using System.ComponentModel;
using System.Windows;
using httpserver.ViewModels;

namespace httpserver.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _viewModel.LogEntryAdded += OnLogEntryAdded;
            _viewModel.ValidationError += msg =>
                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OnLogEntryAdded()
        {
            if (_viewModel.AutoScroll && LogListBox.Items.Count > 0)
                LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            await _viewModel.StopIfRunningAsync();
        }
    }
}
