using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Core.ResponseGeneration
{
    public class CloudFileManager : IFileManager
    {
        public bool Exists(string path)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ReadAllBytesAsync(string path)
        {
            throw new NotImplementedException();
        }

        public Task<string> ReadAllTextAsync(string path, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        public void SaveFile(string path, string fileName, string fileData)
        {
            throw new NotImplementedException();
        }
    }
}
