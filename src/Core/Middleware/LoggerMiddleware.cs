using pandapache.src;
using PandApache3.src.Core.RequestHandling;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;

namespace PandApache3.src.Core.Middleware
{
    public class LoggerMiddleware
    {
        private readonly Func<HttpContext, Task> _next;
        public LoggerMiddleware(Func<HttpContext, Task> next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ExecutionContext.Current.Logger.LogInfo("LoggerMiddleware invoked");
            if (context.Request == null)
                return;

            LogRequest(context.Request);
            await _next(context);
            if (context.Response != null)
            {
                LogResponse(context.Response);
            }
            else
            {
                Console.WriteLine(context.Request.Path);
            }
        }

        private void LogRequest(Request request)
        {
            ExecutionContext.Current.Logger.LogInfo("Log Request");
            if (request != null)
            {
                string logMessage = $"[{DateTime.UtcNow}] {request.Verb} {request.Path}";
                ExecutionContext.Current.Logger.LogInfo(logMessage);
            }
        }

        private void LogResponse(HttpResponse response)
        {
            ExecutionContext.Current.Logger.LogInfo("Log Response");
            string logMessage = $"[{DateTime.UtcNow}] Response status code: {response.StatusCode}";
            ExecutionContext.Current.Logger.LogInfo(logMessage);
        }
    }
}

