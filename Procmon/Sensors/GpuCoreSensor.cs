using System;
using System.Diagnostics;

namespace Procmon.Sensors
{
    /// <summary>
    /// Sensor for monitoring GPU Core usage
    /// </summary>
    public class GpuCoreSensor
    {
        private readonly PerformanceCounter counter;
        
        public string Name => "GPU Core";

        public GpuCoreSensor()
        {
            try
            {
                // Try to create a performance counter for GPU usage
                // This may not work on all systems
                counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_engtype_3D");
            }
            catch
            {
                counter = null;
            }
        }

        /// <summary>
        /// Get the current GPU Core usage as a percentage
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