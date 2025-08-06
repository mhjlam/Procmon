using System;
using System.Diagnostics;
using Procmon.Sensors.Nvidia;

namespace Procmon.Sensors
{
    /// <summary>
    /// Sensor for monitoring GPU Core usage with NVIDIA GPU priority
    /// </summary>
    public class GpuCoreSensor : IDisposable
    {
        private readonly PerformanceCounter counter;
        private readonly NvidiaGpuCoreSensor nvidiaCoreSensor;
        private bool disposed = false;
        
        public string Name => nvidiaCoreSensor?.Name ?? "GPU Core";

        public GpuCoreSensor()
        {
            // Try NVIDIA sensor first
            try
            {
                nvidiaCoreSensor = new NvidiaGpuCoreSensor();
            }
            catch
            {
                nvidiaCoreSensor = null;
            }

            // Fallback to performance counter if NVIDIA sensor fails
            if (nvidiaCoreSensor == null)
            {
                try
                {
                    counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_engtype_3D");
                }
                catch
                {
                    counter = null;
                }
            }
        }

        /// <summary>
        /// Get the current GPU Core usage as a percentage
        /// </summary>
        public float NextValue()
        {
            try
            {
                // Prefer NVIDIA sensor if available
                if (nvidiaCoreSensor != null)
                {
                    return nvidiaCoreSensor.NextValue();
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
                    nvidiaCoreSensor?.Dispose();
                    counter?.Dispose();
                }
                disposed = true;
            }
        }
    }
}