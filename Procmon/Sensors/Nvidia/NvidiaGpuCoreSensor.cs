using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Procmon.Sensors.Nvidia
{
    /// <summary>
    /// NVIDIA GPU Core sensor using NVML (NVIDIA Management Library)
    /// </summary>
    public class NvidiaGpuCoreSensor : IDisposable
    {
        private uint deviceIndex = 0;
        private IntPtr deviceHandle = IntPtr.Zero;
        private bool nvmlInitialized = false;
        private bool disposed = false;
        
        public string Name => "NVIDIA GPU Core";

        public NvidiaGpuCoreSensor()
        {
            InitializeNVML();
        }

        private void InitializeNVML()
        {
            try
            {
                // Initialize NVML
                var result = NvmlInterop.nvmlInit_v2();
                if (result != NvmlReturn.NVML_SUCCESS)
                {
                    return;
                }

                nvmlInitialized = true;

                // Get device handle for first GPU
                result = NvmlInterop.nvmlDeviceGetHandleByIndex_v2(deviceIndex, out deviceHandle);
                if (result != NvmlReturn.NVML_SUCCESS)
                {
                    deviceHandle = IntPtr.Zero;
                }
            }
            catch
            {
                // NVML not available, will fall back to performance counters
                nvmlInitialized = false;
                deviceHandle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Get the current GPU Core usage as a percentage
        /// </summary>
        public float NextValue()
        {
            if (nvmlInitialized && deviceHandle != IntPtr.Zero)
            {
                return GetNvmlGpuUsage();
            }
            else
            {
                // Fallback to performance counters
                return GetPerformanceCounterGpuUsage();
            }
        }

        private float GetNvmlGpuUsage()
        {
            try
            {
                var result = NvmlInterop.nvmlDeviceGetUtilizationRates(deviceHandle, out NvmlUtilization utilization);
                if (result == NvmlReturn.NVML_SUCCESS)
                {
                    return utilization.gpu;
                }
            }
            catch
            {
                // Fall through to return 0
            }
            return 0f;
        }

        private float GetPerformanceCounterGpuUsage()
        {
            try
            {
                using (var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_engtype_3D"))
                {
                    return counter.NextValue();
                }
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
                if (disposing && nvmlInitialized)
                {
                    try
                    {
                        NvmlInterop.nvmlShutdown();
                    }
                    catch
                    {
                        // Ignore errors during shutdown
                    }
                }
                disposed = true;
            }
        }
    }
}