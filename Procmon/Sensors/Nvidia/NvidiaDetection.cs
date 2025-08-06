using System;

namespace Procmon.Sensors.Nvidia
{
    /// <summary>
    /// Utility class to detect and validate NVIDIA GPU capabilities
    /// </summary>
    public static class NvidiaDetection
    {
        /// <summary>
        /// Check if NVIDIA GPU with NVML support is available
        /// </summary>
        public static bool IsNvidiaGpuAvailable()
        {
            try
            {
                var result = NvmlInterop.nvmlInit_v2();
                if (result == NvmlReturn.NVML_SUCCESS)
                {
                    // Try to get device count
                    var deviceResult = NvmlInterop.nvmlDeviceGetCount_v2(out uint deviceCount);
                    
                    // Shutdown NVML after test
                    NvmlInterop.nvmlShutdown();
                    
                    return deviceResult == NvmlReturn.NVML_SUCCESS && deviceCount > 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get NVIDIA GPU information for diagnostics
        /// </summary>
        public static string GetNvidiaGpuInfo()
        {
            try
            {
                var result = NvmlInterop.nvmlInit_v2();
                if (result != NvmlReturn.NVML_SUCCESS)
                {
                    return $"NVML initialization failed: {result}";
                }

                var deviceResult = NvmlInterop.nvmlDeviceGetCount_v2(out uint deviceCount);
                if (deviceResult != NvmlReturn.NVML_SUCCESS)
                {
                    NvmlInterop.nvmlShutdown();
                    return $"Failed to get device count: {deviceResult}";
                }

                if (deviceCount == 0)
                {
                    NvmlInterop.nvmlShutdown();
                    return "No NVIDIA GPUs detected";
                }

                // Get information for first GPU
                var handleResult = NvmlInterop.nvmlDeviceGetHandleByIndex_v2(0, out IntPtr deviceHandle);
                if (handleResult != NvmlReturn.NVML_SUCCESS)
                {
                    NvmlInterop.nvmlShutdown();
                    return $"Failed to get device handle: {handleResult}";
                }

                // Get GPU name
                var nameBuffer = new System.Text.StringBuilder(256);
                var nameResult = NvmlInterop.nvmlDeviceGetName(deviceHandle, nameBuffer, 256);
                string gpuName = nameResult == NvmlReturn.NVML_SUCCESS ? nameBuffer.ToString() : "Unknown";

                // Get memory info
                var memoryResult = NvmlInterop.nvmlDeviceGetMemoryInfo(deviceHandle, out NvmlMemory memory);
                string memoryInfo = memoryResult == NvmlReturn.NVML_SUCCESS 
                    ? $"VRAM: {memory.total / (1024 * 1024)} MB" 
                    : "Memory info unavailable";

                NvmlInterop.nvmlShutdown();

                return $"NVIDIA GPU detected: {gpuName} ({memoryInfo})";
            }
            catch (Exception ex)
            {
                return $"Error detecting NVIDIA GPU: {ex.Message}";
            }
        }
    }
}