using PandApache3.src.Core.RequestHandling;

namespace PandApache3.src.Core.Middleware
{
    public interface IMiddleware
    {
        Task InvokeAsync(HttpContext context);
    }

}
