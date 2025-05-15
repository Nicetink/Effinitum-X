using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Win32;
using SystemOptimizer.Models;

namespace SystemOptimizer.Services
{
    public class SystemService
    {
        public async Task<Dictionary<string, string>> GetSystemInfo()
        {
            var info = new Dictionary<string, string>();
            
            await Task.Run(() => 
            {
                try 
                {
                    // Get system information
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                    {
                        foreach (var os in searcher.Get())
                        {
                            info["OS"] = os["Caption"]?.ToString() ?? string.Empty;
                            info["Version"] = os["Version"]?.ToString() ?? string.Empty;
                            info["Architecture"] = os["OSArchitecture"]?.ToString() ?? string.Empty;
                            info["Manufacturer"] = os["Manufacturer"]?.ToString() ?? string.Empty;
                            info["Last Boot Time"] = ManagementDateTimeConverter.ToDateTime(os["LastBootUpTime"]?.ToString() ?? string.Empty).ToString();
                            
                            // Get memory information
                            double totalRam = Convert.ToDouble(os["TotalVisibleMemorySize"]);
                            double freeRam = Convert.ToDouble(os["FreePhysicalMemory"]);
                            
                            info["Total RAM"] = $"{Math.Round(totalRam / 1024 / 1024, 2)} GB";
                            info["Free RAM"] = $"{Math.Round(freeRam / 1024 / 1024, 2)} GB";
                            info["Used RAM"] = $"{Math.Round((totalRam - freeRam) / 1024 / 1024, 2)} GB";
                        }
                    }
                    
                    // CPU information
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                    {
                        foreach (var cpu in searcher.Get())
                        {
                            info["CPU"] = cpu["Name"]?.ToString() ?? string.Empty;
                            info["Cores"] = cpu["NumberOfCores"]?.ToString() ?? string.Empty;
                            info["Logical Processors"] = cpu["NumberOfLogicalProcessors"]?.ToString() ?? string.Empty;
                            info["CPU Max Clock Speed"] = $"{Convert.ToDouble(cpu["MaxClockSpeed"]) / 1000:F2} GHz";
                            info["CPU Load"] = $"{cpu["LoadPercentage"]}%";
                        }
                    }
                    
                    // Motherboard information
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                    {
                        foreach (var board in searcher.Get())
                        {
                            info["Motherboard"] = (board["Manufacturer"]?.ToString() ?? string.Empty) + " " + (board["Product"]?.ToString() ?? string.Empty);
                        }
                    }
                    
                    // GPU information
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                    {
                        foreach (var gpu in searcher.Get())
                        {
                            info["GPU"] = gpu["Name"]?.ToString() ?? string.Empty;
                            info["GPU Driver Version"] = gpu["DriverVersion"]?.ToString() ?? string.Empty;
                            info["GPU RAM"] = $"{Convert.ToDouble(gpu["AdapterRAM"]) / 1024 / 1024 / 1024:F2} GB";
                        }
                    }
                    
                    // Disk information
                    int diskCounter = 1;
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                        {
                            info[$"Disk {diskCounter} ({drive.Name})"] = 
                                $"Type: {drive.DriveType}, Total: {Math.Round(drive.TotalSize / 1024.0 / 1024.0 / 1024.0, 2)} GB, " +
                                $"Free: {Math.Round(drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0, 2)} GB";
                            diskCounter++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    info["Error"] = ex.Message;
                }
            });
            
            return info;
        }

        public async Task<CleanupResult> CleanTemporaryFiles(bool cleanTempFiles, bool cleanRecycleBin, 
                                                            bool cleanBrowserCache, bool cleanWindowsLogs,
                                                            bool cleanWindowsUpdate, int cleanupLevel)
        {
            CleanupResult result = new CleanupResult();
            
            await Task.Run(() => 
            {
                try 
                {
                    // Clean temporary files
                    if (cleanTempFiles)
                    {
                        result.BytesFreed += CleanDirectory(Path.GetTempPath());
                        
                        // Clean Windows temp files
                        result.BytesFreed += CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                        
                        // Clean Prefetch if cleanup level is higher
                        if (cleanupLevel >= 2)
                        {
                            result.BytesFreed += CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"));
                        }
                    }
                    
                    // Clean recycle bin
                    if (cleanRecycleBin)
                    {
                        EmptyRecycleBin();
                    }
                    
                    // Clean browser cache
                    if (cleanBrowserCache)
                    {
                        // Chrome
                        string chromeCache = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Google\\Chrome\\User Data\\Default\\Cache");
                        result.BytesFreed += CleanDirectory(chromeCache);
                        
                        // Firefox
                        string firefoxProfile = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Mozilla\\Firefox\\Profiles");
                        
                        if (Directory.Exists(firefoxProfile))
                        {
                            foreach (var profile in Directory.GetDirectories(firefoxProfile))
                            {
                                string firefoxCache = Path.Combine(profile, "cache2");
                                result.BytesFreed += CleanDirectory(firefoxCache);
                            }
                        }
                        
                        // Edge
                        string edgeCache = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Microsoft\\Edge\\User Data\\Default\\Cache");
                        result.BytesFreed += CleanDirectory(edgeCache);
                    }
                    
                    // Clean Windows logs
                    if (cleanWindowsLogs && cleanupLevel >= 2)
                    {
                        string eventLogsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32\\winevt\\Logs");
                        // We don't delete logs directly but clear them through wevtutil
                        ClearEventLogs();
                    }
                    
                    // Clean Windows update cache
                    if (cleanWindowsUpdate && cleanupLevel >= 3)
                    {
                        // This is a more aggressive cleanup, only do it at the highest level
                        string windowsUpdatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution\\Download");
                        
                        // Stop Windows Update service first
                        RunProcess("net", "stop wuauserv");
                        
                        result.BytesFreed += CleanDirectory(windowsUpdatePath);
                        
                        // Restart Windows Update service
                        RunProcess("net", "start wuauserv");
                    }
                    
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = ex.Message;
                    result.Success = false;
                }
            });
            
            return result;
        }
        
        private long CleanDirectory(string path)
        {
            long bytesFreed = 0;
            
            if (!Directory.Exists(path))
                return 0;
            
            try
            {
                // Calculate size first
                bytesFreed = CalculateFolderSize(new DirectoryInfo(path));
                
                // Delete files
                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal); // Clear read-only flag if set
                        File.Delete(file);
                    }
                    catch { /* Skip files that can't be deleted */ }
                }
                
