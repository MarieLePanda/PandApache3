using pandapache.src.LoggingAndMonitoring;
using pandapache.src.RequestHandling;

namespace pandapache.src.Middleware
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
                Logger.LogInfo("Logging Middleware");
                LogRequest(context.Request);
                await _next(context);
                LogResponse(context.Response);
            }

            private void LogRequest(Request request)
            {
                Logger.LogInfo("Log Request");
                string logMessage = $"[{DateTime.UtcNow}] {request.Verb} {request.Path}";
                Logger.LogInfo(logMessage);
            }

            private void LogResponse(HttpResponse response)
            {
                Logger.LogInfo("Log Response");
                string logMessage = $"[{DateTime.UtcNow}] Response status code: {response.StatusCode}";
                Logger.LogInfo(logMessage);
            }
        }
    }

