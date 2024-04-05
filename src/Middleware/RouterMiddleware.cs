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
                    string fileExtension = Path.GetExtension(filePath).Substring(1).ToLowerInvariant();
                    string mimeType = fileExtension switch
                    {
                        // Images
                        "jpg" or "jpeg" => "image/jpeg",
                        "png" => "image/png",
                        "gif" => "image/gif",
                        "svg" => "image/svg+xml",
                        "bmp" => "image/bmp",
                        "webp" => "image/webp",
                        "ico" => "image/x-icon",

                        // Documents textuels
                        "txt" => "text/plain",
                        "html" or "htm" => "text/html",
                        "css" => "text/css",
                        "js" => "application/javascript",
                        "json" => "application/json",
                        "xml" => "text/xml", // Ou "application/xml" selon le contexte d'usage

                        // Audio
                        "mp3" => "audio/mpeg",
                        "wav" => "audio/wav",
                        "ogg" => "audio/ogg",
                        "midi" => "audio/midi",

                        // Vidéo
                        "mp4" => "video/mp4",
                        "avi" => "video/x-msvideo",
                        "mov" => "video/quicktime",
                        "webm" => "video/webm",

                        // Archives
                        "zip" => "application/zip",
                        "rar" => "application/vnd.rar",
                        "7z" => "application/x-7z-compressed",

                        // Documents Office et formats ouverts
                        "pdf" => "application/pdf",
                        "doc" or "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        "xls" or "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "ppt" or "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                        "odt" => "application/vnd.oasis.opendocument.text",
                        "ods" => "application/vnd.oasis.opendocument.spreadsheet",

                        // Cas par défaut si l'extension n'est pas reconnue
                        _ => null
                    };

                    if (mimeType != null)
                    {
                        byte[] data = await File.ReadAllBytesAsync(filePath);
                        HttpResponse httpResponse = new HttpResponse(200)
                        {
                            Body = new MemoryStream(data)
                        };
                        httpResponse.AddHeader("Content-Type", mimeType);
                        httpResponse.AddHeader("Content-Length", httpResponse.Body.Length.ToString());
                        context.Response = httpResponse;

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
