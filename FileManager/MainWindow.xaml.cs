using System;
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
using FileManager.Models;
using FileManager.Services;
using MaterialDesignThemes.Wpf;

namespace FileManager
{
    /// <summary>
    /// Main window of the File Manager application.
    /// Provides functionality for browsing, viewing, and managing files and directories.
    /// </summary>
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

    /// <summary>
    /// Main window of the File Manager application.
    /// Provides functionality for browsing, viewing, and managing files and directories.
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Current directory being displayed in the file manager.
        /// </summary>
        private string currentPath;
        private ObservableCollection<FileSystemItem> fileSystemItems;
        private bool showHiddenFiles;
        private string clipboardPath;
        private bool isCutOperation;
        private readonly FileSystemService fileSystemService;

        /// <summary>
        /// List of supported image file extensions.
        /// </summary>
        private readonly string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        /// <summary>
        /// List of supported text file extensions.
        /// </summary>
        private readonly string[] textExtensions = { ".txt", ".cs", ".xaml", ".xml", ".json", ".html", ".css", ".js" };

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether hidden files should be shown.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            fileSystemService = new FileSystemService();
            fileSystemItems = new ObservableCollection<FileSystemItem>();
            FileList.ItemsSource = fileSystemItems;
            
            SetupTreeViewEvents();
            DataContext = this;
            
            // Set initial view to show drives
            currentPath = "Drives";
            LoadDirectoryContents("Drives");
        }

        private void SetupTreeViewEvents()
        {
            NavigationTree.SelectedItemChanged += NavigationTree_SelectedItemChanged;
            
            // Add This PC item to the tree
            var thisPC = new TreeViewItem 
            { 
                Header = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Children =
                    {
                        new MaterialDesignThemes.Wpf.PackIcon
                        {
                            Kind = MaterialDesignThemes.Wpf.PackIconKind.DesktopClassic,
                            Width = 20,
                            Height = 20,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        },
                        new TextBlock
                        {
                            Text = "This PC",
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                },
                Tag = "Drives"
            };
            NavigationTree.Items.Add(thisPC);
            
            // Load drives under This PC
            LoadDrivesIntoTree(thisPC);
        }

        private void LoadDrivesIntoTree(TreeViewItem parentItem)
        {
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        var driveItem = new TreeViewItem 
                        { 
                            Header = new StackPanel
                            {
                                Orientation = System.Windows.Controls.Orientation.Horizontal,
                                Children =
                                {
                                    new MaterialDesignThemes.Wpf.PackIcon
                                    {
                                        Kind = MaterialDesignThemes.Wpf.PackIconKind.Harddisk,
                                        Width = 20,
                                        Height = 20,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Margin = new Thickness(0, 0, 5, 0)
                                    },
                                    new TextBlock
                                    {
                                        Text = $"{drive.Name} ({drive.VolumeLabel})",
                                        VerticalAlignment = VerticalAlignment.Center
                                    }
                                }
                            },
                            Tag = drive.RootDirectory.FullName
                        };
                        parentItem.Items.Add(driveItem);
                        LoadSubDirectories(driveItem);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading drives: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                            Header = new StackPanel
                            {
                                Orientation = System.Windows.Controls.Orientation.Horizontal,
                                Children =
                                {
                                    new MaterialDesignThemes.Wpf.PackIcon
                                    {
                                        Kind = MaterialDesignThemes.Wpf.PackIconKind.Folder,
                                        Width = 20,
                                        Height = 20,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Margin = new Thickness(0, 0, 5, 0)
                                    },
                                    new TextBlock
                                    {
                                        Text = dirInfo.Name,
                                        VerticalAlignment = VerticalAlignment.Center
                                    }
                                }
                            },
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

        /// <summary>
        /// Loads and displays the contents of the specified directory.
        /// </summary>
        /// <param name="path">The path of the directory to load.</param>
        private void LoadDirectoryContents(string path)
        {
            fileSystemItems.Clear();
            
            if (path == "Drives")
            {
                // Add all drives
                try
                {
                    foreach (DriveInfo drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                        {
                            fileSystemItems.Add(new FileSystemItem
                            {
                                Name = $"{drive.Name} ({drive.VolumeLabel})",
                                Path = drive.RootDirectory.FullName,
                                Type = "Drive",
                                DateModified = drive.RootDirectory.LastWriteTime,
                                Size = drive.TotalSize
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error loading drives: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                return;
            }

            try
            {
                var items = fileSystemService.GetDirectoryContents(path, ShowHiddenFiles);
                foreach (var item in items)
                {
                    fileSystemItems.Add(item);
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show("Access denied to this directory.", "Access Denied", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                if (selectedItem.Type == "Drive" || selectedItem.Type == "Directory")
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
                        System.Windows.MessageBox.Show($"Error opening file: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                var dialog = new Microsoft.Win32.OpenFileDialog
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
                        System.Windows.MessageBox.Show($"Error opening file: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                    System.Windows.MessageBox.Show($"Error during paste operation: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                        System.Windows.MessageBox.Show($"Error renaming: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ContextMenu_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete '{selectedItem.Name}'?",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
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
                        System.Windows.MessageBox.Show($"Error deleting: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                    System.Windows.MessageBox.Show($"Error creating folder: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                    System.Windows.MessageBox.Show($"Error creating file: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                    System.Windows.MessageBox.Show($"Error opening file: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            return textExtensions.Contains(extension);
        }
    }

    /// <summary>
    /// Represents a file system item in the file manager.
    /// </summary>
    public class FileSystemItem
    {
        /// <summary>
        /// Gets or sets the name of the file system item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the path of the file system item.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the type of the file system item.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the date and time the file system item was last modified.
        /// </summary>
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Gets or sets the size of the file system item.
        /// </summary>
        public long Size { get; set; }
    }
} 