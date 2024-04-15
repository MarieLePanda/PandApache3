using pandapache.src.LoggingAndMonitoring;
using System.Text;

namespace pandapache.src.ResponseGeneration
{
    public class DiskFileManager : IFileManager
    {

        public async Task<string> ReadAllTextAsync(string path, Encoding encoding)
        {
            return await File.ReadAllTextAsync(path, encoding);
        }

        public async Task<byte[]> ReadAllBytesAsync(string path)
        {
            return await File.ReadAllBytesAsync(path);
        }
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public bool SaveFile(string fileName, string fileData)
        {
            // Écris les données du fichier dans un fichier sur le serveur
            try
            {
                string guidString = Guid.NewGuid().ToString();
                fileName = $"{guidString}-{fileName}";

                File.WriteAllText(fileName, fileData);
                Logger.LogInfo($"File {fileName} saved correclty");

                return true;
            }
            catch(Exception ex)
            {
                Logger.LogError($"Error during file saving: {ex.Message}");

                return false;
            }
        }

    }
}
