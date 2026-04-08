using System.Collections.Generic;
using httpserver.Models;

namespace httpserver
{
    public delegate ApiResponse RouteHandler(RequestContext ctx);

    public class Router
    {
        private readonly Dictionary<RouteKey, RouteHandler> _routes =
            new Dictionary<RouteKey, RouteHandler>();

        public void Get(string path, RouteHandler handler) => Register("GET", path, handler);
        public void Post(string path, RouteHandler handler) => Register("POST", path, handler);
        public void Put(string path, RouteHandler handler) => Register("PUT", path, handler);
        public void Delete(string path, RouteHandler handler) => Register("DELETE", path, handler);

        private void Register(string method, string path, RouteHandler handler)
        {
            _routes[new RouteKey(method, path)] = handler;
        }

        public ApiResponse Dispatch(RequestContext ctx)
        {
            var key = new RouteKey(ctx.Method, ctx.Path);
            if (_routes.TryGetValue(key, out RouteHandler handler))
                return handler(ctx);
            return ApiResponse.NotFound($"No route for {ctx.Method} {ctx.Path}");
        }
    }
}
