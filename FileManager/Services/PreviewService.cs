using System;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace FileManager.Services
{
    /// <summary>
    /// Service class for handling file preview operations.
    /// </summary>
    public class PreviewService
    {
        private readonly string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        private readonly string[] textExtensions = { ".txt", ".cs", ".xaml", ".xml", ".json", ".html", ".css", ".js" };

        /// <summary>
        /// Gets a preview of the specified file.
        /// </summary>
        /// <param name="filePath">The path of the file to preview.</param>
        /// <returns>A string containing the preview content or null if preview is not available.</returns>
        public string GetPreview(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            string extension = Path.GetExtension(filePath).ToLower();

            if (IsImageFile(extension))
            {
                return "Image preview available";
            }
            else if (IsTextFile(extension))
            {
                try
                {
                    return File.ReadAllText(filePath, Encoding.UTF8);
                }
                catch (Exception)
                {
                    return "Unable to read file";
                }
            }

            return "Preview not available for this file type";
        }

        /// <summary>
        /// Determines if a file is an image based on its extension.
        /// </summary>
        /// <param name="extension">The file extension to check.</param>
        /// <returns>True if the file is an image; otherwise, false.</returns>
        public bool IsImageFile(string extension)
        {
            return Array.Exists(imageExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if a file is a text file based on its extension.
        /// </summary>
        /// <param name="extension">The file extension to check.</param>
        /// <returns>True if the file is a text file; otherwise, false.</returns>
        public bool IsTextFile(string extension)
        {
            return Array.Exists(textExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a bitmap image from a file path.
        /// </summary>
        /// <param name="filePath">The path of the image file.</param>
        /// <returns>A BitmapImage object or null if the file is not an image or cannot be loaded.</returns>
        public BitmapImage GetImagePreview(string filePath)
        {
            if (!IsImageFile(Path.GetExtension(filePath)))
                return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(filePath);
                image.EndInit();
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
} 