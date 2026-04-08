using System;
using httpserver.Models;

namespace httpserver.Handlers
{
    public class StatusHandler : IRouteHandler
    {
        private readonly Func<DateTime> _startTimeProvider;

        public StatusHandler(Func<DateTime> startTimeProvider)
        {
            _startTimeProvider = startTimeProvider;
        }

        public string Method => "GET";
        public string Path => "/api/status";

        public ApiResponse Handle(RequestContext ctx) =>
            ApiResponse.Ok(new
            {
                status = "running",
                uptime_seconds = (int)(DateTime.UtcNow - _startTimeProvider()).TotalSeconds
            });
    }
}
