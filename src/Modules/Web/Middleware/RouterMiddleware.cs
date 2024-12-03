using System.Text;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;
using PandApache3.src.Core.RequestHandling;
using pandapache.src;
using PandApache3.src.Core.Middleware;
using PandApache3.src.Core.ResponseGeneration;
using PandApache3.src.Core.Configuration;

namespace PandApache3.src.Modules.Web.Middleware
{

    public class RoutingMiddleware : IMiddleware
    {

        private Func<HttpContext, Task> _next;
        private readonly IFileManager _FileManager;
        public RoutingMiddleware(Func<HttpContext, Task> next, IFileManager fileManager)
        {
            _next = next;
            _FileManager = fileManager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ExecutionContext.Current.Logger.LogDebug("Router Middleware");

            if (context.Request.Verb.ToUpper().Equals("GET"))
            {
                await GetHandlerAsync(context);
            }
            else if (context.Request.Verb.ToUpper().Equals("POST"))
            {
                if (context.Request.Headers["Content-Type"] != null && context.Request.Headers["Content-Type"].StartsWith("multipart/form-data"))
                {
                    context.Response = RequestParser.UploadHandler(context.Request);
                }
                else
                {
                    context.Response = new HttpResponse(404);

                }
            }
            else
            {
                context.Response = new HttpResponse(404);
            }

            await _next(context);

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

        private async Task GetHandlerAsync(HttpContext context)
        {
            Request request = context.Request;

            try
            {
                bool AuthNeeded = false;
                string mainDirectory = ServerConfiguration.Instance.RootDirectory;
                string filePath = Path.Combine(mainDirectory, Utils.GetFilePath(request.Path));

                DirectoryConfig directoryConfig = ServerConfiguration.Instance.GetDirectory(filePath);

                if (directoryConfig != null && directoryConfig.Require.Equals("valid-user"))
                {
                    ExecutionContext.Current.Logger.LogDebug($"Authentification requested");
                    AuthNeeded = true;
                }
                if (AuthNeeded && context.isAuth == false)
                {
                    context.Response = new HttpResponse(401);
                    context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Authentification\"";
                    ExecutionContext.Current.Logger.LogWarning($"User not authenticated");
                    return;
                }
                if (request.Path.StartsWith("/echo"))
                {
                    context.Response = await EchoHandler(request);
                }
                else if (request.Path.Equals("/user-agent"))
                {
                    context.Response = UserAgentHandler(request);
                }
                else if (_FileManager.Exists(filePath))
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
                        "xml" => "text/xml",

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
                else
                {
                    context.Response = new HttpResponse(404);
                }
            }
            catch (Exception ex)
            {
                ExecutionContext.Current.Logger.LogError($"An error occurred with a GET request: {ex.Message}");
                context.Response = new HttpResponse(500);
            }
        }


    }


}
