using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using FileManager.Models;

namespace FileManager.ViewModels
{
    /// <summary>
    /// Manages the collection of tabs and their operations.
    /// </summary>
    public class TabManager : INotifyPropertyChanged
    {
        private readonly ObservableCollection<Tab> _tabs = new ObservableCollection<Tab>();
        private Tab _activeTab;

        /// <summary>
        /// Gets the collection of tabs.
        /// </summary>
        public ObservableCollection<Tab> Tabs => _tabs;

        /// <summary>
        /// Gets or sets the currently active tab.
        /// </summary>
        public Tab ActiveTab
        {
            get => _activeTab;
            set
            {
                if (_activeTab != value)
                {
                    if (_activeTab != null)
                        _activeTab.IsActive = false;
                    
                    _activeTab = value;
                    
                    if (_activeTab != null)
                        _activeTab.IsActive = true;
                    
                    OnPropertyChanged(nameof(ActiveTab));
                    OnActiveTabChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler OnActiveTabChanged;

        /// <summary>
        /// Initializes a new instance of the TabManager class.
        /// </summary>
        public TabManager()
        {
            // Create the initial tab
            AddNewTab("This PC", "Drives");
        }

        /// <summary>
        /// Adds a new tab with the specified name and path.
        /// </summary>
        /// <param name="name">The display name of the tab.</param>
        /// <param name="path">The initial directory path.</param>
        public void AddNewTab(string name, string path)
        {
            var tab = new Tab(name, path);
            _tabs.Add(tab);
            ActiveTab = tab;
        }

        /// <summary>
        /// Closes the specified tab.
        /// </summary>
        /// <param name="tab">The tab to close.</param>
        public void CloseTab(Tab tab)
        {
            if (_tabs.Count > 1)
            {
                int index = _tabs.IndexOf(tab);
                _tabs.Remove(tab);

                // If we removed the active tab, activate the next available tab
                if (tab == ActiveTab)
                {
                    if (index >= _tabs.Count)
                        index = _tabs.Count - 1;
                    ActiveTab = _tabs[index];
                }
            }
        }

        /// <summary>
        /// Switches to the specified tab.
        /// </summary>
        /// <param name="tab">The tab to switch to.</param>
        public void SwitchToTab(Tab tab)
        {
            if (_tabs.Contains(tab))
            {
                ActiveTab = tab;
            }
        }

        /// <summary>
        /// Updates the current path of the active tab.
        /// </summary>
        /// <param name="path">The new path.</param>
        public void UpdateActiveTabPath(string path)
        {
            if (ActiveTab != null)
            {
                ActiveTab.CurrentPath = path;
                ActiveTab.Name = System.IO.Path.GetFileName(path) ?? "This PC";
            }
        }

        /// <summary>
        /// Gets the current path of the active tab.
        /// </summary>
        /// <returns>The current path of the active tab, or null if no tab is active.</returns>
        public string GetActiveTabPath()
        {
            return ActiveTab?.CurrentPath;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 