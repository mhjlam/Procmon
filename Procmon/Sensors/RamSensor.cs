using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Procmon.Sensors
{
    /// <summary>
    /// Sensor for monitoring RAM usage of a specific process
    /// </summary>
    public class RamSensor : IDisposable
    {
        private readonly Process process;
        private readonly PerformanceCounter totalMemoryCounter;

        public string Name => "RAM Usage";
        
        /// <summary>
        /// Total system RAM in MB
        /// </summary>
        public float TotalRam { get; private set; }

        public RamSensor(Process process)
        {
            this.process = process ?? throw new ArgumentNullException(nameof(process));
            
            // Get total system RAM using performance counter
            try
            {
                totalMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                // Make initial call to get available memory
                float availableMB = totalMemoryCounter.NextValue();
                // Estimate total RAM (this is a rough estimate)
                TotalRam = availableMB + 2048f; // Add some buffer for used memory
            }
            catch
            {
                TotalRam = 8192f; // Default to 8GB if we can't determine actual RAM
                totalMemoryCounter = null;
            }
        }

        /// <summary>
        /// Get the current RAM usage in MB
        /// </summary>
        public float NextValue()
        {
            try
            {
                if (process.HasExited)
                    return 0f;

                // Get working set (physical memory usage) in bytes, convert to MB
                return (float)(process.WorkingSet64 / (1024 * 1024));
            }
            catch
            {
                return 0f;
            }
        }

        public void Dispose()
        {
            totalMemoryCounter?.Dispose();
        }
    }
}