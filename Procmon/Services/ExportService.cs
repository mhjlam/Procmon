using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Procmon.Models;

namespace Procmon.Services
{
    /// <summary>
    /// Service for exporting performance data to various formats
    /// </summary>
    public class ExportService
    {
        /// <summary>
        /// Export data to CSV format
        /// </summary>
        public void ExportToCsv(string fileName, IEnumerable<PerformanceDataPoint> data)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                // Write header
                writer.WriteLine("Timestamp,CPU (%),RAM (MB),RAM (%),GPU Core (%),GPU Video (%),GPU VRAM (MB),GPU VRAM (%)");
                
                // Write data
                foreach (var dataPoint in data)
                {
                    writer.WriteLine($"{dataPoint.Timestamp},{dataPoint.CpuPercent},{dataPoint.RamMB}," +
                                   $"{dataPoint.RamPercent},{dataPoint.GpuCorePercent},{dataPoint.GpuVideoPercent}," +
                                   $"{dataPoint.GpuVramMB},{dataPoint.GpuVramPercent}");
                }
            }
        }

        /// <summary>
        /// Export data to JSON format
        /// </summary>
        public void ExportToJson(string fileName, IEnumerable<PerformanceDataPoint> data)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine("[");
                
                var dataList = data.ToList();
                for (int i = 0; i < dataList.Count; i++)
                {
                    var dataPoint = dataList[i];
                    writer.Write($"  {{");
                    writer.Write($"\"Timestamp\":\"{dataPoint.Timestamp}\",");
                    writer.Write($"\"CpuPercent\":{dataPoint.CpuPercent},");
                    writer.Write($"\"RamMB\":{dataPoint.RamMB},");
                    writer.Write($"\"RamPercent\":{dataPoint.RamPercent},");
                    writer.Write($"\"GpuCorePercent\":{dataPoint.GpuCorePercent},");
                    writer.Write($"\"GpuVideoPercent\":{dataPoint.GpuVideoPercent},");
                    writer.Write($"\"GpuVramMB\":{dataPoint.GpuVramMB},");
                    writer.Write($"\"GpuVramPercent\":{dataPoint.GpuVramPercent}");
                    writer.Write($"}}");
                    if (i < dataList.Count - 1) writer.Write(",");
                    writer.WriteLine();
                }
                
                writer.WriteLine("]");
            }
        }

        /// <summary>
        /// Export data to XML format
        /// </summary>
        public void ExportToXml(string fileName, IEnumerable<PerformanceDataPoint> data)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                writer.WriteLine("<PerformanceData>");
                
                foreach (var dataPoint in data)
                {
                    writer.WriteLine("  <DataPoint>");
                    writer.WriteLine($"    <Timestamp>{dataPoint.Timestamp}</Timestamp>");
                    writer.WriteLine($"    <CpuPercent>{dataPoint.CpuPercent}</CpuPercent>");
                    writer.WriteLine($"    <RamMB>{dataPoint.RamMB}</RamMB>");
                    writer.WriteLine($"    <RamPercent>{dataPoint.RamPercent}</RamPercent>");
                    writer.WriteLine($"    <GpuCorePercent>{dataPoint.GpuCorePercent}</GpuCorePercent>");
                    writer.WriteLine($"    <GpuVideoPercent>{dataPoint.GpuVideoPercent}</GpuVideoPercent>");
                    writer.WriteLine($"    <GpuVramMB>{dataPoint.GpuVramMB}</GpuVramMB>");
                    writer.WriteLine($"    <GpuVramPercent>{dataPoint.GpuVramPercent}</GpuVramPercent>");
                    writer.WriteLine("  </DataPoint>");
                }
                
                writer.WriteLine("</PerformanceData>");
            }
        }

        /// <summary>
        /// Export data to plain text format
        /// </summary>
        public void ExportToText(string fileName, IEnumerable<PerformanceDataPoint> data)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var dataList = data.ToList();
            
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine("Process Performance Monitor Export");
                writer.WriteLine("=================================");
                writer.WriteLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"Total Data Points: {dataList.Count}");
                writer.WriteLine();
                
                writer.WriteLine("Timestamp".PadRight(25) + "CPU%".PadRight(8) + "RAM MB".PadRight(10) + "RAM%".PadRight(8) + 
                                "GPU Core%".PadRight(12) + "GPU Video%".PadRight(12) + "GPU VRAM MB".PadRight(12) + "GPU VRAM%");
                writer.WriteLine(new string('-', 90));
                
                foreach (var dataPoint in dataList)
                {
                    writer.WriteLine($"{dataPoint.Timestamp.PadRight(25)}{dataPoint.CpuPercent:F1}".PadRight(8) +
                                   $"{dataPoint.RamMB:F0}".PadRight(10) + $"{dataPoint.RamPercent:F1}".PadRight(8) +
                                   $"{dataPoint.GpuCorePercent:F1}".PadRight(12) + $"{dataPoint.GpuVideoPercent:F1}".PadRight(12) +
                                   $"{dataPoint.GpuVramMB:F0}".PadRight(12) + $"{dataPoint.GpuVramPercent:F1}");
                }
            }
        }

        /// <summary>
        /// Export data to the appropriate format based on file extension
        /// </summary>
        public void ExportData(string fileName, IEnumerable<PerformanceDataPoint> data)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            var extension = Path.GetExtension(fileName).ToLower();
            
            switch (extension)
            {
                case ".csv":
                    ExportToCsv(fileName, data);
                    break;
                case ".json":
                    ExportToJson(fileName, data);
                    break;
                case ".xml":
                    ExportToXml(fileName, data);
                    break;
                case ".txt":
                    ExportToText(fileName, data);
                    break;
                default:
                    // Default to CSV if extension is unknown
                    ExportToCsv(fileName, data);
                    break;
            }
        }

        /// <summary>
        /// Gets supported export formats
        /// </summary>
        public string GetSupportedFormatsFilter()
        {
            return "CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|XML files (*.xml)|*.xml|Text files (*.txt)|*.txt";
        }

        /// <summary>
        /// Generate default export filename with timestamp
        /// </summary>
        public string GenerateDefaultFileName(string extension = "csv")
        {
            return $"Procmon-Export-{DateTime.Now:yyyyMMdd_HHmmss}.{extension.TrimStart('.')}";
        }
    }
}