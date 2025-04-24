using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Windows.Forms;
using FileManager.Models;
using FileManager.Services;

namespace FileManager.ViewModels
{
    /// <summary>
    /// Main ViewModel for the File Manager application.
    /// Handles file system operations and UI state.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly FileSystemService fileSystemService;
        private readonly NavigationService navigationService;
        private readonly PreviewService previewService;
        private bool showHiddenFiles;
        private string clipboardPath;
        private bool isCutOperation;
        private ObservableCollection<FileSystemItem> fileSystemItems;
        private FileSystemItem selectedItem;
        private string previewContent;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            fileSystemService = new FileSystemService();
            navigationService = new NavigationService();
            previewService = new PreviewService();
            fileSystemItems = new ObservableCollection<FileSystemItem>();
            ShowHiddenFiles = false;
            
            // Initialize commands
            NavigateBackCommand = new RelayCommand(ExecuteNavigateBack, () => navigationService.CanNavigateBack);
            NavigateForwardCommand = new RelayCommand(ExecuteNavigateForward, () => navigationService.CanNavigateForward);
            NavigateUpCommand = new RelayCommand(ExecuteNavigateUp);
            NavigateHomeCommand = new RelayCommand(ExecuteNavigateHome);
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            OpenFileCommand = new RelayCommand(ExecuteOpenFile);
            OpenFolderCommand = new RelayCommand(ExecuteOpenFolder);
            SearchCommand = new RelayCommand(ExecuteSearch);
            CreateNewFolderCommand = new RelayCommand(ExecuteCreateNewFolder);
            DeleteItemCommand = new RelayCommand(ExecuteDeleteItem, () => SelectedItem != null);
            RenameItemCommand = new RelayCommand(ExecuteRenameItem, () => SelectedItem != null);
            CopyItemCommand = new RelayCommand(ExecuteCopyItem, () => SelectedItem != null);
            CutItemCommand = new RelayCommand(ExecuteCutItem, () => SelectedItem != null);
            PasteItemCommand = new RelayCommand(ExecutePasteItem, () => !string.IsNullOrEmpty(clipboardPath));

            LoadDirectoryContents();
        }

        /// <summary>
        /// Gets or sets the current directory path.
        /// </summary>
        public string CurrentPath
        {
            get => navigationService.CurrentPath;
            set
            {
                if (navigationService.NavigateTo(value))
                {
                    OnPropertyChanged();
                    LoadDirectoryContents();
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
                    LoadDirectoryContents();
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected file system item.
        /// </summary>
        public FileSystemItem SelectedItem
        {
            get => selectedItem;
            set
            {
                if (selectedItem != value)
                {
                    selectedItem = value;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        /// <summary>
        /// Gets the preview content for the selected item.
        /// </summary>
        public string PreviewContent
        {
            get => previewContent;
            private set
            {
                previewContent = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the collection of file system items in the current directory.
        /// </summary>
        public ObservableCollection<FileSystemItem> FileSystemItems
        {
            get => fileSystemItems;
            private set
            {
                fileSystemItems = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand NavigateBackCommand { get; }
        public ICommand NavigateForwardCommand { get; }
        public ICommand NavigateUpCommand { get; }
        public ICommand NavigateHomeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand CreateNewFolderCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand RenameItemCommand { get; }
        public ICommand CopyItemCommand { get; }
        public ICommand CutItemCommand { get; }
        public ICommand PasteItemCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadDirectoryContents()
        {
            FileSystemItems.Clear();
            try
            {
                var items = fileSystemService.GetDirectoryContents(CurrentPath, ShowHiddenFiles);
                foreach (var item in items)
                {
                    FileSystemItems.Add(item);
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show("Access denied to this directory.", "Access Denied", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void UpdatePreview()
        {
            if (SelectedItem?.Type != "Directory")
            {
                PreviewContent = previewService.GetPreview(SelectedItem?.Path);
            }
            else
            {
                PreviewContent = null;
            }
        }

        // Command execution methods
        private void ExecuteNavigateBack()
        {
            if (navigationService.NavigateBack())
            {
                OnPropertyChanged(nameof(CurrentPath));
                LoadDirectoryContents();
            }
        }

        private void ExecuteNavigateForward()
        {
            if (navigationService.NavigateForward())
            {
                OnPropertyChanged(nameof(CurrentPath));
                LoadDirectoryContents();
            }
        }

        private void ExecuteNavigateUp()
        {
            if (navigationService.NavigateUp())
            {
                OnPropertyChanged(nameof(CurrentPath));
                LoadDirectoryContents();
            }
        }

        private void ExecuteNavigateHome()
        {
            if (navigationService.NavigateHome())
            {
                OnPropertyChanged(nameof(CurrentPath));
                LoadDirectoryContents();
            }
        }

        private void ExecuteRefresh()
        {
            LoadDirectoryContents();
        }

        private void ExecuteOpenFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open File",
                Filter = "All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                CurrentPath = Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void ExecuteOpenFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a folder"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CurrentPath = dialog.SelectedPath;
            }
        }

        private void ExecuteSearch()
        {
            // TODO: Implement search functionality
        }

        private void ExecuteCreateNewFolder()
        {
            var dialog = new InputDialog("New Folder", "Enter folder name:", "New Folder");
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    fileSystemService.CreateDirectory(CurrentPath, dialog.ResponseText);
                    LoadDirectoryContents();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error creating folder: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteDeleteItem()
        {
            if (SelectedItem != null)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete '{SelectedItem.Name}'?",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        fileSystemService.DeleteItem(SelectedItem.Path);
                        LoadDirectoryContents();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error deleting item: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ExecuteRenameItem()
        {
            if (SelectedItem != null)
            {
                var dialog = new InputDialog("Rename", "Enter new name:", SelectedItem.Name);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        fileSystemService.RenameItem(SelectedItem.Path, dialog.ResponseText);
                        LoadDirectoryContents();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error renaming item: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ExecuteCopyItem()
        {
            if (SelectedItem != null)
            {
                clipboardPath = SelectedItem.Path;
                isCutOperation = false;
            }
        }

        private void ExecuteCutItem()
        {
            if (SelectedItem != null)
            {
                clipboardPath = SelectedItem.Path;
                isCutOperation = true;
            }
        }

        private void ExecutePasteItem()
        {
            if (!string.IsNullOrEmpty(clipboardPath))
            {
                try
                {
                    string destinationPath = Path.Combine(CurrentPath, Path.GetFileName(clipboardPath));
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
                    LoadDirectoryContents();
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
    }
} 