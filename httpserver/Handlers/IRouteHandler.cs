using httpserver.Models;

namespace httpserver.Handlers
{
    public interface IRouteHandler
    {
        string Method { get; }
        string Path { get; }
        ApiResponse Handle(RequestContext ctx);
    }
}
