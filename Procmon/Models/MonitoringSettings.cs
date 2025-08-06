using System;
using System.ComponentModel;
using System.IO;

namespace Procmon.Models
{
    /// <summary>
    /// Configuration settings for monitoring sessions
    /// </summary>
    public class MonitoringSettings : INotifyPropertyChanged
    {
        private int durationSeconds = 0;
        private int intervalMilliseconds = 100;
        private bool monitorCpu = true;
        private bool monitorRam = true;
        private bool monitorGpuCore = true;
        private bool monitorGpuVideo = true;
        private bool monitorGpuVram = true;
        private bool logToFile = true;
        private string logFileName;

        public int DurationSeconds 
        { 
            get => durationSeconds; 
            set 
            { 
                if (durationSeconds != value) 
                { 
                    durationSeconds = value; 
                    OnPropertyChanged(nameof(DurationSeconds)); 
                    OnPropertyChanged(nameof(IsInfiniteMode)); 
                } 
            } 
        }

        public int IntervalMilliseconds 
        { 
            get => intervalMilliseconds; 
            set 
            { 
                if (intervalMilliseconds != value) 
                { 
                    intervalMilliseconds = value; 
                    OnPropertyChanged(nameof(IntervalMilliseconds)); 
                } 
            } 
        }

        public bool IsInfiniteMode => DurationSeconds == 0;
        
        // Sensor settings
        public bool MonitorCpu 
        { 
            get => monitorCpu; 
            set 
            { 
                if (monitorCpu != value) 
                { 
                    monitorCpu = value; 
                    OnPropertyChanged(nameof(MonitorCpu)); 
                } 
            } 
        }

        public bool MonitorRam 
        { 
            get => monitorRam; 
            set 
            { 
                if (monitorRam != value) 
                { 
                    monitorRam = value; 
                    OnPropertyChanged(nameof(MonitorRam)); 
                } 
            } 
        }

        public bool MonitorGpuCore 
        { 
            get => monitorGpuCore; 
            set 
            { 
                if (monitorGpuCore != value) 
                { 
                    monitorGpuCore = value; 
                    OnPropertyChanged(nameof(MonitorGpuCore)); 
                } 
            } 
        }

        public bool MonitorGpuVideo 
        { 
            get => monitorGpuVideo; 
            set 
            { 
                if (monitorGpuVideo != value) 
                { 
                    monitorGpuVideo = value; 
                    OnPropertyChanged(nameof(MonitorGpuVideo)); 
                } 
            } 
        }

        public bool MonitorGpuVram 
        { 
            get => monitorGpuVram; 
            set 
            { 
                if (monitorGpuVram != value) 
                { 
                    monitorGpuVram = value; 
                    OnPropertyChanged(nameof(MonitorGpuVram)); 
                } 
            } 
        }
        
        // Logging settings
        public bool LogToFile 
        { 
            get => logToFile; 
            set 
            { 
                if (logToFile != value) 
                { 
                    logToFile = value; 
                    OnPropertyChanged(nameof(LogToFile)); 
                } 
            } 
        }

        public string LogFileName 
        { 
            get => logFileName; 
            set 
            { 
                if (logFileName != value) 
                { 
                    logFileName = value; 
                    OnPropertyChanged(nameof(LogFileName)); 
                } 
            } 
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MonitoringSettings()
        {
            // Default values
            DurationSeconds = 0; // Infinite
            IntervalMilliseconds = 100;
            MonitorCpu = true;
            MonitorRam = true;
            MonitorGpuCore = true;
            MonitorGpuVideo = true;
            MonitorGpuVram = true;
            LogToFile = true;
            
            // Set default log file to logs directory
            string logsDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
            LogFileName = Path.Combine(logsDirectory, $"Procmon-{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        /// <summary>
        /// Validates the monitoring settings
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            if (DurationSeconds < 0)
            {
                errorMessage = "Duration must be 0 or positive (0 for infinite)";
                return false;
            }

            if (IntervalMilliseconds <= 0)
            {
                errorMessage = "Interval must be positive";
                return false;
            }

            if (!HasAnySensorEnabled())
            {
                errorMessage = "At least one sensor must be enabled";
                return false;
            }

            if (LogToFile && string.IsNullOrWhiteSpace(LogFileName))
            {
                errorMessage = "Log file name is required when logging is enabled";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Checks if any sensor is enabled for monitoring
        /// </summary>
        public bool HasAnySensorEnabled()
        {
            return MonitorCpu || MonitorRam || MonitorGpuCore || MonitorGpuVideo || MonitorGpuVram;
        }

        /// <summary>
        /// Gets the monitoring duration as TimeSpan
        /// </summary>
        public TimeSpan GetDuration()
        {
            return IsInfiniteMode ? TimeSpan.Zero : TimeSpan.FromSeconds(DurationSeconds);
        }

        /// <summary>
        /// Gets the monitoring interval as TimeSpan
        /// </summary>
        public TimeSpan GetInterval()
        {
            return TimeSpan.FromMilliseconds(IntervalMilliseconds);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}