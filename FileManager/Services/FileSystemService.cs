using System;
using System.Collections.Generic;
using System.IO;
using FileManager.Models;

namespace FileManager.Services
{
    /// <summary>
    /// Service class for handling file system operations.
    /// </summary>
    public class FileSystemService
    {
        /// <summary>
        /// Gets the contents of a directory.
        /// </summary>
        /// <param name="path">The path of the directory to get contents from.</param>
        /// <param name="showHiddenFiles">Whether to include hidden files in the results.</param>
        /// <returns>A list of FileSystemItem objects representing the directory contents.</returns>
        public List<FileSystemItem> GetDirectoryContents(string path, bool showHiddenFiles = false)
        {
            var items = new List<FileSystemItem>();

            try
            {
                // Add parent directory
                if (Path.GetPathRoot(path) != path)
                {
                    items.Add(new FileSystemItem
                    {
                        Name = "..",
                        Path = Directory.GetParent(path)?.FullName,
                        Type = "Directory",
                        DateModified = DateTime.Now,
                        Size = 0
                    });
                }

                // Add directories
                foreach (string directory in Directory.GetDirectories(path))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(directory);
                        if (!showHiddenFiles && (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        items.Add(new FileSystemItem
                        {
                            Name = dirInfo.Name,
                            Path = directory,
                            Type = "Directory",
                            DateModified = dirInfo.LastWriteTime,
                            Size = 0
                        });
                    }
                    catch (UnauthorizedAccessException) { }
                }

                // Add files
                foreach (string file in Directory.GetFiles(path))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (!showHiddenFiles && (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        items.Add(new FileSystemItem
                        {
                            Name = fileInfo.Name,
                            Path = file,
                            Type = fileInfo.Extension,
                            DateModified = fileInfo.LastWriteTime,
                            Size = fileInfo.Length
                        });
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException("Access denied to this directory.");
            }

            return items;
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="path">The path where the new directory should be created.</param>
        /// <param name="name">The name of the new directory.</param>
        public void CreateDirectory(string path, string name)
        {
            string newPath = Path.Combine(path, name);
            Directory.CreateDirectory(newPath);
        }

        /// <summary>
        /// Deletes a file or directory.
        /// </summary>
        /// <param name="path">The path of the item to delete.</param>
        public void DeleteItem(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        /// <summary>
        /// Renames a file or directory.
        /// </summary>
        /// <param name="oldPath">The current path of the item.</param>
        /// <param name="newName">The new name for the item.</param>
        public void RenameItem(string oldPath, string newName)
        {
            string directory = Path.GetDirectoryName(oldPath);
            string newPath = Path.Combine(directory, newName);

            if (File.Exists(oldPath))
            {
                File.Move(oldPath, newPath);
            }
            else if (Directory.Exists(oldPath))
            {
                Directory.Move(oldPath, newPath);
            }
        }
    }
} 