﻿using System.Text;

namespace pandapache.src.ResponseGeneration
{
    public interface IFileManager
    {
        Task<string> ReadAllTextAsync(string path, Encoding encoding);

        public Task<byte[]> ReadAllBytesAsync(string path);


        bool Exists(string path);

        bool SaveFile(string fileName, string fileData);
    }
}
