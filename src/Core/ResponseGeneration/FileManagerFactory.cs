
using PandApache3.src.Core.Configuration;

namespace PandApache3.src.Core.ResponseGeneration
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
