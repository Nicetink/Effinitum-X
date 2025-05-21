using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;

namespace SystemOptimizer.Models
{
    public class UpdateManager
    {
        private const string GithubRepoApiUrl = "https://api.github.com/repos/Nicetink/Effinitum-X/releases/latest";
        private const string CurrentVersion = "1.6.0"; // Current version
        
        // Event for update notification
        public event EventHandler<UpdateEventArgs> UpdateCheckCompleted;
        
        public async Task<UpdateInfo> CheckForUpdates()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Add User-Agent header for GitHub API
                    client.DefaultRequestHeaders.Add("User-Agent", "Effinitum-X-UpdateChecker");
                    
                    // Get latest release information
                    var response = await client.GetStringAsync(GithubRepoApiUrl);
                    
                    // Parse JSON
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var root = doc.RootElement;
                        
                        string latestVersion = root.GetProperty("tag_name").GetString().Replace("v", "");
                        string releaseNotes = root.GetProperty("body").GetString();
                        string downloadUrl = root.GetProperty("zipball_url").GetString();
                        
                        // Compare versions
                        bool isNewer = IsVersionNewer(latestVersion, CurrentVersion);
                        
                        var updateInfo = new UpdateInfo
                        {
                            CurrentVersion = CurrentVersion,
                            LatestVersion = latestVersion,
                            IsUpdateAvailable = isNewer,
                            ReleaseNotes = releaseNotes,
                            DownloadUrl = downloadUrl
                        };
                        
                        // Trigger event
                        UpdateCheckCompleted?.Invoke(this, new UpdateEventArgs(updateInfo));
                        
                        return updateInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
                
                var updateInfo = new UpdateInfo
                {
                    CurrentVersion = CurrentVersion,
                    IsUpdateAvailable = false,
                    ErrorMessage = ex.Message
                };
                
                // Trigger event with error
                UpdateCheckCompleted?.Invoke(this, new UpdateEventArgs(updateInfo));
                
                return updateInfo;
            }
        }
        
        public async Task<bool> DownloadAndInstallUpdate(UpdateInfo updateInfo)
        {
            try
            {
                if (updateInfo == null || !updateInfo.IsUpdateAvailable)
                    return false;
                
                using (var client = new HttpClient())
                {
                    // Add User-Agent header for GitHub API
                    client.DefaultRequestHeaders.Add("User-Agent", "Effinitum-X-UpdateChecker");
                    
                    // Create temporary folder for update
                    string tempPath = Path.Combine(Path.GetTempPath(), "Effinitum-X-Update");
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                    
                    Directory.CreateDirectory(tempPath);
                    
                    // Download update archive
                    string zipPath = Path.Combine(tempPath, "update.zip");
                    byte[] zipData = await client.GetByteArrayAsync(updateInfo.DownloadUrl);
                    await File.WriteAllBytesAsync(zipPath, zipData);
                    
                    // Extract archive
                    ZipFile.ExtractToDirectory(zipPath, tempPath, true);
                    
                    // Create bat file for update (replaces files on restart)
                    string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                    string updateBatPath = Path.Combine(tempPath, "update.bat");
                    
                    // Create update script
                    string batContent = $@"@echo off
taskkill /f /im SystemOptimizer.exe
timeout /t 2 /nobreak
xcopy ""{tempPath}\*.*"" ""{currentDir}"" /e /i /h /y
start """" ""{currentDir}\SystemOptimizer.exe""
del ""{updateBatPath}""
exit";
                    
                    File.WriteAllText(updateBatPath, batContent);
                    
                    // Run update script
                    Process process = new Process();
                    process.StartInfo.FileName = updateBatPath;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.Start();
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error installing update: {ex.Message}");
                return false;
            }
        }
        
        private bool IsVersionNewer(string latestVersion, string currentVersion)
        {
            try
            {
                // Split versions into components
                var latestParts = latestVersion.Split('.').Select(int.Parse).ToArray();
                var currentParts = currentVersion.Split('.').Select(int.Parse).ToArray();
                
                // Compare components
                for (int i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
                {
                    if (latestParts[i] > currentParts[i])
                        return true;
                    else if (latestParts[i] < currentParts[i])
                        return false;
                }
                
                // If all matches, but latest version has more components
                return latestParts.Length > currentParts.Length;
            }
            catch
            {
                // If version parsing error, assume no update
                return false;
            }
        }
    }
    
    public class UpdateInfo
    {
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    public class UpdateEventArgs : EventArgs
    {
        public UpdateInfo UpdateInfo { get; }
        
        public UpdateEventArgs(UpdateInfo updateInfo)
        {
            UpdateInfo = updateInfo;
        }
    }
} 