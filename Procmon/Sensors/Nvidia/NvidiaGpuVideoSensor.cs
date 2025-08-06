using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Procmon.Sensors.Nvidia
{
    /// <summary>
    /// NVIDIA GPU Video Engine sensor using NVML
    /// </summary>
    public class NvidiaGpuVideoSensor : IDisposable
    {
        private uint deviceIndex = 0;
        private IntPtr deviceHandle = IntPtr.Zero;
        private bool nvmlInitialized = false;
        private bool disposed = false;
        
        public string Name => "NVIDIA GPU Video";

        public NvidiaGpuVideoSensor()
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
        /// Get the current GPU Video Engine usage as a percentage
        /// </summary>
        public float NextValue()
        {
            if (nvmlInitialized && deviceHandle != IntPtr.Zero)
            {
                return GetNvmlVideoUsage();
            }
            else
            {
                // Fallback to performance counters
                return GetPerformanceCounterVideoUsage();
            }
        }

        private float GetNvmlVideoUsage()
        {
            try
            {
                // NVML doesn't have direct video engine utilization, so we'll use encoder/decoder stats
                var result = NvmlInterop.nvmlDeviceGetEncoderUtilization(deviceHandle, out uint encoderUtil, out uint samplingPeriod);
                if (result == NvmlReturn.NVML_SUCCESS)
                {
                    return encoderUtil;
                }
            }
            catch
            {
                // Fall through to return 0
            }
            return 0f;
        }

        private float GetPerformanceCounterVideoUsage()
        {
            try
            {
                using (var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_engtype_VideoEncode"))
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