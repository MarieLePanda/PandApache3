using pandapache.src.Configuration;
using pandapache.src.Middleware;
using pandapache.src.RequestHandling;
using PandApache3.src.Configuration;
using PandApache3.src.ResponseGeneration;
using pandapache.src.LoggingAndMonitoring;

namespace PandApache3.src.Middleware
{
    public class DirectoryMiddleware : IMiddleware
    {
        private readonly Func<HttpContext, Task> _next;

        public DirectoryMiddleware(Func<HttpContext, Task> next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Logger.LogDebug("DirectoryMiddleware");
            string mainDirectory = ServerConfiguration.Instance.RootDirectory;
            string filePath = Path.Combine(mainDirectory, Utils.GetFilePath(context.Request.Path));

            DirectoryConfig directoryConfig = ServerConfiguration.Instance.GetDirectory(filePath);

            bool directoryAccess = DirectorySecurity(context, directoryConfig);
            bool verbAccess = VerbSecurity(context, directoryConfig);

            if (directoryAccess && verbAccess) 
            {
                await _next(context);
            }
        }

        private bool DirectorySecurity(HttpContext context, DirectoryConfig directoryConfig) 
        {
            bool AuthNeeded = false;

            if (directoryConfig != null && directoryConfig.Require.Equals("valid-user"))
            {
                Logger.LogDebug($"Authentification requested");
                AuthNeeded = true;
            }
            if (AuthNeeded && context.isAuth == false)
            {
                context.Response = new HttpResponse(401);
                context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Authentification\"";
                Logger.LogWarning($"User not authenticated");
                return false;
            }

            return true;
        }

        private bool VerbSecurity(HttpContext context, DirectoryConfig directoryConfig)
        {
            if (directoryConfig != null)
            {
                if (directoryConfig.AllowedMethods != null)
                {
                    if (directoryConfig.AllowedMethods.Contains(context.Request.Verb) == false)
                    {
                        Logger.LogError($"Verb {context.Request.Verb} not allowed for the directory {directoryConfig.Path}");
                        context.Response = new HttpResponse(401);

                        return false;
                    }
                }
            }

            return true;
        }
    }
}
