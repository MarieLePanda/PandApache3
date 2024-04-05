using pandapache.src.RequestHandling;
using pandapache.src.ResponseGeneration;
using pandapache.src.Configuration;
using System.Text;

namespace pandapache.src.Middleware
{

    public class RoutingMiddleware
    {

        private readonly Func<HttpContext, Task> _next;
        private readonly IFileManager _FileManager;

        public RoutingMiddleware(Func<HttpContext, Task> next, IFileManager fileManager)
        {
            _next = next;
            _FileManager = fileManager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Console.WriteLine("Router Middleware");

            Request request = context.Request;
            try
            {
                string mainDirectory = ServerConfiguration.Instance.RootDirectory;
                string filePath = Path.Combine(mainDirectory, GetFilePath(request.Path));
                if (_FileManager.Exists(filePath))
                {
                    string fileExtension = Path.GetExtension(filePath);
                    if (fileExtension == ".png")
                    {
                        byte[] imageData = await File.ReadAllBytesAsync(filePath);
                        HttpResponse httpResponse = new HttpResponse(200)
                        {
                            Body = new MemoryStream(imageData)
                        };
                        httpResponse.AddHeader("Content-Type", "image/png");
                        httpResponse.AddHeader("Content-Length", httpResponse.Body.Length.ToString());
                        context.Response = httpResponse;

                    }
                    else
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        DateTime lastModified = fileInfo.LastWriteTime;

                        byte[] fileContent = await _FileManager.ReadAllBytesAsync(filePath);
                        HttpResponse response = new HttpResponse(200)
                        {
                            Body = new MemoryStream(fileContent)
                        };
                        SetContentTypeAndLength(response, "text/html; charset=utf-8");
                        //response.AddHeader("Last-Modified", lastModified.ToUniversalTime().ToString("R"));
                        context.Response = response;
                    }
                }
                else if (request.Path.StartsWith("/echo"))
                {
                   context.Response = await EchoHandler(request);
                }
                else if (request.Path.Equals("/user-agent"))
                {
                    context.Response = UserAgentHandler(request);
                }
                else
                {
                    context.Response = new HttpResponse(404);
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Return a generic error response
                context.Response = new HttpResponse(500);
            }

            await _next(context);

        }

        private static string GetFilePath(string path)
        {
            if (path == "/")
                return "index.html";
            else
                return path.Substring(1);
        }

        private static async Task<HttpResponse> EchoHandler(Request request)
        {
            string body = request.Path.Replace("/echo/", "");
            HttpResponse response = new HttpResponse(200)
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(body))
            };
            SetContentTypeAndLength(response, "text/plain; charset=utf-8");
            return response;
        }

        private static HttpResponse UserAgentHandler(Request request)
        {
            string userAgent = request.Headers["User-Agent"];
            HttpResponse response = new HttpResponse(200)
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(userAgent))
            };
            SetContentTypeAndLength(response, "text/plain; charset=utf-8");
            return response;
        }

        private static void SetContentTypeAndLength(HttpResponse response, string contentType)
        {
            response.AddHeader("Content-Type", contentType);
            response.AddHeader("Content-Length", response.Body.Length.ToString());
        }
    }
}
