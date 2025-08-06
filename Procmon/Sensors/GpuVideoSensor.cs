using System;
using System.Diagnostics;

namespace Procmon.Sensors
{
    /// <summary>
    /// Sensor for monitoring GPU Video Engine usage
    /// </summary>
    public class GpuVideoSensor
    {
        private readonly PerformanceCounter counter;
        
        public string Name => "GPU Video Engine";

        public GpuVideoSensor()
        {
            try
            {
                // Try to create a performance counter for GPU video engine usage
                counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_engtype_VideoEncode");
            }
            catch
            {
                counter = null;
            }
        }

        /// <summary>
        /// Get the current GPU Video Engine usage as a percentage
        /// </summary>
        public float NextValue()
        {
            try
            {
                if (counter != null)
                {
                    return counter.NextValue();
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