                // Delete subdirectories
                foreach (var dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch { /* Skip directories that can't be deleted */ }
                }
            }
            catch
            {
                // Ignore errors if we can't access the directory
            }
            
            return bytesFreed;
        }
        
        private void EmptyRecycleBin()
        {
            RunProcess("cmd.exe", "/c rd /s /q C:\\$Recycle.Bin");
        }
        
        private void ClearEventLogs()
        {
            string[] logNames = {"Application", "System", "Security"};
            
            foreach (string log in logNames)
            {
                RunProcess("wevtutil.exe", $"cl {log}");
            }
        }
        
        private void RunProcess(string fileName, string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                try
                {
                    process.Start();
                    process.WaitForExit();
                }
                catch
                {
                    // Ignore any errors running the process
                }
            }
        }

        public async Task<Dictionary<string, long>> ScanDiskUsage(string path)
        {
            var result = new Dictionary<string, long>();
            
            await Task.Run(() => 
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    if (di.Exists)
                    {
                        foreach (var dir in di.GetDirectories())
                        {
                            try
                            {
                                long size = CalculateFolderSize(dir);
                                result[dir.FullName] = size;
                            }
                            catch { /* Skip directories we can't access */ }
                        }
                    }
                }
                catch { /* Error handling */ }
            });
            
            return result;
        }
        
        private long CalculateFolderSize(DirectoryInfo folder)
        {
            long folderSize = 0;
            
            // Add the sizes of all files in the directory
            try
            {
                foreach (FileInfo file in folder.GetFiles())
                {
                    folderSize += file.Length;
                }
            
                // Recursively process subdirectories
                foreach (DirectoryInfo subDir in folder.GetDirectories())
                {
                    try
                    {
                        folderSize += CalculateFolderSize(subDir);
                    }
                    catch { /* Skip inaccessible directories */ }
                }
            }
            catch
            {
                // If we can't access the folder, return 0
            }
            
            return folderSize;
        }

        public async Task<List<ProcessInfo>> GetRunningProcesses()
        {
            var processes = new List<ProcessInfo>();
            
            await Task.Run(() => 
            {
                var cpuUsage = GetProcessCpuUsage();
                
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        double cpuPercent = 0;
                        if (cpuUsage.ContainsKey(process.Id))
                        {
                            cpuPercent = cpuUsage[process.Id];
                        }
                        
                        ProcessInfo info = new ProcessInfo
                        {
                            Id = process.Id,
                            Name = process.ProcessName,
                            MemoryUsage = process.WorkingSet64 / 1024 / 1024, // MB
                            CpuUsage = cpuPercent,
                            StartTime = process.StartTime
                        };
                        
                        processes.Add(info);
                    }
                    catch { /* Skip processes with access errors */ }
                }
            });
            
            return processes;
        }
        
        private Dictionary<int, double> GetProcessCpuUsage()
        {
            var result = new Dictionary<int, double>();
            
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT IDProcess, PercentProcessorTime FROM Win32_PerfFormattedData_PerfProc_Process"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        int pid = Convert.ToInt32(obj["IDProcess"]);
                        double cpu = Convert.ToDouble(obj["PercentProcessorTime"]);
                        
                        if (pid > 0)
                        {
                            result[pid] = cpu;
                        }
                    }
                }
            }
            catch
            {
                // Return empty dictionary if there's an error
            }
            
            return result;
        }
        
        public bool KillProcess(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                process.Kill();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<List<StartupItem>> GetStartupItems()
        {
            var items = new List<StartupItem>();
            
            await Task.Run(() => 
            {
                // Check HKCU\Run
                items.AddRange(GetRegistryStartupItems(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", StartupItemType.RegistryRun));
                
                // Check HKLM\Run
                items.AddRange(GetRegistryStartupItems(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", StartupItemType.RegistryRun));
                
                // Check HKCU\RunOnce
                items.AddRange(GetRegistryStartupItems(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", StartupItemType.RegistryRunOnce));
                
                // Check HKLM\RunOnce
                items.AddRange(GetRegistryStartupItems(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", StartupItemType.RegistryRunOnce));
                
                // Check Startup folders
                items.AddRange(GetStartupFolderItems());
            });
            
            return items;
        }
        
        private IEnumerable<StartupItem> GetRegistryStartupItems(RegistryKey rootKey, string keyPath, StartupItemType itemType)
        {
            var items = new List<StartupItem>();
            
            try
            {
                using (var key = rootKey.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            try
                            {
                                string command = key.GetValue(valueName)?.ToString() ?? string.Empty;
                                
                                var item = new StartupItem
                                {
                                    Name = valueName,
                                    Command = command,
                                    Publisher = GetFilePublisher(command),
                                    IsEnabled = true,
                                    RegistryKey = keyPath,
                                    RegistryValue = valueName,
                                    ItemType = itemType
                                };
                                
                                items.Add(item);
                            }
                            catch
                            {
                                // Skip items that cause errors
                            }
                        }
                    }
                }
            }
            catch
            {
                // Return empty list if there's an error
            }
            
            return items;
        }
        
        private IEnumerable<StartupItem> GetStartupFolderItems()
        {
            var items = new List<StartupItem>();
            
            try
            {
                // Current user startup folder
                string startupFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                
                if (Directory.Exists(startupFolder))
                {
                    foreach (var file in Directory.GetFiles(startupFolder))
                    {
                        try
                        {
                            var item = new StartupItem
                            {
                                Name = Path.GetFileNameWithoutExtension(file),
                                Command = file,
                                Publisher = GetFilePublisher(file),
                                IsEnabled = true,
                                RegistryKey = startupFolder,
                                RegistryValue = file,
                                ItemType = StartupItemType.StartupFolder
                            };
                            
                            items.Add(item);
                        }
                        catch
                        {
                            // Skip items that cause errors
                        }
                    }
                }
                
                // All users startup folder
                string allUsersStartupFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup));
                
                if (Directory.Exists(allUsersStartupFolder))
                {
                    foreach (var file in Directory.GetFiles(allUsersStartupFolder))
                    {
                        try
                        {
                            var item = new StartupItem
                            {
                                Name = Path.GetFileNameWithoutExtension(file),
                                Command = file,
                                Publisher = GetFilePublisher(file),
                                IsEnabled = true,
                                RegistryKey = allUsersStartupFolder,
                                RegistryValue = file,
                                ItemType = StartupItemType.StartupFolder
                            };
                            
                            items.Add(item);
                        }
                        catch
                        {
                            // Skip items that cause errors
                        }
                    }
                }
            }
            catch
            {
                // Return empty list if there's an error
            }
            
            return items;
        }
        
        public bool ToggleStartupItem(StartupItem item, bool enable)
        {
            try
            {
                if (item.ItemType == StartupItemType.RegistryRun || item.ItemType == StartupItemType.RegistryRunOnce)
                {
                    RegistryKey rootKey = item.RegistryKey.StartsWith(@"SOFTWARE\Microsoft") 
                        ? Registry.CurrentUser 
                        : Registry.LocalMachine;
                    
                    using (var key = rootKey.OpenSubKey(item.RegistryKey, true))
                    {
                        if (key != null)
                        {
                            if (enable)
                            {
                                // If it was disabled and we're enabling it, recreate the value
                                key.SetValue(item.RegistryValue, item.Command);
                            }
                            else
                            {
                                // If we're disabling it, remove it from the registry
                                key.DeleteValue(item.RegistryValue, false);
                            }
                            
                            return true;
                        }
                    }
                }
                else if (item.ItemType == StartupItemType.StartupFolder)
                {
                    if (File.Exists(item.RegistryValue))
                    {
                        if (enable)
                        {
                            // Rename back from .disabled to original extension
                            if (item.RegistryValue.EndsWith(".disabled"))
                            {
                                string newPath = item.RegistryValue.Substring(0, item.RegistryValue.Length - 9);
                                File.Move(item.RegistryValue, newPath);
                                item.RegistryValue = newPath;
                            }
                        }
                        else
                        {
                            // Rename to .disabled
                            if (!item.RegistryValue.EndsWith(".disabled"))
                            {
                                string newPath = item.RegistryValue + ".disabled";
                                File.Move(item.RegistryValue, newPath);
                                item.RegistryValue = newPath;
                            }
                        }
                        
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        private string GetFilePublisher(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return string.Empty;
                
                // Extract executable path if the command has arguments
                string exePath = filePath.Trim('"');
                if (exePath.Contains(" "))
                {
                    exePath = exePath.Substring(0, exePath.IndexOf(" "));
                }
                
                if (File.Exists(exePath))
                {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                    return !string.IsNullOrEmpty(versionInfo.CompanyName) ? versionInfo.CompanyName : "Unknown";
                }
            }
            catch
            {
                // If we can't determine the publisher, return Unknown
            }
            
            return "Unknown";
        }
        
        public async Task<bool> OptimizeStartup()
        {
            bool success = true;
            
            await Task.Run(() => 
            {
                try
                {
                    // Get all startup items
                    var items = GetStartupItems().GetAwaiter().GetResult();
                    
                    // List of known safe items to keep
                    List<string> safePublishers = new List<string>
                    {
                        "Microsoft Corporation",
                        "Microsoft",
                        "Intel Corporation",
                        "Intel",
                        "NVIDIA Corporation",
                        "AMD",
                        "Advanced Micro Devices, Inc.",
                        "Dell",
                        "Dell Inc."
                    };
                    
                    // List of known items to disable
                    List<string> disableNames = new List<string>
                    {
                        "spotify",
                        "steam",
                        "epic games",
                        "discord",
                        "skype",
                        "ccleaner",
                        "onedrive",
                        "quicktime",
                        "itunes",
                        "google update",
                        "adobe reader",
                        "adobe acrobat",
                        "adobe creative cloud",
                        "dropbox",
                        "box",
                        "zoom",
                        "slack"
                    };
                    
                    foreach (var item in items)
                    {
                        // Skip if item is from Microsoft or other critical system vendors
                        if (safePublishers.Any(p => item.Publisher.Contains(p, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                        
                        // Disable items that match known applications that don't need to start with Windows
                        bool shouldDisable = disableNames.Any(
                            name => item.Name.Contains(name, StringComparison.OrdinalIgnoreCase) || 
                                   item.Command.Contains(name, StringComparison.OrdinalIgnoreCase));
                                   
                        if (shouldDisable && item.IsEnabled)
                        {
                            bool result = ToggleStartupItem(item, false);
                            if (!result) success = false;
                        }
                    }
                }
                catch
                {
                    success = false;
                }
            });
            
            return success;
        }
    }
    
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double MemoryUsage { get; set; } // In MB
        public double CpuUsage { get; set; } // In percentage
        public DateTime StartTime { get; set; }
    }
    
    public class CleanupResult
    {
        public bool Success { get; set; }
        public long BytesFreed { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        
        public string FormattedBytesFreed
        {
            get
            {
                string[] units = { "B", "KB", "MB", "GB", "TB" };
                double size = BytesFreed;
                int unitIndex = 0;
                
                while (size >= 1024 && unitIndex < units.Length - 1)
                {
                    size /= 1024;
                    unitIndex++;
                }
                
                return $"{size:F2} {units[unitIndex]}";
            }
        }
    }
} 