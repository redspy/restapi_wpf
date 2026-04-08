using System.Collections.Generic;
using httpserver.Handlers;
using httpserver.Models;

namespace httpserver.Services
{
    public class Router
    {
        private readonly Dictionary<RouteKey, IRouteHandler> _routes =
            new Dictionary<RouteKey, IRouteHandler>();

        public void Register(IRouteHandler handler)
        {
            _routes[new RouteKey(handler.Method, handler.Path)] = handler;
        }

        public ApiResponse Dispatch(RequestContext ctx)
        {
            var key = new RouteKey(ctx.Method, ctx.Path);
            if (_routes.TryGetValue(key, out IRouteHandler handler))
                return handler.Handle(ctx);
            return ApiResponse.NotFound($"No route for {ctx.Method} {ctx.Path}");
        }
    }
}
