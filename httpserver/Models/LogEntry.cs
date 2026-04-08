using System;

namespace httpserver.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; }
        public string Method { get; }
        public string Path { get; }
        public int StatusCode { get; }

        public LogEntry(string method, string path, int statusCode)
        {
            Timestamp = DateTime.Now;
            Method = method;
            Path = path;
            StatusCode = statusCode;
        }

        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss}] {Method} {Path} \u2192 {StatusCode}";
    }
}
