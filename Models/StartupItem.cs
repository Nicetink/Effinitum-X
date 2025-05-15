using System;

namespace SystemOptimizer.Models
{
    public class StartupItem
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public string Publisher { get; set; }
        public bool IsEnabled { get; set; }
        public string RegistryKey { get; set; }
        public string RegistryValue { get; set; }
        public StartupItemType ItemType { get; set; }
        
        public StartupItem()
        {
            Name = string.Empty;
            Command = string.Empty; 
            Publisher = string.Empty;
            RegistryKey = string.Empty;
            RegistryValue = string.Empty;
        }
    }
    
    public enum StartupItemType
    {
        RegistryRun,
        RegistryRunOnce,
        StartupFolder,
        Service,
        ScheduledTask
    }
} 