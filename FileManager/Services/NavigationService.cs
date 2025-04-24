using System;
using System.Collections.Generic;
using System.IO;

namespace FileManager.Services
{
    /// <summary>
    /// Service class for handling directory navigation operations and history.
    /// </summary>
    public class NavigationService
    {
        private readonly Stack<string> backStack;
        private readonly Stack<string> forwardStack;
        private string currentPath;

        /// <summary>
        /// Initializes a new instance of the NavigationService class.
        /// </summary>
        public NavigationService()
        {
            backStack = new Stack<string>();
            forwardStack = new Stack<string>();
            currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>
        /// Gets the current directory path.
        /// </summary>
        public string CurrentPath
        {
            get => currentPath;
            private set
            {
                if (currentPath != value)
                {
                    backStack.Push(currentPath);
                    forwardStack.Clear();
                    currentPath = value;
                }
            }
        }

        /// <summary>
        /// Navigates to the specified directory.
        /// </summary>
        /// <param name="path">The path to navigate to.</param>
        /// <returns>True if navigation was successful; otherwise, false.</returns>
        public bool NavigateTo(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    CurrentPath = path;
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Navigates to the parent directory.
        /// </summary>
        /// <returns>True if navigation was successful; otherwise, false.</returns>
        public bool NavigateUp()
        {
            try
            {
                var parent = Directory.GetParent(currentPath);
                if (parent != null)
                {
                    return NavigateTo(parent.FullName);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Navigates back in the history.
        /// </summary>
        /// <returns>True if navigation was successful; otherwise, false.</returns>
        public bool NavigateBack()
        {
            if (backStack.Count > 0)
            {
                forwardStack.Push(currentPath);
                currentPath = backStack.Pop();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Navigates forward in the history.
        /// </summary>
        /// <returns>True if navigation was successful; otherwise, false.</returns>
        public bool NavigateForward()
        {
            if (forwardStack.Count > 0)
            {
                backStack.Push(currentPath);
                currentPath = forwardStack.Pop();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Navigates to the home directory.
        /// </summary>
        /// <returns>True if navigation was successful; otherwise, false.</returns>
        public bool NavigateHome()
        {
            return NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        /// <summary>
        /// Checks if backward navigation is possible.
        /// </summary>
        public bool CanNavigateBack => backStack.Count > 0;

        /// <summary>
        /// Checks if forward navigation is possible.
        /// </summary>
        public bool CanNavigateForward => forwardStack.Count > 0;

        /// <summary>
        /// Clears the navigation history.
        /// </summary>
        public void ClearHistory()
        {
            backStack.Clear();
            forwardStack.Clear();
        }
    }
} 