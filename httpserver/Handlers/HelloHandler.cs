using System;
using httpserver.Models;

namespace httpserver.Handlers
{
    public class HelloHandler : IRouteHandler
    {
        public string Method => "GET";
        public string Path => "/api/hello";

        public ApiResponse Handle(RequestContext ctx) =>
            ApiResponse.Ok(new
            {
                message = "Hello, World!",
                timestamp = DateTime.UtcNow.ToString("o")
            });
    }
}
