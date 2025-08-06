using System;
using System.Collections.Generic;
using System.Linq;
using Procmon.Models;

namespace Procmon.Helpers
{
    /// <summary>
    /// Helper class for calculating performance statistics
    /// </summary>
    public static class StatisticsHelper
    {
        /// <summary>
        /// Calculate statistics for CPU performance data
        /// </summary>
        public static PerformanceStatistics CalculateCpuStats(IEnumerable<PerformanceDataPoint> data)
        {
            var values = data.Select(d => d.CpuPercent).ToList();
            return CalculateStats(values, "%");
        }

        /// <summary>
        /// Calculate statistics for RAM performance data
        /// </summary>
        public static PerformanceStatistics CalculateRamStats(IEnumerable<PerformanceDataPoint> data)
        {
            var percentValues = data.Select(d => d.RamPercent).ToList();
            var mbValues = data.Select(d => d.RamMB).ToList();
            
            if (!percentValues.Any())
            {
                return new PerformanceStatistics
                {
                    Average = "0.0% (0 MB)",
                    Maximum = "0.0% (0 MB)",
                    Minimum = "0.0% (0 MB)"
                };
            }
            
            return new PerformanceStatistics
            {
                Average = $"{percentValues.Average():F1}% ({mbValues.Average():F0} MB)",
                Maximum = $"{percentValues.Max():F1}% ({mbValues.Max():F0} MB)",
                Minimum = $"{percentValues.Min():F1}% ({mbValues.Min():F0} MB)"
            };
        }

        /// <summary>
        /// Calculate statistics for GPU Core performance data
        /// </summary>
        public static PerformanceStatistics CalculateGpuCoreStats(IEnumerable<PerformanceDataPoint> data)
        {
            var values = data.Select(d => d.GpuCorePercent).ToList();
            return CalculateStats(values, "%");
        }

        /// <summary>
        /// Calculate statistics for GPU Video performance data
        /// </summary>
        public static PerformanceStatistics CalculateGpuVideoStats(IEnumerable<PerformanceDataPoint> data)
        {
            var values = data.Select(d => d.GpuVideoPercent).ToList();
            return CalculateStats(values, "%");
        }

        /// <summary>
        /// Calculate statistics for GPU VRAM performance data
        /// </summary>
        public static PerformanceStatistics CalculateGpuVramStats(IEnumerable<PerformanceDataPoint> data)
        {
            var percentValues = data.Select(d => d.GpuVramPercent).ToList();
            var mbValues = data.Select(d => d.GpuVramMB).ToList();
            
            if (!percentValues.Any())
            {
                return new PerformanceStatistics
                {
                    Average = "0.0% (0 MB)",
                    Maximum = "0.0% (0 MB)",
                    Minimum = "0.0% (0 MB)"
                };
            }
            
            return new PerformanceStatistics
            {
                Average = $"{percentValues.Average():F1}% ({mbValues.Average():F0} MB)",
                Maximum = $"{percentValues.Max():F1}% ({mbValues.Max():F0} MB)",
                Minimum = $"{percentValues.Min():F1}% ({mbValues.Min():F0} MB)"
            };
        }

        /// <summary>
        /// Helper method to calculate basic statistics
        /// </summary>
        private static PerformanceStatistics CalculateStats(List<float> values, string unit)
        {
            if (!values.Any())
            {
                return new PerformanceStatistics
                {
                    Average = $"0.0{unit}",
                    Maximum = $"0.0{unit}",
                    Minimum = $"0.0{unit}"
                };
            }
            
            return new PerformanceStatistics
            {
                Average = $"{values.Average():F1}{unit}",
                Maximum = $"{values.Max():F1}{unit}",
                Minimum = $"{values.Min():F1}{unit}"
            };
        }

        /// <summary>
        /// Calculate data collection rate
        /// </summary>
        public static double CalculateDataRate(int dataPointCount, TimeSpan elapsed)
        {
            if (elapsed.TotalMinutes <= 0) return 0;
            return dataPointCount / elapsed.TotalMinutes;
        }

        /// <summary>
        /// Format elapsed time for display
        /// </summary>
        public static string FormatElapsedTime(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1)
                return elapsed.ToString(@"hh\:mm\:ss");
            else
                return elapsed.ToString(@"mm\:ss");
        }

        /// <summary>
        /// Format remaining time for display
        /// </summary>
        public static string FormatRemainingTime(TimeSpan remaining)
        {
            if (remaining.TotalSeconds <= 0)
                return "Expired";
            
            if (remaining.TotalHours >= 1)
                return remaining.ToString(@"hh\:mm\:ss");
            else
                return remaining.ToString(@"mm\:ss");
        }
    }

    /// <summary>
    /// Represents calculated performance statistics
    /// </summary>
    public class PerformanceStatistics
    {
        public string Average { get; set; }
        public string Maximum { get; set; }
        public string Minimum { get; set; }
    }
}