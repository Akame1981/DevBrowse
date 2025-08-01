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
using FileManager.ViewModels;
using MaterialDesignThemes.Wpf;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.Linq;

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
        private TabManager tabManager;

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
        /// Gets or sets the tab manager for the application.
        /// </summary>
        public TabManager TabManager
        {
            get => tabManager;
            set
            {
                if (tabManager != value)
                {
                    if (tabManager != null)
                    {
                        tabManager.OnActiveTabChanged -= TabManager_OnActiveTabChanged;
                    }
                    
                    tabManager = value;
                    
                    if (tabManager != null)
                    {
                        tabManager.OnActiveTabChanged += TabManager_OnActiveTabChanged;
                    }
                    
                    OnPropertyChanged();
                }
            }
        }

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
                    if (TabManager?.ActiveTab != null)
                    {
                        LoadDirectoryContents(TabManager.ActiveTab.CurrentPath);
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
            TabManager = new TabManager();
            fileSystemItems = new ObservableCollection<FileSystemItem>();
            FileList.ItemsSource = fileSystemItems;
            
            SetupTreeViewEvents();
            SetupSyntaxHighlighting();
            DataContext = this;
            
            // Set initial view to show drives
            currentPath = "Drives";
            LoadDirectoryContents("Drives");
            
            // Initialize terminal
            TerminalControl.CloseRequested += TerminalControl_CloseRequested;
            
            // Set initial terminal size
            TerminalPanel.Height = 0;
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
                    TabManager.UpdateActiveTabPath(path);
                    LoadDirectoryContents(path);
                }
            }
        }

        private void TabManager_OnActiveTabChanged(object sender, EventArgs e)
        {
            if (TabManager?.ActiveTab != null)
            {
                LoadDirectoryContents(TabManager.ActiveTab.CurrentPath);
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
            
            // Update terminal working directory
            UpdateTerminalWorkingDirectory(path);
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileList.SelectedItem is FileSystemItem selectedItem)
            {
                if (selectedItem.Type == "Drive" || selectedItem.Type == "Directory")
                {
                    string newPath = selectedItem.Path;
                    TabManager.UpdateActiveTabPath(newPath);
                    LoadDirectoryContents(newPath);
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
            if (TabManager?.ActiveTab != null)
            {
                string currentPath = TabManager.ActiveTab.CurrentPath;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    LoadDirectoryContents(currentPath);
                    HiddenFilesIcon.Kind = ShowHiddenFiles ? 
                        MaterialDesignThemes.Wpf.PackIconKind.Eye : 
                        MaterialDesignThemes.Wpf.PackIconKind.EyeOff;
                }
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
                        
                        // Set syntax highlighting with soft theme
                        var highlighting = HighlightingManager.Instance.GetDefinition(GetHighlightingDefinition(extension));
                        if (highlighting != null)
                        {
                            TextViewer.SyntaxHighlighting = highlighting;
                            
                            // Apply soft, muted color scheme
                            var commentColor = highlighting.GetNamedColor("Comment");
                            var stringColor = highlighting.GetNamedColor("String");
                            var keywordColor = highlighting.GetNamedColor("Keyword");
                            var typeColor = highlighting.GetNamedColor("Type");
                            var numberColor = highlighting.GetNamedColor("Number");
                            var methodCallColor = highlighting.GetNamedColor("MethodCall");
                            var propertyColor = highlighting.GetNamedColor("Property");
                            var tagColor = highlighting.GetNamedColor("Tag");
                            var attributeColor = highlighting.GetNamedColor("Attribute");
                            var defaultColor = highlighting.GetNamedColor("Default");

                            // Soft, muted color palette
                            if (commentColor != null) commentColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(92, 99, 112));  // Muted gray
                            if (stringColor != null) stringColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(152, 195, 121));  // Soft green
                            if (keywordColor != null) keywordColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(197, 165, 197));  // Soft purple
                            if (typeColor != null) typeColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(229, 192, 123));  // Soft orange
                            if (numberColor != null) numberColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(209, 154, 102));  // Soft brown
                            if (methodCallColor != null) methodCallColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(97, 175, 239));  // Soft blue
                            if (propertyColor != null) propertyColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(97, 175, 239));  // Soft blue
                            if (tagColor != null) tagColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(224, 108, 117));  // Soft red
                            if (attributeColor != null) attributeColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(229, 192, 123));  // Soft orange
                            if (defaultColor != null) defaultColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(171, 178, 191));  // Soft white
                        }
                        
                        // Set dark background
                        TextViewer.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(40, 44, 52));  // Dark background
                        
                        // Ensure line numbers are visible but subtle
                        TextViewer.ShowLineNumbers = true;
                        TextViewer.LineNumbersForeground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(92, 99, 112));  // Muted gray for line numbers
                    }
                    else
                    {
                        TextViewer.Text = "Preview not available for this file type.";
                        TextViewer.SyntaxHighlighting = null;
                    }
                }
                catch (Exception ex)
                {
                    TextViewer.Text = $"Error reading file: {ex.Message}";
                    TextViewer.SyntaxHighlighting = null;
                }
            }
            else
            {
                TextViewer.Text = string.Empty;
                TextViewer.SyntaxHighlighting = null;
            }
        }

        private string GetHighlightingDefinition(string extension)
        {
            switch (extension)
            {
                case ".cs":
                    return "C#";
                case ".xaml":
                case ".xml":
                    return "XML";
                case ".json":
                case ".js":
                    return "JavaScript";
                case ".html":
                    return "HTML";
                case ".css":
                    return "CSS";
                default:
                    return null;
            }
        }

        private bool IsTextFile(string extension)
        {
            return textExtensions.Contains(extension);
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            TabManager.AddNewTab("This PC", "Drives");
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Tab tab)
            {
                TabManager.CloseTab(tab);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabManager?.ActiveTab != null)
            {
                LoadDirectoryContents(TabManager.ActiveTab.CurrentPath);
            }
        }

        private void SetupSyntaxHighlighting()
        {
            // This method is no longer needed as we're applying colors directly in FileList_SelectionChanged
            // Keeping it as a placeholder in case we need it in the future
        }

        private void ToggleTerminalButton_Click(object sender, RoutedEventArgs e)
        {
            if (TerminalPanel.Visibility == Visibility.Visible)
            {
                TerminalPanel.Visibility = Visibility.Collapsed;
                TerminalPanel.Height = 0;
            }
            else
            {
                TerminalPanel.Visibility = Visibility.Visible;
                TerminalPanel.Height = 200;
                
                // Initialize terminal with current directory
                string currentPath = TabManager?.ActiveTab?.CurrentPath;
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    TerminalControl.Initialize(currentPath);
                }
                else
                {
                    TerminalControl.Initialize();
                }
                
                // Focus the terminal input when shown
                TerminalControl.Focus();
            }
        }

        private void TerminalControl_CloseRequested(object sender, EventArgs e)
        {
            TerminalPanel.Visibility = Visibility.Collapsed;
            TerminalPanel.Height = 0;
        }

        private void UpdateTerminalWorkingDirectory(string path)
        {
            if (TerminalPanel.Visibility == Visibility.Visible && Directory.Exists(path))
            {
                TerminalControl.SetWorkingDirectory(path);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                // If search box is empty, show all files
                if (TabManager?.ActiveTab != null)
                {
                    LoadDirectoryContents(TabManager.ActiveTab.CurrentPath);
                }
                return;
            }

            FilterFiles(SearchTextBox.Text);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                FilterFiles(SearchTextBox.Text);
            }
        }

        private void FilterFiles(string searchText)
        {
            if (TabManager?.ActiveTab == null) return;

            string currentPath = TabManager.ActiveTab.CurrentPath;
            if (string.IsNullOrEmpty(currentPath)) return;

            try
            {
                var items = fileSystemService.GetDirectoryContents(currentPath, ShowHiddenFiles);
                var filteredItems = items.Where(item => 
                    item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

                fileSystemItems.Clear();
                foreach (var item in filteredItems)
                {
                    fileSystemItems.Add(item);
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show("Access denied to this directory.", "Access Denied", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
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