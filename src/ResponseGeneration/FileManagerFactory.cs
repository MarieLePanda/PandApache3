using pandapache.src.Configuration;
using pandapache.src.ResponseGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.ResponseGeneration
{
    public class FileManagerFactory
    {
        private static IFileManager instance;

        public static void Configure(IFileManager fileManager)
        {
            instance = fileManager;
        }

        public static IFileManager Instance()
        {
            if (instance == null)
            {
                if (ServerConfiguration.Instance.Persistence.Equals("disk"))
                    Configure(new DiskFileManager());
                else if (ServerConfiguration.Instance.Persistence.Equals("cloud"))
                    Configure(new CloudFileManager());
            }
            return instance;
        }
    }
}
