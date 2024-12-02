using System.Text;

namespace PandApache3.src.Core.ResponseGeneration
{
    public interface IFileManager
    {
        Task<string> ReadAllTextAsync(string path, Encoding encoding);

        public Task<byte[]> ReadAllBytesAsync(string path);


        bool Exists(string path);

        void SaveFile(string path, string fileName, string fileData);
    }
}
