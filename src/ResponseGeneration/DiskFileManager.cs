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
    }
}
