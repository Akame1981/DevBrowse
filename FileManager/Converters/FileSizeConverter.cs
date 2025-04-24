using System;
using System.Globalization;
using System.Windows.Data;

namespace FileManager.Converters
{
    /// <summary>
    /// Converts file sizes from bytes to human-readable format (B, KB, MB, GB, TB).
    /// </summary>
    public class FileSizeConverter : IValueConverter
    {
        /// <summary>
        /// Converts a file size in bytes to a human-readable string.
        /// </summary>
        /// <param name="value">The file size in bytes.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>A string representing the file size in appropriate units.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long size)
            {
                if (size == 0) return "Folder";
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                while (size >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    size = size / 1024;
                }
                return $"{size:0.##} {sizes[order]}";
            }
            return value;
        }

        /// <summary>
        /// Converts a human-readable file size string back to bytes (not implemented).
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 