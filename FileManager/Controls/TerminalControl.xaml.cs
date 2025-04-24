using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileManager.Controls
{
    public partial class TerminalControl : System.Windows.Controls.UserControl
    {
        private Process process;
        private StringBuilder outputBuffer;
        private string currentDirectory;

        public event EventHandler CloseRequested;

        public TerminalControl()
        {
            InitializeComponent();
            outputBuffer = new StringBuilder();
            this.Unloaded += TerminalControl_Unloaded;
            InitializeTerminal();
        }

        private void TerminalControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                process.Dispose();
            }
        }

        private void InitializeTerminal()
        {
            try
            {
                process = new Process();
                process.StartInfo.FileName = GetShellExecutable();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = currentDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                WritePrompt();
            }
            catch (Exception ex)
            {
                AppendOutput($"Error starting terminal: {ex.Message}");
            }
        }

        private string GetShellExecutable()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return "powershell.exe";
            }
            else
            {
                return "/bin/bash";
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Dispatcher.Invoke(() => AppendOutput(e.Data));
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Dispatcher.Invoke(() => AppendOutput(e.Data, true));
            }
        }

        private void AppendOutput(string text, bool isError = false)
        {
            outputBuffer.AppendLine(text);
            OutputTextBlock.Text = outputBuffer.ToString();
            OutputScrollViewer.ScrollToEnd();
        }

        private void WritePrompt()
        {
            string prompt = $"{Environment.UserName}@{Environment.MachineName} {process.StartInfo.WorkingDirectory}> ";
            AppendOutput(prompt);
        }

        private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string command = InputTextBox.Text;
                InputTextBox.Text = string.Empty;
                
                if (!string.IsNullOrWhiteSpace(command))
                {
                    try
                    {
                        process.StandardInput.WriteLine(command);
                        process.StandardInput.Flush();
                    }
                    catch (Exception ex)
                    {
                        AppendOutput($"Error executing command: {ex.Message}", true);
                    }
                }
                else
                {
                    WritePrompt();
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public void SetWorkingDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                currentDirectory = directory;
                if (process != null && !process.HasExited)
                {
                    try
                    {
                        process.StandardInput.WriteLine($"cd \"{directory}\"");
                        process.StandardInput.Flush();
                    }
                    catch (Exception ex)
                    {
                        AppendOutput($"Error changing directory: {ex.Message}", true);
                    }
                }
            }
        }
    }
} 