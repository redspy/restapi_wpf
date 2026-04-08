using System;

namespace httpserver.Models
{
    public struct RouteKey : IEquatable<RouteKey>
    {
        public string Method { get; }
        public string Path { get; }

        public RouteKey(string method, string path)
        {
            Method = method.ToUpperInvariant();
            Path = path.TrimEnd('/').ToLowerInvariant();
            if (string.IsNullOrEmpty(Path)) Path = "/";
        }

        public bool Equals(RouteKey other) =>
            Method == other.Method && Path == other.Path;

        public override bool Equals(object obj) =>
            obj is RouteKey other && Equals(other);

        public override int GetHashCode() =>
            (Method ?? "").GetHashCode() ^ (Path ?? "").GetHashCode();
    }
}
