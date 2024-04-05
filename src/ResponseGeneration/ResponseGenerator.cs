using System.IO;
using System.Text;
using pandapache.src.RequestHandling;

namespace pandapache.src.ResponseGenerator
{
    public static class ResponseGenerator
    {
        public static string GenerateHttpResponse(HttpResponse httpResponse)
        {
            StringBuilder responseBuilder = new StringBuilder();

            // Start line
            responseBuilder.AppendLine($"HTTP/1.1 {httpResponse.StatusCode} {GetStatusMessage(httpResponse.StatusCode)}");

            // Headers
            foreach (var header in httpResponse.Headers)
            {
                responseBuilder.AppendLine($"{header.Key}: {header.Value}");
            }

            // Body
            if (httpResponse.Body != null)
            {
                httpResponse.Body.Seek(0, SeekOrigin.Begin); // Ensure we're reading from the beginning of the stream
                using (StreamReader reader = new StreamReader(httpResponse.Body))
                {
                    string bodyContent = reader.ReadToEnd();
                    responseBuilder.AppendLine($"Content-Length: {bodyContent.Length}");
                    responseBuilder.AppendLine(); // Empty line before body
                    responseBuilder.Append(bodyContent);
                }
            }
            else
            {
                responseBuilder.AppendLine(); // Empty line if no body
            }

            return responseBuilder.ToString();
        }

        private static string GetStatusMessage(int statusCode)
        {
            // You can add more status codes and messages as needed
            switch (statusCode)
            {
                case 200:
                    return "OK";
                case 404:
                    return "Not Found";
                default:
                    return "Unknown";
            }
        }
    }
}
