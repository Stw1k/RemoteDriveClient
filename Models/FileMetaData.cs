using System;

namespace RemoteDriveClient.Models
{
    public class FileMetadata
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string UploadedBy { get; set; }
        public string EditedBy { get; set; }
        public string RemotePath { get; set; }
    }
}
