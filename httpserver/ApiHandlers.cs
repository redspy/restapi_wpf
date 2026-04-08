using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using httpserver.Models;

namespace httpserver
{
    public static class ApiHandlers
    {
        public static DateTime ServerStartTime { get; set; }

        // GET /api/hello
        public static ApiResponse Hello(RequestContext ctx)
        {
            return ApiResponse.Ok(new
            {
                message = "Hello, World!",
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        // GET /api/status
        public static ApiResponse Status(RequestContext ctx)
        {
            return ApiResponse.Ok(new
            {
                status = "running",
                uptime_seconds = (int)(DateTime.UtcNow - ServerStartTime).TotalSeconds
            });
        }

        // POST /api/echo
        public static ApiResponse Echo(RequestContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.RawBody))
                return ApiResponse.BadRequest("Request body is empty");

            var serializer = new JavaScriptSerializer();
            Dictionary<string, object> incoming;
            try
            {
                incoming = serializer.Deserialize<Dictionary<string, object>>(ctx.RawBody);
            }
            catch
            {
                return ApiResponse.BadRequest("Invalid JSON body");
            }

            incoming["echoed"] = true;
            incoming["timestamp"] = DateTime.UtcNow.ToString("o");
            return ApiResponse.Ok(incoming);
        }
    }
}
