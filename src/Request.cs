
namespace pandapache.src
{
    [Obsolete]
    public class Request
    {
        public string RequestString { get; }
        public string StartLine { get; }
        public string Verb { get; }
        public string Path {  get; }
        
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public Request(string requestString) 
        {
            RequestString = requestString;
            //Console.WriteLine(RequestString);
            List<string> requestList = requestString.Split("\r\n").ToList();
            StartLine = requestList[0];

            Verb = StartLine.Split(" ")[0];
            Path = StartLine.Split(" ")[1];

            foreach (string line in requestList)
            {
                if (line.Contains(":"))
                {
                    string key = (string)line.Split(":").GetValue(0);
                    string value = (string)line.Split(":").GetValue(1);

                    key = key.Remove(key.Length);

                    Headers.Add(key, value.Substring(1));

                }
            }

        }
    }
}
