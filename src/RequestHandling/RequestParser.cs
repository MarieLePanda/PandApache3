
using pandapache.src.Configuration;
using pandapache.src.LoggingAndMonitoring;
using PandApache3.src.ResponseGeneration;

namespace pandapache.src.RequestHandling
{

    public class RequestParser
    {
        public static HttpRequest ParseRequest(string rawRequest)
        {
            if (string.IsNullOrEmpty(rawRequest))
            {
                throw new ArgumentException("Raw request cannot be null or empty.");
            }

            // Separate request lines
            string[] requestLines = rawRequest.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            // Parse first line to extract method and URL
            string[] firstLineParts = requestLines[0].Split(' ');
            string method = firstLineParts[0];
            string url = firstLineParts[1];

            // Create HttpRequest object
            HttpRequest httpRequest = new HttpRequest(method, url);

            // Parse headers
            for (int i = 1; i < requestLines.Length; i++)
            {
                string line = requestLines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    // Empty line indicates end of headers
                    break;
                }

                int separatorIndex = line.IndexOf(':');
                if (separatorIndex != -1)
                {
                    string headerName = line.Substring(0, separatorIndex).Trim();
                    string headerValue = line.Substring(separatorIndex + 1).Trim();
                    httpRequest.AddHeader(headerName, headerValue);
                }
            }

            // Parse body
            int bodyIndex = Array.IndexOf(requestLines, "");
            if (bodyIndex != -1 && bodyIndex < requestLines.Length - 1)
            {
                httpRequest.Body = requestLines[bodyIndex + 1];
            }

            return httpRequest;
        }

        public static HttpResponse UploadHandler(Request request, bool admin=false)
        {

            if (ServerConfiguration.Instance.AllowUpload)
            {
                string boundary = GetBoundary(request.Headers["Content-Type"]);
                string[] parts = request.Body.Split(new[] { boundary }, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    foreach (string part in parts)
                    {
                        if (part.Contains("filename="))
                        {
                            string fileName = GetFileName(part);
                            string fileData = GetFileData(part);
                            string downloadDirecotry = ServerConfiguration.Instance.DocumentDirectory;
                            if (admin)
                            {
                                downloadDirecotry = ServerConfiguration.Instance.AdminDirectory.Path;
                            }

                            FileManagerFactory.Instance().SaveFile(downloadDirecotry, fileName, fileData);
                        }
                    }

                    return new HttpResponse(200);

                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error during file saving: {ex.Message}");
                    return new HttpResponse(500);
                }

            }
            else
            {
                Logger.LogWarning("Document upload not allowed");
                return new HttpResponse(413);
            }

        }

        public static string GetBoundary(string contentType)
        {
            string[] parts = contentType.Split(';');
            string boundary = parts[1].Trim().Substring("boundary=".Length);
            return "--" + boundary;
        }

        public static string GetFileName(string part)
        {
            string[] lines = part.Split('\n');
            string contentDispositionLine = lines[1].Trim();
            int fileNameIndex = contentDispositionLine.IndexOf("filename=\"") + "filename=\"".Length;
            string fileName = contentDispositionLine.Substring(fileNameIndex);
            return fileName.Trim('"');
        }

        public static string GetFileData(string part)
        {
            string[] lines = part.Split('\n');
            return string.Join("\n", lines.Skip(2).Take(lines.Length - 4));
        }

    }
}