using RemoteDriveClient.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RemoteDriveClient.Services
{
    public interface IFileService
    {
        Task<string> AuthenticateAsync(string username, string password);
        Task<List<FileMetadata>> ListFilesAsync(string token);
        Task<Stream> GetFileStreamAsync(string token, string remotePath);
        Task<FileMetadata> UploadFileAsync(string token, string localFilePath, string uploadedBy);
        Task<bool> DeleteFileAsync(string token, string remotePath);

        Task<FileMetadata> GetFileMetadataAsync(string token, string remotePath);
        Task<bool> FileExistsAsync(string token, string remotePath);
    }
}
