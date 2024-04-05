
namespace pandapache.src.RequestHandling
{
    public class HttpRequest
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public string Body { get; set; }

        public HttpRequest(string method, string url)
        {
            Method = method;
            Url = url;
        }

        public void AddHeader(string key, string value)
        {
            Headers[key] = value;
        }

        public override string ToString()
        {
            string headersString = string.Join("\n", Headers.Select(kv => $"{kv.Key}: {kv.Value}"));
            return $"Method: {Method}\nUrl: {Url}\nHeaders:\n{headersString}\nBody: {Body}";
        }
    }
}