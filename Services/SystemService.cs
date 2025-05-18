using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Win32;
using SystemOptimizer.Models;
using System.Xml.Linq;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;

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

        public async Task<List<UwpApp>> GetUwpApps()
        {
            List<UwpApp> apps = new List<UwpApp>();
            
            await Task.Run(() =>
            {
                try
                {
                    using (var runspace = RunspaceFactory.CreateRunspace())
                    {
                        runspace.Open();
                        
                        using (var ps = PowerShell.Create())
                        {
                            ps.Runspace = runspace;
                            ps.AddCommand("Get-AppxPackage");
                            
                            var results = ps.Invoke();
                            
                            foreach (var result in results)
                            {
                                var app = new UwpApp
                                {
                                    Name = result.Properties["Name"]?.Value?.ToString() ?? string.Empty,
                                    PackageFamilyName = result.Properties["PackageFamilyName"]?.Value?.ToString() ?? string.Empty,
                                    Publisher = result.Properties["Publisher"]?.Value?.ToString() ?? string.Empty,
                                    InstallLocation = result.Properties["InstallLocation"]?.Value?.ToString() ?? string.Empty,
                                    Version = result.Properties["Version"]?.Value?.ToString() ?? string.Empty,
                                    IsSystem = IsSystemApp(result.Properties["Name"]?.Value?.ToString() ?? string.Empty)
                                };
                                
                                apps.Add(app);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Fallback to using PackageManager COM object if PowerShell is not available
                    try
                    {
                        // This is a simplified alternative method
                        var process = new Process();
                        process.StartInfo.FileName = "powershell.exe";
                        process.StartInfo.Arguments = "-Command \"Get-AppxPackage | Select-Object Name, PackageFamilyName, Publisher, InstallLocation, Version | ConvertTo-Csv -NoTypeInformation\"";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;
                        
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        
                        if (process.ExitCode == 0)
                        {
                            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length > 1) // Header + at least one row
                            {
                                string[] headers = lines[0].Split(',');
                                
                                for (int i = 1; i < lines.Length; i++)
                                {
                                    string[] values = lines[i].Split(',');
                                    
                                    if (values.Length >= 5)
                                    {
                                        var app = new UwpApp
                                        {
                                            Name = values[0].Trim('"'),
                                            PackageFamilyName = values[1].Trim('"'),
                                            Publisher = values[2].Trim('"'),
                                            InstallLocation = values[3].Trim('"'),
                                            Version = values[4].Trim('"'),
                                            IsSystem = IsSystemApp(values[0].Trim('"'))
                                        };
                                        
                                        apps.Add(app);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // If all methods fail, return empty list
                    }
                }
            });
            
            return apps;
        }
        
        private bool IsSystemApp(string name)
        {
            // Определяем системные приложения, которые не рекомендуется удалять
            string[] systemApps = new string[]
            {
                "Microsoft.Windows.ShellExperienceHost",
                "Microsoft.Windows.StartMenuExperienceHost",
                "windows.immersivecontrolpanel",
                "Microsoft.Windows.Cortana",
                "Microsoft.AAD.BrokerPlugin",
                "Microsoft.Windows.CloudExperienceHost",
                "Microsoft.Windows.ContentDeliveryManager",
                "Microsoft.Windows.SecHealthUI",
                "Microsoft.Windows.SecureAssessmentBrowser",
                "Microsoft.Windows.PeopleExperienceHost",
                "Microsoft.Windows.NarratorQuickStart",
                "Microsoft.Windows.ParentalControls",
                "Microsoft.Windows.PrintDialog",
                "Microsoft.Windows.SecureAssessmentBrowser",
                "Microsoft.Windows.XGpuEjectDialog",
                "Windows.CBSPreview",
                "Windows.PrintDialog",
                "Windows.MiracastView",
                "Microsoft.StorePurchaseApp",
                "Microsoft.WindowsStore",
                "Microsoft.Microsoft3DViewer"
            };
            
            return systemApps.Any(a => name.StartsWith(a, StringComparison.OrdinalIgnoreCase));
        }
        
        public async Task<bool> UninstallUwpApp(string packageName)
        {
            bool success = false;
            
            await Task.Run(() =>
            {
                try
                {
                    using (var runspace = RunspaceFactory.CreateRunspace())
                    {
                        runspace.Open();
                        
                        using (var ps = PowerShell.Create())
                        {
                            ps.Runspace = runspace;
                            ps.AddCommand("Remove-AppxPackage");
                            ps.AddParameter("Package", packageName);
                            
                            ps.Invoke();
                            success = ps.HadErrors == false;
                        }
                    }
                }
                catch
                {
                    // Try alternative method using process
                    try
                    {
                        var process = new Process();
                        process.StartInfo.FileName = "powershell.exe";
                        process.StartInfo.Arguments = $"-Command \"Remove-AppxPackage -Package '{packageName}'\"";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        
                        process.Start();
                        process.WaitForExit();
                        
                        success = process.ExitCode == 0;
                    }
                    catch
                    {
                        success = false;
                    }
                }
            });
            
            return success;
        }

        // Методы для управления обновлениями Windows
        public async Task<bool> DisableWindowsUpdates()
        {
            bool success = false;
            
            await Task.Run(() =>
            {
                try
                {
                    // Отключение службы обновлений Windows
                    using (var runspace = RunspaceFactory.CreateRunspace())
                    {
                        runspace.Open();
                        
                        using (var ps = PowerShell.Create())
                        {
                            ps.Runspace = runspace;
                            
                            // Отключаем службу обновлений Windows
                            ps.AddCommand("Stop-Service").AddParameter("Name", "wuauserv").AddParameter("Force", true);
                            ps.Invoke();
                            ps.Commands.Clear();
                            
                            ps.AddCommand("Set-Service").AddParameter("Name", "wuauserv").AddParameter("StartupType", "Disabled");
                            ps.Invoke();
                            ps.Commands.Clear();
                            
                            // Отключаем Центр обновления через реестр
                            ps.AddScript(
                                "# Отключение автоматических обновлений через реестр\r\n" +
                                "$windowsUpdatePath = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate'\r\n" +
                                "$auPath = \"$windowsUpdatePath\\AU\"\r\n" +
                                "\r\n" +
                                "if(!(Test-Path $windowsUpdatePath)) { \r\n" +
                                "    New-Item -Path $windowsUpdatePath -Force | Out-Null\r\n" +
                                "}\r\n" +
                                "\r\n" +
                                "if(!(Test-Path $auPath)) {\r\n" +
                                "    New-Item -Path $auPath -Force | Out-Null\r\n" +
                                "}\r\n" +
                                "\r\n" +
                                "# Запрещаем загрузку обновлений\r\n" +
                                "Set-ItemProperty -Path $auPath -Name 'NoAutoUpdate' -Value 1 -Type DWord -Force | Out-Null\r\n" +
                                "Set-ItemProperty -Path $auPath -Name 'AUOptions' -Value 1 -Type DWord -Force | Out-Null\r\n" +
                                "\r\n" +
                                "# Отключаем WindowsUpdateBox\r\n" +
                                "$wuboxPath = 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update'\r\n" +
                                "if(!(Test-Path $wuboxPath)) {\r\n" +
                                "    New-Item -Path $wuboxPath -Force | Out-Null\r\n" +
                                "}\r\n" +
                                "Set-ItemProperty -Path $wuboxPath -Name 'EnableFeaturedSoftware' -Value 0 -Type DWord -Force | Out-Null");
                            
                            ps.Invoke();
                            
                            success = !ps.HadErrors;
                        }
                    }
                }
                catch
                {
                    // Альтернативный метод с использованием CMD и reg.exe
                    try
                    {
                        // Отключаем службу обновлений Windows
                        RunProcess("net", "stop wuauserv");
                        RunProcess("sc", "config wuauserv start= disabled");
                        
                        // Отключаем через реестр
                        RunProcess("reg", @"add HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate /f");
                        RunProcess("reg", @"add HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU /f");
                        RunProcess("reg", @"add HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU /v NoAutoUpdate /t REG_DWORD /d 1 /f");
                        RunProcess("reg", @"add HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU /v AUOptions /t REG_DWORD /d 1 /f");
                        
                        success = true;
                    }
                    catch
                    {
                        success = false;
                    }
                }
            });
            
            return success;
        }
        
        public async Task<bool> EnableWindowsUpdates()
        {
            bool success = false;
            
            await Task.Run(() =>
            {
                try
                {
                    using (var runspace = RunspaceFactory.CreateRunspace())
                    {
                        runspace.Open();
                        
                        using (var ps = PowerShell.Create())
                        {
                            ps.Runspace = runspace;
                            
                            // Включаем службу обновлений Windows
                            ps.AddCommand("Set-Service").AddParameter("Name", "wuauserv").AddParameter("StartupType", "Automatic");
                            ps.Invoke();
                            ps.Commands.Clear();
                            
                            ps.AddCommand("Start-Service").AddParameter("Name", "wuauserv");
                            ps.Invoke();
                            ps.Commands.Clear();
                            
                            // Восстанавливаем настройки реестра
                            ps.AddScript(
                                "# Включение автоматических обновлений через реестр\r\n" +
                                "$auPath = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU'\r\n" +
                                "\r\n" +
                                "if(Test-Path $auPath) { \r\n" +
                                "    Remove-ItemProperty -Path $auPath -Name 'NoAutoUpdate' -Force -ErrorAction SilentlyContinue\r\n" +
                                "    Remove-ItemProperty -Path $auPath -Name 'AUOptions' -Force -ErrorAction SilentlyContinue\r\n" +
                                "}");
                            
                            ps.Invoke();
                            success = !ps.HadErrors;
                        }
                    }
                }
                catch
                {
                    // Альтернативный метод
                    try
                    {
                        // Включаем службу обновлений Windows
                        RunProcess("sc", "config wuauserv start= auto");
                        RunProcess("net", "start wuauserv");
                        
                        // Удаляем ключи реестра
                        RunProcess("reg", @"delete HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU /v NoAutoUpdate /f");
                        RunProcess("reg", @"delete HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU /v AUOptions /f");
                        
                        success = true;
                    }
                    catch
                    {
                        success = false;
                    }
                }
            });
            
            return success;
        }
        
        // Методы для управления Windows Defender
        public async Task<bool> DisableWindowsDefender()
        {
            bool success = false;
            
            await Task.Run(() =>
            {
                try
                {
                    using (var runspace = RunspaceFactory.CreateRunspace())
                    {
                        runspace.Open();
                        
                        using (var ps = PowerShell.Create())
                        {
                            ps.Runspace = runspace;
                            
                            // Отключение компонентов Windows Defender через реестр
                            ps.AddScript(
                                "# Отключение компонентов Windows Defender\r\n" +
                                "$defenderPath = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows Defender'\r\n" +
                                "\r\n" +
                                "if(!(Test-Path $defenderPath)) {\r\n" +
                                "    New-Item -Path $defenderPath -Force | Out-Null\r\n" +
                                "}\r\n" +
                                "\r\n" +
                                "# Отключаем антивирус\r\n" +
                                "Set-ItemProperty -Path $defenderPath -Name 'DisableAntiSpyware' -Value 1 -Type DWord -Force | Out-Null\r\n" +
                                "\r\n" +
                                "# Отключаем защиту в реальном времени\r\n" +
                                "$realTimePath = \"$defenderPath\\Real-Time Protection\"\r\n" +
                                "if(!(Test-Path $realTimePath)) {\r\n" +
                                "    New-Item -Path $realTimePath -Force | Out-Null\r\n" +
                                "}\r\n" +
                                "Set-ItemProperty -Path $realTimePath -Name 'DisableRealtimeMonitoring' -Value 1 -Type DWord -Force | Out-Null\r\n" +
                                "Set-ItemProperty -Path $realTimePath -Name 'DisableBehaviorMonitoring' -Value 1 -Type DWord -Force | Out-Null\r\n" +
                                "Set-ItemProperty -Path $realTimePath -Name 'DisableScanOnRealtimeEnable' -Value 1 -Type DWord -Force | Out-Null\r\n" +
                                "\r\n" +
                                "# Отключаем службы\r\n" +
                                "Stop-Service -Name WinDefend -Force -ErrorAction SilentlyContinue\r\n" +
                                "Set-Service -Name WinDefend -StartupType Disabled -ErrorAction SilentlyContinue\r\n" +
                                "\r\n" +
                                "# Отключаем WdFilter\r\n" +
                                "Stop-Service -Name WdFilter -Force -ErrorAction SilentlyContinue\r\n" +
                                "Set-Service -Name WdFilter -StartupType Disabled -ErrorAction SilentlyContinue\r\n" +
                                "\r\n" +
                                "# Отключаем WdNisSvc\r\n" +
                                "Stop-Service -Name WdNisSvc -Force -ErrorAction SilentlyContinue\r\n" +
                                "Set-Service -Name WdNisSvc -StartupType Disabled -ErrorAction SilentlyContinue");
                            
                            ps.Invoke();
                            
                            success = !ps.HadErrors;
                        }
                    }
                }
                catch
                {
                    // Альтернативный метод через реестр
                    try
                    {
                        RunProcess("reg", @"add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender"" /v DisableAntiSpyware /t REG_DWORD /d 1 /f");
                        RunProcess("reg", @"add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableRealtimeMonitoring /t REG_DWORD /d 1 /f");
                        RunProcess("reg", @"add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableBehaviorMonitoring /t REG_DWORD /d 1 /f");
                        RunProcess("reg", @"add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableScanOnRealtimeEnable /t REG_DWORD /d 1 /f");
                        
                        // Отключаем службы
                        RunProcess("net", "stop WinDefend");
                        RunProcess("sc", "config WinDefend start= disabled");
                        
                        RunProcess("net", "stop WdFilter");
                        RunProcess("sc", "config WdFilter start= disabled");
                        
                        RunProcess("net", "stop WdNisSvc");
                        RunProcess("sc", "config WdNisSvc start= disabled");
                        
                        success = true;
                    }
                    catch
                    {
                        success = false;
                    }
                }
            });
            
            return success;
        }
        
        public async Task<bool> EnableWindowsDefender()
        {
            bool success = false;
            
            await Task.Run(() =>
            {
                try
                {
                    using (var runspace = RunspaceFactory.CreateRunspace())
                    {
                        runspace.Open();
                        
                        using (var ps = PowerShell.Create())
                        {
                            ps.Runspace = runspace;
                            
                            // Включение компонентов Windows Defender
                            ps.AddScript(
                                "# Включение компонентов Windows Defender\r\n" +
                                "$defenderPath = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows Defender'\r\n" +
                                "\r\n" +
                                "# Включаем антивирус (удаляем ключ отключения)\r\n" +
                                "if(Test-Path $defenderPath) {\r\n" +
                                "    Remove-ItemProperty -Path $defenderPath -Name 'DisableAntiSpyware' -Force -ErrorAction SilentlyContinue\r\n" +
                                "}\r\n" +
                                "\r\n" +
                                "# Включаем защиту в реальном времени\r\n" +
                                "$realTimePath = \"$defenderPath\\Real-Time Protection\"\r\n" +
                                "if(Test-Path $realTimePath) {\r\n" +
                                "    Remove-ItemProperty -Path $realTimePath -Name 'DisableRealtimeMonitoring' -Force -ErrorAction SilentlyContinue\r\n" +
                                "    Remove-ItemProperty -Path $realTimePath -Name 'DisableBehaviorMonitoring' -Force -ErrorAction SilentlyContinue\r\n" +
                                "    Remove-ItemProperty -Path $realTimePath -Name 'DisableScanOnRealtimeEnable' -Force -ErrorAction SilentlyContinue\r\n" +
                                "}\r\n" +
                                "\r\n" +
                                "# Включаем службы\r\n" +
                                "Set-Service -Name WinDefend -StartupType Automatic -ErrorAction SilentlyContinue\r\n" +
                                "Start-Service -Name WinDefend -ErrorAction SilentlyContinue\r\n" +
                                "\r\n" +
                                "# Включаем WdFilter\r\n" +
                                "Set-Service -Name WdFilter -StartupType Automatic -ErrorAction SilentlyContinue\r\n" +
                                "Start-Service -Name WdFilter -ErrorAction SilentlyContinue\r\n" +
                                "\r\n" +
                                "# Включаем WdNisSvc\r\n" +
                                "Set-Service -Name WdNisSvc -StartupType Automatic -ErrorAction SilentlyContinue\r\n" +
                                "Start-Service -Name WdNisSvc -ErrorAction SilentlyContinue");
                            
                            ps.Invoke();
                            
                            success = !ps.HadErrors;
                        }
                    }
                }
                catch
                {
                    // Альтернативный метод через реестр
                    try
                    {
                        RunProcess("reg", @"delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender"" /v DisableAntiSpyware /f");
                        RunProcess("reg", @"delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableRealtimeMonitoring /f");
                        RunProcess("reg", @"delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableBehaviorMonitoring /f");
                        RunProcess("reg", @"delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableScanOnRealtimeEnable /f");
                        
                        // Включаем службы
                        RunProcess("sc", "config WinDefend start= auto");
                        RunProcess("net", "start WinDefend");
                        
                        RunProcess("sc", "config WdFilter start= auto");
                        RunProcess("net", "start WdFilter");
                        
                        RunProcess("sc", "config WdNisSvc start= auto");
                        RunProcess("net", "start WdNisSvc");
                        
                        success = true;
                    }
                    catch
                    {
                        success = false;
                    }
                }
            });
            
            return success;
        }

        // Методы для интеграции с zapret-discord-youtube
        public async Task<bool> InstallZapretDiscordYoutube()
        {
            bool success = false;
            
            await Task.Run(() =>
            {
                try
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), "zapret-discord-youtube");
                    
                    // Создаем временный каталог
                    if (Directory.Exists(tempPath))
                    {
                        Directory.Delete(tempPath, true);
                    }
                    Directory.CreateDirectory(tempPath);
                    
                    // Клонируем репозиторий
                    var processClone = new Process();
                    processClone.StartInfo.FileName = "git";
                    processClone.StartInfo.Arguments = "clone https://github.com/Flowseal/zapret-discord-youtube.git .";
                    processClone.StartInfo.WorkingDirectory = tempPath;
                    processClone.StartInfo.UseShellExecute = false;
                    processClone.StartInfo.CreateNoWindow = true;
                    
                    try
                    {
                        processClone.Start();
                        processClone.WaitForExit();
                        
                        if (processClone.ExitCode != 0)
                        {
                            throw new Exception("Failed to clone repository");
                        }
                    }
                    catch
                    {
                        // Если git недоступен, скачиваем zip архив
                        string downloadUrl = "https://github.com/Flowseal/zapret-discord-youtube/archive/refs/heads/main.zip";
                        string zipPath = Path.Combine(Path.GetTempPath(), "zapret-discord-youtube.zip");
                        
                        using (var client = new System.Net.WebClient())
                        {
                            client.DownloadFile(downloadUrl, zipPath);
                        }
                        
                        // Распаковываем архив
                        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, Path.GetTempPath());
                        
                        // Переименовываем распакованную директорию
                        string extractedPath = Path.Combine(Path.GetTempPath(), "zapret-discord-youtube-main");
                        if (Directory.Exists(extractedPath))
                        {
                            if (Directory.Exists(tempPath))
                            {
                                Directory.Delete(tempPath, true);
                            }
                            Directory.Move(extractedPath, tempPath);
                        }
                        
                        // Удаляем zip-файл
                        if (File.Exists(zipPath))
                        {
                            File.Delete(zipPath);
                        }
                    }
                    
                    // Запустить install.bat от имени администратора
                    var processInstall = new Process();
                    processInstall.StartInfo.FileName = "cmd.exe";
                    processInstall.StartInfo.Arguments = "/c install.bat";
                    processInstall.StartInfo.WorkingDirectory = tempPath;
                    processInstall.StartInfo.Verb = "runas"; // Запуск от имени администратора
                    processInstall.StartInfo.UseShellExecute = true;
                    
                    processInstall.Start();
                    processInstall.WaitForExit();
                    
                    success = processInstall.ExitCode == 0;
                    
                    // Создаем автозапуск для программы, чтобы она запускалась при старте Windows
                    string startupFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                        "zapret-discord-youtube.bat");
                    
                    string exePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "zapret-discord-youtube", "start.bat");
                    
                    if (File.Exists(exePath))
                    {
                        File.WriteAllText(startupFolder, "\"" + exePath + "\"");
                        success = true;
                    }
                }
                catch (Exception)
                {
                    success = false;
                }
            });
            
            return success;
        }
        
        public async Task<bool> UninstallZapretDiscordYoutube()
        {
            bool success = false;
            
            await Task.Run(() =>
            {
                try
                {
                    // Удаляем из автозагрузки
                    string startupFile = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                        "zapret-discord-youtube.bat");
                    
                    if (File.Exists(startupFile))
                    {
                        File.Delete(startupFile);
                    }
                    
                    // Определяем путь установки
                    string installPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "zapret-discord-youtube");
                    
                    // Запустить uninstall.bat
                    if (Directory.Exists(installPath))
                    {
                        string uninstallPath = Path.Combine(installPath, "uninstall.bat");
                        
                        if (File.Exists(uninstallPath))
                        {
                            var process = new Process();
                            process.StartInfo.FileName = "cmd.exe";
                            process.StartInfo.Arguments = "/c " + uninstallPath;
                            process.StartInfo.Verb = "runas"; // Запуск от имени администратора
                            process.StartInfo.UseShellExecute = true;
                            
                            process.Start();
                            process.WaitForExit();
                            
                            success = process.ExitCode == 0;
                        }
                        
                        // Удаляем папку с программой
                        try
                        {
                            Directory.Delete(installPath, true);
                            success = true;
                        }
                        catch
                        {
                            // Игнорируем ошибку если что-то не удалилось
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
        
        // Методы для проверки и оптимизации диска (как в CCleaner)
        public async Task<bool> CheckDiskHealth(string driveLetter)
        {
            bool success = false;
            string result = string.Empty;
            
            await Task.Run(() =>
            {
                try
                {
                    // Запуск проверки диска через chkdsk (только проверка, без исправления)
                    var process = new Process();
                    process.StartInfo.FileName = "chkdsk";
                    process.StartInfo.Arguments = $"{driveLetter}: /f /r /x";
                    process.StartInfo.Verb = "runas"; // Запуск от имени администратора
                    process.StartInfo.UseShellExecute = true;
                    
                    process.Start();
                    process.WaitForExit();
                    
                    success = true;
                }
                catch
                {
                    success = false;
                }
            });
            
            return success;
        }
        
        public async Task<DiskOptimizationResult> OptimizeDisk(string driveLetter, bool defragment, bool cleanupDisk)
        {
            var result = new DiskOptimizationResult
            {
                Success = false,
                CleanupSpaceFreed = 0,
                DefragmentSuccess = false
            };
            
            await Task.Run(() =>
            {
                try
                {
                    if (cleanupDisk)
                    {
                        // Запуск очистки диска через cleanmgr
                        result.CleanupSpaceFreed = RunDiskCleanup(driveLetter);
                    }
                    
                    if (defragment)
                    {
                        // Дефрагментация диска
                        result.DefragmentSuccess = RunDefragmentation(driveLetter);
                    }
                    
                    result.Success = true;
                }
                catch
                {
                    result.Success = false;
                }
            });
            
            return result;
        }
        
        private long RunDiskCleanup(string driveLetter)
        {
            // Получаем размер диска до очистки
            var driveInfo = new DriveInfo(driveLetter);
            long freeSpaceBefore = driveInfo.AvailableFreeSpace;
            
            try
            {
                // Определяем параметры очистки - все возможные варианты очистки
                // См. https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/cleanmgr
                string sageset = "11"; // Любое число от 0 до 65535
                
                // Устанавливаем настройки очистки - все доступные опции
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cleanmgr",
                    Arguments = $"/d {driveLetter}: /sageset:{sageset}",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                
                // Открываем меню выбора. Пользователь должен сам выбрать опции и нажать ОК
                var process = Process.Start(psi);
                process.WaitForExit();
                
                // Запускаем очистку с выбранными опциями
                psi = new ProcessStartInfo
                {
                    FileName = "cleanmgr",
                    Arguments = $"/d {driveLetter}: /sagerun:{sageset}",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                
                process = Process.Start(psi);
                process.WaitForExit();
                
                // Ждем завершения процесса cleanmgr
                Thread.Sleep(2000);
                
                // Получаем размер диска после очистки
                driveInfo = new DriveInfo(driveLetter);
                long freeSpaceAfter = driveInfo.AvailableFreeSpace;
                
                return freeSpaceAfter - freeSpaceBefore;
            }
            catch
            {
                return 0;
            }
        }
        
        private bool RunDefragmentation(string driveLetter)
        {
            try
            {
                // Запуск дефрагментации
                var process = new Process();
                process.StartInfo.FileName = "defrag";
                process.StartInfo.Arguments = $"{driveLetter}: /O"; // /O для оптимизации (подходит и для SSD)
                process.StartInfo.Verb = "runas"; // Запуск от имени администратора
                process.StartInfo.UseShellExecute = true;
                
                process.Start();
                process.WaitForExit();
                
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
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
                if (BytesFreed < 1024)
                    return $"{BytesFreed} B";
                else if (BytesFreed < 1024 * 1024)
                    return $"{BytesFreed / 1024.0:F2} KB";
                else if (BytesFreed < 1024 * 1024 * 1024)
                    return $"{BytesFreed / 1024.0 / 1024.0:F2} MB";
                else
                    return $"{BytesFreed / 1024.0 / 1024.0 / 1024.0:F2} GB";
            }
        }
    }

    // Добавляем новый класс для результатов оптимизации диска
    public class DiskOptimizationResult
    {
        public bool Success { get; set; }
        public long CleanupSpaceFreed { get; set; }
        public bool DefragmentSuccess { get; set; }
        
        public string FormattedSpaceFreed
        {
            get
            {
                if (CleanupSpaceFreed < 1024)
                    return $"{CleanupSpaceFreed} B";
                else if (CleanupSpaceFreed < 1024 * 1024)
                    return $"{CleanupSpaceFreed / 1024.0:F2} KB";
                else if (CleanupSpaceFreed < 1024 * 1024 * 1024)
                    return $"{CleanupSpaceFreed / 1024.0 / 1024.0:F2} MB";
                else
                    return $"{CleanupSpaceFreed / 1024.0 / 1024.0 / 1024.0:F2} GB";
            }
        }
    }
} 