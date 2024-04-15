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

        public void SaveFile(string path, string fileName, string fileData)
        {
            // Écris les données du fichier dans un fichier sur le serveur
           string guidString = Guid.NewGuid().ToString();
           string fullPath = $"{path}{guidString}-{fileName}";

            File.WriteAllText(fullPath, fileData);
            Logger.LogInfo($"File {fileName} saved correclty to {path}");
        }

    }
}
