using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using System.Text;
using System.Collections.Generic;

namespace FileManager
{
    public class FileSizeConverter : IValueConverter
    {
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string currentPath;
        private ObservableCollection<FileSystemItem> fileSystemItems;
        private bool showHiddenFiles;
        private string clipboardPath;
        private bool isCutOperation;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ShowHiddenFiles
        {
            get => showHiddenFiles;
            set
            {
                if (showHiddenFiles != value)
                {
                    showHiddenFiles = value;
                    OnPropertyChanged();
                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        LoadDirectoryContents(currentPath);
                    }
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            fileSystemItems = new ObservableCollection<FileSystemItem>();
            FileList.ItemsSource = fileSystemItems;
            LoadDrives();
            SetupTreeViewEvents();
            DataContext = this;
        }

        private void SetupTreeViewEvents()
        {
            NavigationTree.SelectedItemChanged += NavigationTree_SelectedItemChanged;
        }

        private void LoadDrives()
        {
            NavigationTree.Items.Clear();
            var thisPC = new TreeViewItem { Header = "This PC" };
            NavigationTree.Items.Add(thisPC);

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                var driveItem = new TreeViewItem 
                { 
                    Header = $"{drive.Name} ({drive.VolumeLabel})",
                    Tag = drive.RootDirectory.FullName
                };
                thisPC.Items.Add(driveItem);
                LoadSubDirectories(driveItem);
            }
        }

        private void LoadSubDirectories(TreeViewItem parentItem)
        {
            try
            {
                string path = parentItem.Tag as string;
                if (string.IsNullOrEmpty(path)) return;

                foreach (string directory in Directory.GetDirectories(path))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(directory);
                        if (!ShowHiddenFiles && (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        var item = new TreeViewItem
                        {
                            Header = dirInfo.Name,
                            Tag = directory
                        };
                        parentItem.Items.Add(item);
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        private void NavigationTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem)
            {
                string path = selectedItem.Tag as string;
                if (!string.IsNullOrEmpty(path))
                {
                    currentPath = path;
                    LoadDirectoryContents(path);
                }
            }
        }

        private void LoadDirectoryContents(string path)
        {
            fileSystemItems.Clear();
            try
            {
                // Add parent directory
                if (Path.GetPathRoot(path) != path)
                {
                    fileSystemItems.Add(new FileSystemItem
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
                        if (!ShowHiddenFiles && (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        fileSystemItems.Add(new FileSystemItem
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
                        if (!ShowHiddenFiles && (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        fileSystemItems.Add(new FileSystemItem
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
                MessageBox.Show("Access denied to this directory.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                if (selectedItem.Type == "Directory")
                {
                    LoadDirectoryContents(selectedItem.Path);
                }
                else
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = selectedItem.Path,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ToggleHiddenFiles_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                LoadDirectoryContents(currentPath);
            }
        }

        private void OpenTaskManager_Click(object sender, RoutedEventArgs e)
        {
            var taskManager = new TaskManagerWindow();
            taskManager.Owner = this;
            taskManager.ShowDialog();
        }

        private void FileList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (FileList.SelectedItem == null)
            {
                e.Handled = true;
            }
        }

        private void ContextMenu_Open_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                OpenItem(selectedItem);
            }
        }

        private void ContextMenu_OpenWith_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem && selectedItem.Type != "Directory")
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Program",
                    Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = dialog.FileName,
                            Arguments = $"\"{selectedItem.Path}\"",
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ContextMenu_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                clipboardPath = selectedItem.Path;
                isCutOperation = false;
            }
        }

        private void ContextMenu_Cut_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                clipboardPath = selectedItem.Path;
                isCutOperation = true;
            }
        }

        private void ContextMenu_Paste_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(clipboardPath) && !string.IsNullOrEmpty(currentPath))
            {
                try
                {
                    string destinationPath = Path.Combine(currentPath, Path.GetFileName(clipboardPath));
                    if (isCutOperation)
                    {
                        if (File.Exists(clipboardPath))
                        {
                            File.Move(clipboardPath, destinationPath);
                        }
                        else if (Directory.Exists(clipboardPath))
                        {
                            Directory.Move(clipboardPath, destinationPath);
                        }
                    }
                    else
                    {
                        if (File.Exists(clipboardPath))
                        {
                            File.Copy(clipboardPath, destinationPath);
                        }
                        else if (Directory.Exists(clipboardPath))
                        {
                            CopyDirectory(clipboardPath, destinationPath);
                        }
                    }
                    LoadDirectoryContents(currentPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during paste operation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }

        private void ContextMenu_Rename_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                var dialog = new InputDialog("Rename", "Enter new name:", selectedItem.Name);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        string newPath = Path.Combine(Path.GetDirectoryName(selectedItem.Path), dialog.ResponseText);
                        if (File.Exists(selectedItem.Path))
                        {
                            File.Move(selectedItem.Path, newPath);
                        }
                        else if (Directory.Exists(selectedItem.Path))
                        {
                            Directory.Move(selectedItem.Path, newPath);
                        }
                        LoadDirectoryContents(currentPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ContextMenu_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{selectedItem.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (File.Exists(selectedItem.Path))
                        {
                            File.Delete(selectedItem.Path);
                        }
                        else if (Directory.Exists(selectedItem.Path))
                        {
                            Directory.Delete(selectedItem.Path, true);
                        }
                        LoadDirectoryContents(currentPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ContextMenu_Properties_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                var properties = new PropertiesWindow(selectedItem);
                properties.Owner = this;
                properties.ShowDialog();
            }
        }

        private void ContextMenu_NewFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("New Folder", "Enter folder name:", "New Folder");
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string newFolderPath = Path.Combine(currentPath, dialog.ResponseText);
                    Directory.CreateDirectory(newFolderPath);
                    LoadDirectoryContents(currentPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ContextMenu_NewTextFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("New Text File", "Enter file name:", "New Text File.txt");
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string newFilePath = Path.Combine(currentPath, dialog.ResponseText);
                    File.WriteAllText(newFilePath, string.Empty);
                    LoadDirectoryContents(currentPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenItem(FileSystemItem item)
        {
            if (item.Type == "Directory")
            {
                LoadDirectoryContents(item.Path);
            }
            else
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = item.Path,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem && selectedItem.Type != "Directory")
            {
                try
                {
                    string extension = Path.GetExtension(selectedItem.Path).ToLower();
                    if (IsTextFile(extension))
                    {
                        string content = File.ReadAllText(selectedItem.Path);
                        TextViewer.Text = content;
                    }
                    else
                    {
                        TextViewer.Text = "Preview not available for this file type.";
                    }
                }
                catch (Exception ex)
                {
                    TextViewer.Text = $"Error reading file: {ex.Message}";
                }
            }
            else
            {
                TextViewer.Text = string.Empty;
            }
        }

        private bool IsTextFile(string extension)
        {
            string[] textExtensions = { ".txt", ".cs", ".xaml", ".xml", ".json", ".html", ".css", ".js", ".md", ".log", ".ini", ".config" };
            return textExtensions.Contains(extension);
        }
    }

    public class FileSystemItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public DateTime DateModified { get; set; }
        public long Size { get; set; }
    }
} 