using System;
using System.Windows.Input;

namespace SystemOptimizer.Models
{
    public class UwpApp
    {
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string Version { get; set; }
        public string Size { get; set; }
        public string PackageFullName { get; set; }
        public ICommand? UninstallCommand { get; set; }
        
        public UwpApp()
        {
            Name = string.Empty;
            Publisher = string.Empty;
            Version = string.Empty;
            Size = string.Empty;
            PackageFullName = string.Empty;
            UninstallCommand = null;
        }
    }
} 