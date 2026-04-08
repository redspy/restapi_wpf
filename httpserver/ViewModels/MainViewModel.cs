using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using httpserver.Handlers;
using httpserver.Models;
using httpserver.Services;

namespace httpserver.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly HttpServerService _server;
        private DateTime _serverStartTime;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action LogEntryAdded;
        public event Action<string> ValidationError;

        public ObservableCollection<LogEntry> LogEntries { get; } =
            new ObservableCollection<LogEntry>();

        // --- Commands ---
        public ICommand StartStopCommand { get; }
        public ICommand ClearLogCommand { get; }

        // --- Properties ---
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StartStopLabel));
                OnPropertyChanged(nameof(UrlText));
                OnPropertyChanged(nameof(IsPortEditable));
            }
        }

        private string _port = "8080";
        public string Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        private int _requestCount;
        public int RequestCount
        {
            get => _requestCount;
            private set { _requestCount = value; OnPropertyChanged(); }
        }

        public bool AutoScroll { get; set; } = true;

        // --- Computed Properties ---
        public string StatusText    => IsRunning ? "Running" : "Stopped";
        public string StartStopLabel => IsRunning ? "Stop" : "Start";
        public string UrlText       => IsRunning ? $"http://localhost:{Port}" : "";
        public bool   IsPortEditable => !IsRunning;

        public MainViewModel()
        {
            var router = BuildRouter();
            _server = new HttpServerService(router, new RequestParser());
            _server.RequestCompleted += OnRequestCompleted;

            StartStopCommand = new AsyncRelayCommand(
                () => IsRunning ? StopServerAsync() : StartServerAsync());

            ClearLogCommand = new RelayCommand(() =>
            {
                LogEntries.Clear();
                RequestCount = 0;
            });
        }

        private Router BuildRouter()
        {
            var router = new Router();
            router.Register(new HelloHandler());
            router.Register(new StatusHandler(() => _serverStartTime));
            router.Register(new EchoHandler());
            return router;
        }

        private Task StartServerAsync()
        {
            if (!int.TryParse(Port, out int port) || port < 1 || port > 65535)
            {
                ValidationError?.Invoke("Invalid port number. (1-65535)");
                return Task.CompletedTask;
            }

            try
            {
                _serverStartTime = DateTime.UtcNow;
                _server.Start(port);
                IsRunning = true;
            }
            catch (HttpListenerException ex)
            {
                ValidationError?.Invoke($"Server failed to start: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private async Task StopServerAsync()
        {
            await _server.StopAsync();
            IsRunning = false;
        }

        public async Task StopIfRunningAsync()
        {
            if (IsRunning) await StopServerAsync();
        }

        private void OnRequestCompleted(LogEntry entry)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LogEntries.Add(entry);
                RequestCount++;
                LogEntryAdded?.Invoke();
            });
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
