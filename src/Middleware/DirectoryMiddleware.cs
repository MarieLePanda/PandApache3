using pandapache.src.Configuration;
using pandapache.src;
using pandapache.src.Middleware;
using pandapache.src.RequestHandling;
using PandApache3.src.Configuration;
using PandApache3.src.ResponseGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            if (directoryConfig != null)
            {
                if (directoryConfig.AllowedMethods != null) 
                {
                    if(directoryConfig.AllowedMethods.Contains(context.Request.Verb) == false)
                    {
                        Logger.LogError($"Verb {context.Request.Verb} not allowed for the directory {directoryConfig.Path}");
                        context.Response = new HttpResponse(401);

                        return;
                    }
                }
            }
            await _next(context);
        }
    }
}
