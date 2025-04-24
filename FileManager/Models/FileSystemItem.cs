using System;

namespace FileManager.Models
{
    /// <summary>
    /// Represents a file system item (file or directory) in the file manager.
    /// </summary>
    public class FileSystemItem
    {
        /// <summary>
        /// Gets or sets the name of the file system item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the full path of the file system item.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the type of the file system item (e.g., "File", "Directory").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the item was last modified.
        /// </summary>
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Gets or sets the size of the file in bytes. For directories, this is typically 0.
        /// </summary>
        public long Size { get; set; }
    }
} 