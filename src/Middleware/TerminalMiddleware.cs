using pandapache.src.LoggingAndMonitoring;
using pandapache.src.RequestHandling;

namespace pandapache.src.Middleware
{
    public class TerminalMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context)
        {
            Logger.LogDebug("Last middleware");
        }
    }

}
