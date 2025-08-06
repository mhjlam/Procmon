using System;
using System.Diagnostics;

namespace Procmon.Sensors
{
    /// <summary>
    /// Sensor for monitoring GPU VRAM usage
    /// </summary>
    public class GpuVramSensor
    {
        private readonly PerformanceCounter counter;
        
        public string Name => "GPU VRAM";
        
        /// <summary>
        /// Total VRAM in MB (estimated)
        /// </summary>
        public float TotalVram { get; private set; }

        public GpuVramSensor()
        {
            try
            {
                // Try to create a performance counter for GPU memory usage
                counter = new PerformanceCounter("GPU Process Memory", "Local Usage", "_Total");
                
                // Estimate total VRAM (this is a rough estimate)
                TotalVram = 4096f; // Default to 4GB, could be improved with WMI queries
            }
            catch
            {
                counter = null;
                TotalVram = 4096f;
            }
        }

        /// <summary>
        /// Get the current GPU VRAM usage in MB
        /// </summary>
        public float NextValue()
        {
            try
            {
                if (counter != null)
                {
                    // Convert from bytes to MB
                    return (float)(counter.NextValue() / (1024 * 1024));
                }
                else
                {
                    // Return a simulated value if GPU counters are not available
                    return 0f;
                }
            }
            catch
            {
                return 0f;
            }
        }

        public void Dispose()
        {
            counter?.Dispose();
        }
    }
}