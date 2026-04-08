using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using httpserver.Models;

namespace httpserver
{
    public class HttpServer
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private Task _listenTask;
        private readonly Router _router;

        public event Action<string> OnRequestLogged;

        public bool IsRunning => _listener != null && _listener.IsListening;

        public HttpServer(Router router)
        {
            _router = router;
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
            catch { /* TaskCanceledException / ObjectDisposedException는 정상 */ }

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

                Task.Run(() => HandleRequest(ctx));
            }
        }

        private void HandleRequest(HttpListenerContext listenerCtx)
        {
            var req = listenerCtx.Request;
            var res = listenerCtx.Response;

            RequestContext ctx = BuildRequestContext(req);
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
            catch { /* 클라이언트가 연결을 끊은 경우 무시 */ }

            string logLine = $"[{DateTime.Now:HH:mm:ss}] {req.HttpMethod} {req.Url.PathAndQuery} → {apiResponse.StatusCode}";
            OnRequestLogged?.Invoke(logLine);
        }

        private RequestContext BuildRequestContext(HttpListenerRequest req)
        {
            string body = "";
            if (req.HasEntityBody)
                using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    body = reader.ReadToEnd();

            var queryParams = new Dictionary<string, string>();
            foreach (string key in req.QueryString.AllKeys)
                if (key != null)
                    queryParams[key] = req.QueryString[key];

            string path = req.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            if (string.IsNullOrEmpty(path)) path = "/";

            return new RequestContext
            {
                Method = req.HttpMethod.ToUpperInvariant(),
                Path = path,
                RawBody = body,
                Headers = req.Headers.AllKeys
                    .Where(k => k != null)
                    .ToDictionary(k => k, k => req.Headers[k]),
                QueryParams = queryParams
            };
        }
    }
}
