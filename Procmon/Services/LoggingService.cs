using System;
using System.IO;
using System.Text;
using Procmon.Models;

namespace Procmon.Services
{
    /// <summary>
    /// Service for handling file logging operations
    /// </summary>
    public class LoggingService : IDisposable
    {
        private StreamWriter logWriter;
        private bool disposed = false;
        private readonly MonitoringSettings settings;

        public bool IsLoggingActive => logWriter != null;
        public string CurrentLogFile { get; private set; }

        public LoggingService(MonitoringSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Initialize logging with the specified filename and process name
        /// </summary>
        public void InitializeLogging(string processName)
        {
            if (!settings.LogToFile)
                return;

            try
            {
                string fileName = settings.LogFileName;
                
                // Ensure .csv extension
                if (!Path.HasExtension(fileName) || Path.GetExtension(fileName) != ".csv")
                    fileName += ".csv";

                // Update filename with process name and timestamp if not already included
                string directory = Path.GetDirectoryName(fileName) ?? "";
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                
                // Add process name and timestamp to filename if not already present
                if (!fileNameWithoutExt.Contains(processName) || !fileNameWithoutExt.Contains(DateTime.Now.ToString("yyyyMMdd")))
                {
                    fileName = Path.Combine(directory, $"{fileNameWithoutExt}-{processName}-{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
                }

                CurrentLogFile = fileName;
                logWriter = new StreamWriter(fileName, false, Encoding.UTF8);
                WriteLogHeader();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize logging: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Write the CSV header based on enabled sensors
        /// </summary>
        private void WriteLogHeader()
        {
            if (logWriter == null) return;

            var header = "\"Timestamp\"";
            
            if (settings.MonitorCpu)
                header += ",\"CPU Load (%)\"";
            
            if (settings.MonitorRam)
            {
                header += ",\"RAM Usage (MB)\"";
                header += ",\"RAM Usage (%)\"";
            }
            
            if (settings.MonitorGpuCore)
                header += ",\"GPU Core Load (%)\"";
            
            if (settings.MonitorGpuVideo)
                header += ",\"GPU Video Engine (%)\"";
            
            if (settings.MonitorGpuVram)
            {
                header += ",\"GPU VRAM Usage (MB)\"";
                header += ",\"GPU VRAM Usage (%)\"";
            }

            logWriter.WriteLine(header);
            logWriter.Flush();
        }

        /// <summary>
        /// Write a performance data point to the log file
        /// </summary>
        public void LogDataPoint(PerformanceDataPoint dataPoint)
        {
            if (logWriter == null || dataPoint == null) return;

            try
            {
                var logLine = $"\"{dataPoint.Timestamp}\"";

                if (settings.MonitorCpu)
                    logLine += $",\"{dataPoint.CpuPercent}\"";

                if (settings.MonitorRam)
                {
                    logLine += $",\"{dataPoint.RamMB}\"";
                    logLine += $",\"{dataPoint.RamPercent}\"";
                }

                if (settings.MonitorGpuCore)
                    logLine += $",\"{dataPoint.GpuCorePercent}\"";

                if (settings.MonitorGpuVideo)
                    logLine += $",\"{dataPoint.GpuVideoPercent}\"";

                if (settings.MonitorGpuVram)
                {
                    logLine += $",\"{dataPoint.GpuVramMB}\"";
                    logLine += $",\"{dataPoint.GpuVramPercent}\"";
                }

                logWriter.WriteLine(logLine);
                logWriter.Flush();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to write data point to log: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Close the current log file
        /// </summary>
        public void CloseLog()
        {
            try
            {
                logWriter?.Close();
                logWriter?.Dispose();
                logWriter = null;
                CurrentLogFile = null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to close log file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate that the log file can be created
        /// </summary>
        public static bool ValidateLogFile(string fileName, out string errorMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    errorMessage = "Log file name cannot be empty";
                    return false;
                }

                var fileInfo = new FileInfo(fileName);
                var directory = fileInfo.DirectoryName;
                
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Invalid log file path: {ex.Message}";
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    CloseLog();
                }
                disposed = true;
            }
        }

        ~LoggingService()
        {
            Dispose(false);
        }
    }
}