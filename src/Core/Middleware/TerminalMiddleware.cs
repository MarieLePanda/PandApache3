using PandApache3.src.Core.RequestHandling;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;

namespace PandApache3.src.Core.Middleware
{
    public class TerminalMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context)
        {
            ExecutionContext.Current.Logger.LogDebug("Last middleware");
        }
    }

}
