using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Procmon.Sensors.Nvidia
{
    /// <summary>
    /// NVIDIA GPU VRAM sensor using NVML
    /// </summary>
    public class NvidiaGpuVramSensor : IDisposable
    {
        private uint deviceIndex = 0;
        private IntPtr deviceHandle = IntPtr.Zero;
        private bool nvmlInitialized = false;
        private bool disposed = false;
        
        public string Name => "NVIDIA GPU VRAM";
        public float TotalVram { get; private set; }

        public NvidiaGpuVramSensor()
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
                    TotalVram = 4096f; // Default fallback
                    return;
                }

                nvmlInitialized = true;

                // Get device handle for first GPU
                result = NvmlInterop.nvmlDeviceGetHandleByIndex_v2(deviceIndex, out deviceHandle);
                if (result != NvmlReturn.NVML_SUCCESS)
                {
                    deviceHandle = IntPtr.Zero;
                    TotalVram = 4096f; // Default fallback
                    return;
                }

                // Get total VRAM
                result = NvmlInterop.nvmlDeviceGetMemoryInfo(deviceHandle, out NvmlMemory memory);
                if (result == NvmlReturn.NVML_SUCCESS)
                {
                    TotalVram = (float)(memory.total / (1024 * 1024)); // Convert bytes to MB
                }
                else
                {
                    TotalVram = 4096f; // Default fallback
                }
            }
            catch
            {
                // NVML not available, will fall back to performance counters
                nvmlInitialized = false;
                deviceHandle = IntPtr.Zero;
                TotalVram = 4096f; // Default fallback
            }
        }

        /// <summary>
        /// Get the current GPU VRAM usage in MB
        /// </summary>
        public float NextValue()
        {
            if (nvmlInitialized && deviceHandle != IntPtr.Zero)
            {
                return GetNvmlVramUsage();
            }
            else
            {
                // Fallback to performance counters
                return GetPerformanceCounterVramUsage();
            }
        }

        private float GetNvmlVramUsage()
        {
            try
            {
                var result = NvmlInterop.nvmlDeviceGetMemoryInfo(deviceHandle, out NvmlMemory memory);
                if (result == NvmlReturn.NVML_SUCCESS)
                {
                    return (float)(memory.used / (1024 * 1024)); // Convert bytes to MB
                }
            }
            catch
            {
                // Fall through to return 0
            }
            return 0f;
        }

        private float GetPerformanceCounterVramUsage()
        {
            try
            {
                using (var counter = new PerformanceCounter("GPU Process Memory", "Local Usage", "_Total"))
                {
                    return (float)(counter.NextValue() / (1024 * 1024)); // Convert bytes to MB
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