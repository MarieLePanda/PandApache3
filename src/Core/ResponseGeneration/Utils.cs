using PandApache3.src.Core.Configuration;


namespace PandApache3.src.Core.ResponseGeneration
{
    public static class Utils
    {
        public static string GetFilePath(string path)
        {
            if (path == "/")
                return "index.html";
            else
            {
                if (ServerConfiguration.Instance.Platform.Equals("WIN"))
                    path = path.Substring(1).Replace("/", "\\");
                else
                    path = path.Substring(1);
                return path;
            }
        }

    }
}
