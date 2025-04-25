using System.Windows;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Security.Principal;
using System.Diagnostics;

namespace FileManager
{
    public partial class PropertiesWindow : Window, INotifyPropertyChanged
    {
        private readonly FileSystemItem item;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ItemName => item.Name;
        public string ItemPath => item.Path;
        public string ItemType => FormatType(item.Type);
        public string ItemSize => FormatSize(item.Size);
        public string CreatedDate => GetCreatedDate();
        public string ModifiedDate => FormatDate(item.DateModified);
        public string LastAccessedDate => GetLastAccessedDate();
        public string FileExtension => GetFileExtension();
        public string ItemCount => GetItemCount();
        public string FileOwner => GetFileOwner();
        public string FileVersion => GetFileVersion();
        public string Attributes => GetAttributes();

        public PropertiesWindow(FileSystemItem item)
        {
            InitializeComponent();
            this.item = item;
            DataContext = this;
        }

        private string FormatType(string type)
        {
            if (type == "Directory") return "Folder";
            if (string.IsNullOrEmpty(type)) return "File";
            return $"{type.TrimStart('.')} File";
        }

        private string FormatSize(long size)
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

        private string FormatDate(DateTime date)
        {
            return date.ToString("MMMM dd, yyyy h:mm tt");
        }

        private string GetCreatedDate()
        {
            try
            {
                if (File.Exists(item.Path))
                {
                    return FormatDate(File.GetCreationTime(item.Path));
                }
                else if (Directory.Exists(item.Path))
                {
                    return FormatDate(Directory.GetCreationTime(item.Path));
                }
            }
            catch { }
            return "Unknown";
        }

        private string GetLastAccessedDate()
        {
            try
            {
                if (File.Exists(item.Path))
                {
                    return FormatDate(File.GetLastAccessTime(item.Path));
                }
                else if (Directory.Exists(item.Path))
                {
                    return FormatDate(Directory.GetLastAccessTime(item.Path));
                }
            }
            catch { }
            return "Unknown";
        }

        private string GetFileExtension()
        {
            if (File.Exists(item.Path))
            {
                return Path.GetExtension(item.Path).ToUpper();
            }
            return "N/A";
        }

        private string GetItemCount()
        {
            if (Directory.Exists(item.Path))
            {
                try
                {
                    int fileCount = Directory.GetFiles(item.Path).Length;
                    int dirCount = Directory.GetDirectories(item.Path).Length;
                    return $"{fileCount + dirCount} items ({fileCount} files, {dirCount} folders)";
                }
                catch { }
            }
            return "N/A";
        }

        private string GetFileOwner()
        {
            try
            {
                if (File.Exists(item.Path))
                {
                    var fileInfo = new FileInfo(item.Path);
                    var security = fileInfo.GetAccessControl();
                    var owner = security.GetOwner(typeof(NTAccount));
                    return owner?.Value ?? "Unknown";
                }
                else if (Directory.Exists(item.Path))
                {
                    var dirInfo = new DirectoryInfo(item.Path);
                    var security = dirInfo.GetAccessControl();
                    var owner = security.GetOwner(typeof(NTAccount));
                    return owner?.Value ?? "Unknown";
                }
            }
            catch { }
            return "Unknown";
        }

        private string GetFileVersion()
        {
            if (File.Exists(item.Path) && (item.Path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || 
                                          item.Path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(item.Path);
                    return versionInfo.FileVersion ?? "N/A";
                }
                catch { }
            }
            return "N/A";
        }

        private string GetAttributes()
        {
            try
            {
                var attributes = new StringBuilder();
                if (File.Exists(item.Path))
                {
                    var fileInfo = new FileInfo(item.Path);
                    if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        attributes.Append("Read-only, ");
                    if ((fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        attributes.Append("Hidden, ");
                    if ((fileInfo.Attributes & FileAttributes.Archive) == FileAttributes.Archive)
                        attributes.Append("Archive, ");
                    if ((fileInfo.Attributes & FileAttributes.System) == FileAttributes.System)
                        attributes.Append("System, ");
                }
                else if (Directory.Exists(item.Path))
                {
                    var dirInfo = new DirectoryInfo(item.Path);
                    if ((dirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        attributes.Append("Read-only, ");
                    if ((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        attributes.Append("Hidden, ");
                    if ((dirInfo.Attributes & FileAttributes.System) == FileAttributes.System)
                        attributes.Append("System, ");
                }

                string result = attributes.ToString().TrimEnd(',', ' ');
                return string.IsNullOrEmpty(result) ? "Normal" : result;
            }
            catch
            {
                return "Access Denied";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 