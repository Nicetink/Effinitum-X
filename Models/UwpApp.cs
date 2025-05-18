using System;

namespace SystemOptimizer.Models
{
    public class UwpApp
    {
        public string Name { get; set; } = string.Empty;
        public string PackageFamilyName { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string InstallLocation { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public string Version { get; set; } = string.Empty;
    }
} 