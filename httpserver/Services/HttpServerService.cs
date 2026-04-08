using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using httpserver.Models;

namespace httpserver.Services
{
    public class HttpServerService
    {
        private readonly Router _router;
        private readonly RequestParser _parser;

        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private Task _listenTask;

        public event Action<LogEntry> RequestCompleted;

        public bool IsRunning => _listener != null && _listener.IsListening;

        public HttpServerService(Router router, RequestParser parser)
        {
            _router = router;
            _parser = parser;
        }

        public void Start(int port)
        {
            if (IsRunning) throw new InvalidOperationException("Server is already running.");

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();

            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            if (!IsRunning) return;

            _cts.Cancel();
            _listener.Stop();

            try { await _listenTask; }
            catch { }

            _listener.Close();
            _listener = null;
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync();
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }

                _ = Task.Run(() => HandleRequest(ctx));
            }
        }

        private void HandleRequest(HttpListenerContext listenerCtx)
        {
            var req = listenerCtx.Request;
            var res = listenerCtx.Response;

            var ctx = _parser.Parse(req);
            ApiResponse apiResponse;

            try
            {
                apiResponse = _router.Dispatch(ctx);
            }
            catch (Exception ex)
            {
                apiResponse = ApiResponse.Error(ex.Message);
            }

            try
            {
                res.StatusCode = apiResponse.StatusCode;
                res.ContentType = apiResponse.ContentType;
                byte[] buffer = Encoding.UTF8.GetBytes(apiResponse.Body);
                res.ContentLength64 = buffer.Length;
                res.OutputStream.Write(buffer, 0, buffer.Length);
                res.OutputStream.Close();
            }
            catch { }

            var entry = new LogEntry(req.HttpMethod, req.Url.PathAndQuery, apiResponse.StatusCode);
            RequestCompleted?.Invoke(entry);
        }
    }
}
