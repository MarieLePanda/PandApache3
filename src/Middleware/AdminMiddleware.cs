using pandapache.src.Configuration;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.RequestHandling;
using System.Text;
using System.Xml;

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
            else if (request.Verb == "POST")
            {
                context.Response = postAdmin(request);
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
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(Server.STATUS))
                };

            }
            else if (request.Path.ToLower().Equals(adminURL + "/reload"))
            {
                ServerConfiguration.Instance.ReloadConfiguration();
                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Configuration reloaded"))
                };

            }
            else if (request.Path.ToLower().Equals(adminURL + "/config"))
            {

                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(ServerConfiguration.Instance.ExportJSON()))
                };
            }
            else if (request.Path.ToLower().Equals(adminURL + "/stop"))
            {
                Task.Run(() => Server.StoppingServerAsync(false));

                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Stopping...."))
                };
            }
            else if (request.Path.ToLower().Equals(adminURL + "/restart"))
            {
                Task.Run(() => Server.StoppingServerAsync(true));

                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Restarting...."))
                };
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

        private HttpResponse postAdmin(Request request)
        {
            HttpResponse response = new HttpResponse(200);

            string adminURL = ServerConfiguration.Instance.AdminDirectory.Path;

            if (request.Path.ToLower().StartsWith(adminURL + "/config"))
            {
                Logger.LogDebug($"QueryString: {request.QueryString}");

                if (request.queryParameters.Count > 0)
                {
                    foreach (var item in request.queryParameters)
                    {
                        Logger.LogDebug($"key and value: <{item.Key}/{item.Value}>");
                        ServerConfiguration.Instance.MapConfiguration(item.Key, item.Value);
                    }

                    ServerConfiguration.Instance.Export(Path.Combine(ServerConfiguration.Instance._configurationPath,"PandApache3.conf"));
                }
            }
            return response;
        }
    }

}
