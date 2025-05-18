using System;

namespace SystemOptimizer.Models
{
    public class FolderSizeInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }
} 