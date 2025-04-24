using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Management;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FileManager
{
    public partial class TaskManagerWindow : Window
    {
        private readonly DispatcherTimer timer;
        private readonly ObservableCollection<ProcessInfo> processes;
        private readonly PerformanceCounter cpuCounter;
        private readonly PerformanceCounter ramCounter;
        private readonly PerformanceCounter diskCounter;

        public TaskManagerWindow()
        {
            InitializeComponent();
            
            processes = new ObservableCollection<ProcessInfo>();
            ProcessList.ItemsSource = processes;

            // Initialize performance counters
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

            // Set up timer to update information every second
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateSystemInfo();
            UpdateProcessList();
        }

        private void UpdateSystemInfo()
        {
            try
            {
                CpuUsage.Text = $"{cpuCounter.NextValue():F1}%";
                
                // Get total physical memory using WMI
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var totalMemory = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                        var availableMemory = ramCounter.NextValue() * 1024 * 1024; // Convert MB to bytes
                        var usedMemory = (totalMemory - availableMemory) / totalMemory * 100;
                        MemoryUsage.Text = $"{usedMemory:F1}%";
                    }
                }
                
                DiskUsage.Text = $"{diskCounter.NextValue():F1}%";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating system info: {ex.Message}");
            }
        }

        private void UpdateProcessList()
        {
            processes.Clear();
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    processes.Add(new ProcessInfo
                    {
                        ProcessName = process.ProcessName,
                        Id = process.Id,
                        CPU = process.TotalProcessorTime.TotalMilliseconds,
                        Memory = process.WorkingSet64 / (1024 * 1024)
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting process info: {ex.Message}");
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            timer.Stop();
            cpuCounter.Dispose();
            ramCounter.Dispose();
            diskCounter.Dispose();
            base.OnClosing(e);
        }
    }

    public class ProcessInfo
    {
        public string ProcessName { get; set; }
        public int Id { get; set; }
        public double CPU { get; set; }
        public long Memory { get; set; }
    }
} 