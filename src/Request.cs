
using pandapache.src.LoggingAndMonitoring;

namespace pandapache.src
{
    [Obsolete]
    public class Request
    {
        public string RequestString { get; }
        public string StartLine { get; }
        public string Verb { get; }
        public string Path {  get; }
        public string Body { get; }
        public string QueryString { get; }
        public Dictionary<string, string> queryParameters { get; }
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public Request(string requestString) 
        {
            RequestString = requestString;

            StringReader reader = new StringReader(requestString);

            // Lecture de la première ligne (start line)
            StartLine = reader.ReadLine();

            //List<string> requestList = requestString.Split("\r\n").ToList();

            Verb = StartLine.Split(" ")[0];
            Path = StartLine.Split(" ")[1];

            string line;
            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {
                int separatorIndex = line.IndexOf(':');
                if (separatorIndex > 0)
                {
                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();
                    Headers.Add(key, value);
                }
            }

            // Lecture du corps
            Body = reader.ReadToEnd();

            string[] pathParts = Path.Split('?');

            if (pathParts.Length > 1)
            {
                QueryString = pathParts[1];
            }

            queryParameters = GetQueryParameters();

        }

        private Dictionary<string, string> GetQueryParameters()
        {
            Logger.LogInfo("Reading query string parameter");
            var parameters = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(QueryString))
            {
                var keyValuePairs = QueryString.Split('&');

                foreach (var pair in keyValuePairs)
                {
                    var keyValue = pair.Split('=');
                    Logger.LogDebug($"KeyValue: {keyValue}");

                    if (keyValue.Length == 2)
                    {
                        var key = Uri.UnescapeDataString(keyValue[0]);
                        var value = Uri.UnescapeDataString(keyValue[1]);
                        parameters[key] = value;
                    }
                }
            }

            return parameters;
        }
    }
}
