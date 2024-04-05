
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
    }
}