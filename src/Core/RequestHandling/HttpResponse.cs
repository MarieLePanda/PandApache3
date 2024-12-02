namespace PandApache3.src.Core.RequestHandling
{
    public class HttpResponse
    {
        public int StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public Stream Body { get; set; }

        public HttpResponse(int statusCode)
        {
            StatusCode = statusCode;
        }

        public void AddHeader(string key, string value)
        {
            Headers[key] = value;
        }

        public override string ToString()
        {
            string headersString = string.Join("\r\n", Headers.Select(kv => $"{kv.Key}: {kv.Value}"));
            return $"HTTP/1.1 {StatusCode} OK\r\n{headersString}\r\n\r\n";
        }
    }
}