using System;
using System.Diagnostics;
using Procmon.Sensors.Nvidia;

namespace Procmon.Sensors
{
    /// <summary>
    /// Sensor for monitoring GPU VRAM usage with NVIDIA GPU priority
    /// </summary>
    public class GpuVramSensor : IDisposable
    {
        private readonly PerformanceCounter counter;
        private readonly NvidiaGpuVramSensor nvidiaVramSensor;
        private bool disposed = false;
        
        public string Name => nvidiaVramSensor?.Name ?? "GPU VRAM";
        
        /// <summary>
        /// Total VRAM in MB
        /// </summary>
        public float TotalVram { get; private set; }

        public GpuVramSensor()
        {
            // Try NVIDIA sensor first
            try
            {
                nvidiaVramSensor = new NvidiaGpuVramSensor();
                TotalVram = nvidiaVramSensor.TotalVram;
            }
            catch
            {
                nvidiaVramSensor = null;
            }

            // Fallback to performance counter if NVIDIA sensor fails
            if (nvidiaVramSensor == null)
            {
                try
                {
                    counter = new PerformanceCounter("GPU Process Memory", "Local Usage", "_Total");
                    TotalVram = 4096f; // Default estimate
                }
                catch
                {
                    counter = null;
                    TotalVram = 4096f; // Default estimate
                }
            }
        }

        /// <summary>
        /// Get the current GPU VRAM usage in MB
        /// </summary>
        public float NextValue()
        {
            try
            {
                // Prefer NVIDIA sensor if available
                if (nvidiaVramSensor != null)
                {
                    return nvidiaVramSensor.NextValue();
                }
                
                // Fallback to performance counter
                if (counter != null)
                {
                    return (float)(counter.NextValue() / (1024 * 1024)); // Convert bytes to MB
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
                    nvidiaVramSensor?.Dispose();
                    counter?.Dispose();
                }
                disposed = true;
            }
        }
    }
}