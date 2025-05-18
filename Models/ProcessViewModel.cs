using System;
using System.Windows.Input;

namespace SystemOptimizer.Models
{
    public class ProcessViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double MemoryUsage { get; set; } // In MB
        public double CpuUsage { get; set; } // In percentage
        public DateTime StartTime { get; set; }
        public ICommand KillProcessCommand { get; set; }
    }
} 