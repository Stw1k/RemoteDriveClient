using RemoteDriveClient.Models;
using RemoteDriveClient.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteDriveClient.Controllers
{
    public class SyncController
    {
        private readonly IFileService fileService;
        private readonly string token;
        private readonly string username;

        public event EventHandler<string> ProgressChanged;

        public SyncController(IFileService svc, string token, string username)
        {
            this.fileService = svc;
            this.token = token;
            this.username = username;
        }

        
        public async Task<List<FileMetadata>> GetLocalFilesAsync(string localFolder)
        {
            if (!Directory.Exists(localFolder))
                throw new DirectoryNotFoundException();

            var files = Directory.GetFiles(localFolder)
                .Select(f => new FileInfo(f))
                .Select(fi => new FileMetadata
                {
                    Name = fi.Name,
                    Extension = fi.Extension,
                    Size = fi.Length,
                    CreatedAt = fi.CreationTimeUtc,
                    ModifiedAt = fi.LastWriteTimeUtc
                })
                .ToList();

            return await Task.FromResult(files);
        }

        
        public async Task<List<FileMetadata>> GetRemoteFilesAsync()
        {
            return await fileService.ListFilesAsync(token);
        }

        
        public async Task<bool> NeedsSync(FileMetadata local, FileMetadata remote)
        {
            return await Task.FromResult(
                local.ModifiedAt > remote.ModifiedAt.AddSeconds(1) ||
                remote.ModifiedAt > local.ModifiedAt.AddSeconds(1)
            );
        }

        public async Task<string> SyncFolderAsync(string localFolder)
        {
            if (!Directory.Exists(localFolder)) throw new DirectoryNotFoundException();

            OnProgress("Starting synchronization...");

            var remoteFiles = await fileService.ListFilesAsync(token);
            var localFiles = Directory.GetFiles(localFolder).Select(f => new FileInfo(f)).ToList();

            var remoteByName = remoteFiles.ToDictionary(r => r.Name, r => r);
            var localByName = localFiles.ToDictionary(l => l.Name, l => l);

            int uploaded = 0, downloaded = 0;

            foreach (var lf in localFiles)
            {
                OnProgress("Checking " + lf.Name);
                if (!remoteByName.ContainsKey(lf.Name))
                {
                    await fileService.UploadFileAsync(token, lf.FullName, username);
                    uploaded++;
                    OnProgress($"Uploaded new file: {lf.Name}");
                }
                else
                {
                    var rf = remoteByName[lf.Name];
                    if (lf.LastWriteTimeUtc > rf.ModifiedAt.AddSeconds(1))
                    {
                        await fileService.UploadFileAsync(token, lf.FullName, username);
                        uploaded++;
                        OnProgress($"Updated file: {lf.Name}");
                    }
                    else if (rf.ModifiedAt > lf.LastWriteTimeUtc.AddSeconds(1))
                    {
                        await DownloadFileAsync(localFolder, rf);
                        downloaded++;
                        OnProgress($"Downloaded file: {rf.Name}");
                    }
                }
            }

            foreach (var rf in remoteFiles)
            {
                if (!localByName.ContainsKey(rf.Name))
                {
                    await DownloadFileAsync(localFolder, rf);
                    downloaded++;
                    OnProgress($"Downloaded new file: {rf.Name}");
                }
            }

            string result = $"Synchronization complete. Uploaded: {uploaded}, Downloaded: {downloaded}";
            OnProgress(result);
            return result;
        }

        
        private async Task DownloadFileAsync(string localFolder, FileMetadata remoteFile)
        {
            Stream s = await fileService.GetFileStreamAsync(token, remoteFile.RemotePath);
            using (var fs = new FileStream(Path.Combine(localFolder, remoteFile.Name), FileMode.Create, FileAccess.Write))
            {
                await s.CopyToAsync(fs);
            }
            s.Dispose();

            var localPath = Path.Combine(localFolder, remoteFile.Name);
            if (File.Exists(localPath))
            {
                File.SetLastWriteTimeUtc(localPath, remoteFile.ModifiedAt);
            }
        }

        
        public async Task<int> GetSyncStatus(string localFolder)
        {
            if (!Directory.Exists(localFolder)) return -1;

            var remoteFiles = await fileService.ListFilesAsync(token);
            var localFiles = Directory.GetFiles(localFolder).Select(f => new FileInfo(f)).ToList();

            int outOfSyncCount = 0;

            foreach (var localFile in localFiles)
            {
                var remoteFile = remoteFiles.FirstOrDefault(r => r.Name == localFile.Name);
                if (remoteFile != null)
                {
                    if (Math.Abs((localFile.LastWriteTimeUtc - remoteFile.ModifiedAt).TotalSeconds) > 1)
                    {
                        outOfSyncCount++;
                    }
                }
            }

            return outOfSyncCount;
        }

        private void OnProgress(string msg)
        {
            ProgressChanged?.Invoke(this, msg);
        }
    }
}
