using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using httpserver.Models;

namespace httpserver.Handlers
{
    public class EchoHandler : IRouteHandler
    {
        public string Method => "POST";
        public string Path => "/api/echo";

        public ApiResponse Handle(RequestContext ctx)
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
