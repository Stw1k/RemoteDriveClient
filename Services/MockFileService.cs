using RemoteDriveClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RemoteDriveClient.Services
{
    public class MockFileService : IFileService
    {
        private readonly string storageDir;
        private readonly Dictionary<string, string> tokens = new Dictionary<string, string>();

        public MockFileService()
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            storageDir = Path.Combine(docs, "RemoteStorageMock");
            Directory.CreateDirectory(storageDir);
        }

        public Task<string> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentException("Username required");
            string token = Guid.NewGuid().ToString();
            tokens[token] = username;
            return Task.FromResult(token);
        }

        public Task<List<FileMetadata>> ListFilesAsync(string token)
        {
            EnsureToken(token);
            var list = new List<FileMetadata>();
            foreach (var file in Directory.GetFiles(storageDir))
            {
                if (file.EndsWith(".meta.json")) continue;
                var fi = new FileInfo(file);
                var meta = LoadMeta(file) ?? new FileMetadata
                {
                    Id = Guid.NewGuid(),
                    Name = fi.Name,
                    Extension = fi.Extension,
                    Size = fi.Length,
                    CreatedAt = fi.CreationTimeUtc,
                    ModifiedAt = fi.LastWriteTimeUtc,
                    UploadedBy = tokens[token],
                    EditedBy = tokens[token],
                    RemotePath = file
                };
                SaveMeta(file, meta);
                list.Add(meta);
            }
            return Task.FromResult(list.OrderBy(f => f.Name).ToList());
        }

        public Task<Stream> GetFileStreamAsync(string token, string remotePath)
        {
            EnsureToken(token);
            if (!File.Exists(remotePath)) throw new FileNotFoundException();
            return Task.FromResult((Stream)new FileStream(remotePath, FileMode.Open, FileAccess.Read));
        }

        public Task<FileMetadata> UploadFileAsync(string token, string localFilePath, string uploadedBy)
        {
            EnsureToken(token);
            string dest = Path.Combine(storageDir, Path.GetFileName(localFilePath));
            File.Copy(localFilePath, dest, true);
            var fi = new FileInfo(dest);
            var meta = new FileMetadata
            {
                Id = Guid.NewGuid(),
                Name = fi.Name,
                Extension = fi.Extension,
                Size = fi.Length,
                CreatedAt = fi.CreationTimeUtc,
                ModifiedAt = fi.LastWriteTimeUtc,
                UploadedBy = uploadedBy,
                EditedBy = uploadedBy,
                RemotePath = dest
            };
            SaveMeta(dest, meta);
            return Task.FromResult(meta);
        }

        public Task<bool> DeleteFileAsync(string token, string remotePath)
        {
            EnsureToken(token);
            if (!File.Exists(remotePath)) return Task.FromResult(false);
            File.Delete(remotePath);
            string meta = remotePath + ".meta.json";
            if (File.Exists(meta)) File.Delete(meta);
            return Task.FromResult(true);
        }

        // Реалізація нових методів
        public Task<FileMetadata> GetFileMetadataAsync(string token, string remotePath)
        {
            EnsureToken(token);
            if (!File.Exists(remotePath)) throw new FileNotFoundException();

            var meta = LoadMeta(remotePath);
            if (meta == null)
            {
                var fi = new FileInfo(remotePath);
                meta = new FileMetadata
                {
                    Id = Guid.NewGuid(),
                    Name = fi.Name,
                    Extension = fi.Extension,
                    Size = fi.Length,
                    CreatedAt = fi.CreationTimeUtc,
                    ModifiedAt = fi.LastWriteTimeUtc,
                    UploadedBy = tokens[token],
                    EditedBy = tokens[token],
                    RemotePath = remotePath
                };
            }

            return Task.FromResult(meta);
        }

        public Task<bool> FileExistsAsync(string token, string remotePath)
        {
            EnsureToken(token);
            return Task.FromResult(File.Exists(remotePath));
        }

        private FileMetadata LoadMeta(string path)
        {
            string metaPath = path + ".meta.json";
            if (!File.Exists(metaPath)) return null;
            try
            {
                string json = File.ReadAllText(metaPath);
                return JsonConvert.DeserializeObject<FileMetadata>(json);
            }
            catch { return null; }
        }

        private void SaveMeta(string path, FileMetadata meta)
        {
            try
            {
                string json = JsonConvert.SerializeObject(meta, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(path + ".meta.json", json);
            }
            catch { }
        }

        private void EnsureToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || !tokens.ContainsKey(token))
                throw new UnauthorizedAccessException();
        }
    }
}