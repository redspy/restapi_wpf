using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace httpserver
{
    public partial class MainWindow : Window
    {
        private HttpServer _server;
        private int _requestCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            BuildServer();
        }

        private void BuildServer()
        {
            var router = new Router();
            router.Get("/api/hello", ApiHandlers.Hello);
            router.Get("/api/status", ApiHandlers.Status);
            router.Post("/api/echo", ApiHandlers.Echo);

            _server = new HttpServer(router);
            _server.OnRequestLogged += OnRequestLogged;
        }

        private async void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_server.IsRunning)
                await StartServer();
            else
                await StopServer();
        }

        private async Task StartServer()
        {
            if (!int.TryParse(PortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("유효하지 않은 포트 번호입니다. (1~65535)", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ApiHandlers.ServerStartTime = DateTime.UtcNow;
                _server.Start(port);

                ToggleButton.Content = "Stop";
                ToggleButton.Background = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));
                StatusLabel.Content = "Running";
                StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
                UrlLabel.Content = $"http://localhost:{port}";
                PortTextBox.IsEnabled = false;
            }
            catch (HttpListenerException ex)
            {
                MessageBox.Show($"서버 시작 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StopServer()
        {
            try
            {
                await _server.StopAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"서버 중지 중 오류: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            ToggleButton.Content = "Start";
            ToggleButton.Background = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
            StatusLabel.Content = "Stopped";
            StatusLabel.Foreground = Brushes.Red;
            UrlLabel.Content = "";
            PortTextBox.IsEnabled = true;
        }

        private void OnRequestLogged(string logLine)
        {
            Dispatcher.InvokeAsync(() =>
            {
                LogListBox.Items.Add(logLine);
                _requestCount++;
                RequestCountLabel.Content = $"{_requestCount} requests";

                if (AutoScrollCheckBox.IsChecked == true && LogListBox.Items.Count > 0)
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            });
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
            _requestCount = 0;
            RequestCountLabel.Content = "0 requests";
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (_server.IsRunning)
                await _server.StopAsync();
        }
    }
}
