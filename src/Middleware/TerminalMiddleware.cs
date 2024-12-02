using pandapache.src.RequestHandling;
using ExecutionContext = PandApache3.src.Module.ExecutionContext;

namespace pandapache.src.Middleware
{
    public class TerminalMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context)
        {
            ExecutionContext.Current.Logger.LogDebug("Last middleware");
        }
    }

}
