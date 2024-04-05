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

                // Log incoming request
                LogRequest(context.Request);

                // Call the next middleware in the pipeline
                await _next(context);

                // Log outgoing response
                LogResponse(context.Response);

            }

            private void LogRequest(Request request)
            {
                Logger.LogInfo("Log Request");
                // Log request details
                string logMessage = $"[{DateTime.UtcNow}] {request.Verb} {request.Path}";
                Logger.LogInfo(logMessage);
            }

            private void LogResponse(HttpResponse response)
            {
                Logger.LogInfo("Log Response");
                // Log response details
                string logMessage = $"[{DateTime.UtcNow}] Response status code: {response.StatusCode}";
                Logger.LogInfo(logMessage);
            }
        }
    }

