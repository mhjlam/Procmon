using System;
using System.Diagnostics;
using Procmon.Sensors.Nvidia;

namespace Procmon.Sensors
{
    /// <summary>
    /// Sensor for monitoring GPU Video Engine usage with NVIDIA GPU priority
    /// </summary>
    public class GpuVideoSensor : IDisposable
    {
        private readonly PerformanceCounter counter;
        private readonly NvidiaGpuVideoSensor nvidiaVideoSensor;
        private bool disposed = false;
        
        public string Name => nvidiaVideoSensor?.Name ?? "GPU Video Engine";

        public GpuVideoSensor()
        {
            // Try NVIDIA sensor first
            try
            {
                nvidiaVideoSensor = new NvidiaGpuVideoSensor();
            }
            catch
            {
                nvidiaVideoSensor = null;
            }

            // Fallback to performance counter if NVIDIA sensor fails
            if (nvidiaVideoSensor == null)
            {
                try
                {
                    counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_engtype_VideoEncode");
                }
                catch
                {
                    counter = null;
                }
            }
        }

        /// <summary>
        /// Get the current GPU Video Engine usage as a percentage
        /// </summary>
        public float NextValue()
        {
            try
            {
                // Prefer NVIDIA sensor if available
                if (nvidiaVideoSensor != null)
                {
                    return nvidiaVideoSensor.NextValue();
                }
                
                // Fallback to performance counter
                if (counter != null)
                {
                    return counter.NextValue();
                }
                
                return 0f;
            }
            catch
            {
                return 0f;
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
                    nvidiaVideoSensor?.Dispose();
                    counter?.Dispose();
                }
                disposed = true;
            }
        }
    }
}