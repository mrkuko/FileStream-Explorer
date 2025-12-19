using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileStreamExplorer.Infrastructure.Services
{
    /// <summary>
    /// Implementation of file system operations
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        public async Task<IEnumerable<FileItem>> GetFilesAsync(string directory, bool includeSubdirectories = false)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(directory))
                    return Enumerable.Empty<FileItem>();

                var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = new List<FileItem>();

                try
                {
                    // Get files
                    var filePaths = Directory.GetFiles(directory, "*", searchOption);
                    foreach (var filePath in filePaths)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(filePath);
                            files.Add(CreateFileItem(fileInfo));
                        }
                        catch
                        {
                            // Skip files we can't access
                        }
                    }

                    // Get directories
                    var dirPaths = Directory.GetDirectories(directory, "*", searchOption);
                    foreach (var dirPath in dirPaths)
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(dirPath);
                            files.Add(CreateFileItem(dirInfo));
                        }
                        catch
                        {
                            // Skip directories we can't access
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Handle access denied
                }

                return files;
            });
        }

        public async Task<FileItem?> GetFileInfoAsync(string path)
        {
            return await Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    return CreateFileItem(fileInfo);
                }
                else if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    return CreateFileItem(dirInfo);
                }
                
                return null;
            });
        }

        public bool Exists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        public async Task<bool> RenameFileAsync(string sourcePath, string newName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var directory = Path.GetDirectoryName(sourcePath);
                    var newPath = Path.Combine(directory ?? string.Empty, newName);

                    if (File.Exists(sourcePath))
                    {
                        File.Move(sourcePath, newPath);
                        return true;
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        Directory.Move(sourcePath, newPath);
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> MoveFileAsync(string sourcePath, string destinationPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Ensure destination directory exists
                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    if (File.Exists(sourcePath))
                    {
                        File.Move(sourcePath, destinationPath, overwrite: false);
                        return true;
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        Directory.Move(sourcePath, destinationPath);
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> DeleteFileAsync(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        return true;
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.Delete(path, recursive: true);
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> CreateDirectoryAsync(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        private FileItem CreateFileItem(FileInfo fileInfo)
        {
            return new FileItem(fileInfo.FullName)
            {
                Name = fileInfo.Name,
                Extension = fileInfo.Extension,
                Size = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                IsDirectory = false,
                Attributes = fileInfo.Attributes
            };
        }

        private FileItem CreateFileItem(DirectoryInfo dirInfo)
        {
            return new FileItem(dirInfo.FullName)
            {
                Name = dirInfo.Name,
                Extension = string.Empty,
                Size = 0,
                CreatedDate = dirInfo.CreationTime,
                ModifiedDate = dirInfo.LastWriteTime,
                IsDirectory = true,
                Attributes = dirInfo.Attributes
            };
        }
    }
}
