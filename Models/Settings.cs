using System;
using System.IO;
using System.Text.Json;

namespace SystemOptimizer.Models
{
    public class Settings
    {
        public ThemeMode AppTheme { get; set; } = ThemeMode.System;
        public bool RunAtStartup { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool EnableNotifications { get; set; } = true;
        public int CleanupLevel { get; set; } = 2;
        
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SystemOptimizer",
            "settings.json");
            
        public static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    return settings ?? new Settings();
                }
            }
            catch
            {
                // If anything goes wrong, return default settings
            }
            
            return new Settings();
        }
        
        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }
                
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Error saving settings, will use defaults next time
            }
        }
    }
    
    public enum ThemeMode
    {
        Light,
        Dark,
        System
    }
} 