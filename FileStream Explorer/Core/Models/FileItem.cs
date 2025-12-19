using System;
using System.IO;

namespace FileStreamExplorer.Core.Models
{
    /// <summary>
    /// Represents a file or directory in the file system
    /// </summary>
    public class FileItem
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsDirectory { get; set; }
        public FileAttributes Attributes { get; set; }

        public string ParentDirectory => Path.GetDirectoryName(FullPath) ?? string.Empty;

        public FileItem(string fullPath)
        {
            FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            Name = Path.GetFileName(fullPath);
            Extension = Path.GetExtension(fullPath);
        }

        public FileItem Clone()
        {
            return new FileItem(FullPath)
            {
                Name = Name,
                Extension = Extension,
                Size = Size,
                CreatedDate = CreatedDate,
                ModifiedDate = ModifiedDate,
                IsDirectory = IsDirectory,
                Attributes = Attributes
            };
        }

        public override string ToString() => FullPath;
    }
}
