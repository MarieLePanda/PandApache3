using pandapache.src.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.ResponseGeneration
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

                return path;
            }
        }
    }
}
