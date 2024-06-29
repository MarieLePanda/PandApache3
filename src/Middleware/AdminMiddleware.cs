﻿using pandapache.src.Configuration;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.RequestHandling;
using System.Text;

namespace pandapache.src.Middleware
{
    public class AdminMiddleware : IMiddleware
    {
        private readonly Func<HttpContext, Task> _next;
        public AdminMiddleware(Func<HttpContext, Task> next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Logger.LogDebug("Admin middleware");

            Request request = context.Request;

            if (request.Verb == "GET")
            {
                context.Response = getAdmin(request);
            }

            await _next(context);
        }

        private HttpResponse getAdmin(Request request)
        {
            Logger.LogInfo($"Request for admin: {request.Path}");
            HttpResponse response;
            string adminURL = ServerConfiguration.Instance.AdminDirectory.Path;
            Logger.LogDebug($"URL for adminsitration: {adminURL}");
            if (request.Path.ToLower().Equals(adminURL + "/status"))
            {
                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("PandApache3 is up and running !"))
                };

            }
            if (request.Path.ToLower().Equals(adminURL + "/stop"))
            {
                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Stopping PandApache3..."))
                };
                Server.StopServer();
            }
            else
            {
                response = new HttpResponse(404)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Oh oh, you should not have seen that"))
                };
            }
            return response;
        }
    }

}