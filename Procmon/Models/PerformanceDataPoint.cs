using System;

namespace Procmon.Models
{
    /// <summary>
    /// Represents a single performance measurement point with timestamps and sensor values
    /// </summary>
    public class PerformanceDataPoint
    {
        public string Timestamp { get; set; }
        public float CpuPercent { get; set; }
        public float RamMB { get; set; }
        public float RamPercent { get; set; }
        public float GpuCorePercent { get; set; }
        public float GpuVideoPercent { get; set; }
        public float GpuVramMB { get; set; }
        public float GpuVramPercent { get; set; }

        public PerformanceDataPoint()
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        /// <summary>
        /// Create a data point with custom timestamp
        /// </summary>
        public PerformanceDataPoint(DateTime timestamp)
        {
            Timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        /// <summary>
        /// Gets all sensor values as an array for easy iteration
        /// </summary>
        public float[] GetAllValues()
        {
            return new float[] 
            { 
                CpuPercent, RamMB, RamPercent, 
                GpuCorePercent, GpuVideoPercent, GpuVramMB, GpuVramPercent 
            };
        }

        /// <summary>
        /// Gets percentage values for charting purposes
        /// </summary>
        public float[] GetPercentageValues()
        {
            return new float[] 
            { 
                CpuPercent, RamPercent, 
                GpuCorePercent, GpuVideoPercent, GpuVramPercent 
            };
        }
    }
}