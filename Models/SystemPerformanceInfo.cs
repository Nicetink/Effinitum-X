using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SystemOptimizer.Models
{
    public class SystemPerformanceInfo : INotifyPropertyChanged
    {
        private double _cpuUsage;
        private double _memoryUsage;
        private double _diskUsage;
        private ObservableCollection<PerformancePoint> _cpuHistory;
        private ObservableCollection<PerformancePoint> _memoryHistory;
        private ObservableCollection<PerformancePoint> _diskHistory;
        
        public double CpuUsage
        {
            get => _cpuUsage;
            set
            {
                if (_cpuUsage != value)
                {
                    _cpuUsage = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public double MemoryUsage
        {
            get => _memoryUsage;
            set
            {
                if (_memoryUsage != value)
                {
                    _memoryUsage = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public double DiskUsage
        {
            get => _diskUsage;
            set
            {
                if (_diskUsage != value)
                {
                    _diskUsage = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public ObservableCollection<PerformancePoint> CpuHistory
        {
            get => _cpuHistory;
            set
            {
                if (_cpuHistory != value)
                {
                    _cpuHistory = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public ObservableCollection<PerformancePoint> MemoryHistory
        {
            get => _memoryHistory;
            set
            {
                if (_memoryHistory != value)
                {
                    _memoryHistory = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public ObservableCollection<PerformancePoint> DiskHistory
        {
            get => _diskHistory;
            set
            {
                if (_diskHistory != value)
                {
                    _diskHistory = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public SystemPerformanceInfo()
        {
            _cpuHistory = new ObservableCollection<PerformancePoint>();
            _memoryHistory = new ObservableCollection<PerformancePoint>();
            _diskHistory = new ObservableCollection<PerformancePoint>();
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class PerformancePoint
    {
        public DateTime Time { get; set; }
        public double Value { get; set; }
        
        public PerformancePoint(DateTime time, double value)
        {
            Time = time;
            Value = value;
        }
    }
} 