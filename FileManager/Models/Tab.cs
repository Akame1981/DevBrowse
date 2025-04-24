using System;
using System.IO;
using System.ComponentModel;

namespace FileManager.Models
{
    /// <summary>
    /// Represents a single tab in the file explorer.
    /// </summary>
    public class Tab : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the unique identifier for this tab.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        private string currentPath;
        private string name;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the current directory path for this tab.
        /// </summary>
        public string CurrentPath
        {
            get => currentPath;
            set
            {
                if (currentPath != value)
                {
                    currentPath = value;
                    UpdateName();
                    OnPropertyChanged(nameof(CurrentPath));
                }
            }
        }

        /// <summary>
        /// Gets or sets the display name for this tab.
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether this tab is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Initializes a new instance of the Tab class.
        /// </summary>
        /// <param name="name">The display name of the tab.</param>
        /// <param name="path">The initial directory path.</param>
        public Tab(string name, string path)
        {
            this.name = name;
            this.currentPath = path;
        }

        private void UpdateName()
        {
            if (currentPath == "Drives")
            {
                Name = "This PC";
            }
            else
            {
                try
                {
                    string newName = Path.GetFileName(currentPath) ?? "This PC";
                    if (string.IsNullOrEmpty(newName))
                    {
                        newName = currentPath;
                    }
                    Name = newName;
                }
                catch
                {
                    Name = "This PC";
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>The name of the tab.</returns>
        public override string ToString()
        {
            return Name;
        }
    }
} 