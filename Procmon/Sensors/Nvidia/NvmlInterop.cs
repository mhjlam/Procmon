using System;
using System.Runtime.InteropServices;

namespace Procmon.Sensors.Nvidia
{
    /// <summary>
    /// NVML (NVIDIA Management Library) return codes
    /// </summary>
    public enum NvmlReturn : uint
    {
        NVML_SUCCESS = 0,
        NVML_ERROR_UNINITIALIZED = 1,
        NVML_ERROR_INVALID_ARGUMENT = 2,
        NVML_ERROR_NOT_SUPPORTED = 3,
        NVML_ERROR_NO_PERMISSION = 4,
        NVML_ERROR_ALREADY_INITIALIZED = 5,
        NVML_ERROR_NOT_FOUND = 6,
        NVML_ERROR_INSUFFICIENT_SIZE = 7,
        NVML_ERROR_INSUFFICIENT_POWER = 8,
        NVML_ERROR_DRIVER_NOT_LOADED = 9,
        NVML_ERROR_TIMEOUT = 10,
        NVML_ERROR_IRQ_ISSUE = 11,
        NVML_ERROR_LIBRARY_NOT_FOUND = 12,
        NVML_ERROR_FUNCTION_NOT_FOUND = 13,
        NVML_ERROR_CORRUPTED_INFOROM = 14,
        NVML_ERROR_GPU_IS_LOST = 15,
        NVML_ERROR_RESET_REQUIRED = 16,
        NVML_ERROR_OPERATING_SYSTEM = 17,
        NVML_ERROR_LIB_RM_VERSION_MISMATCH = 18,
        NVML_ERROR_IN_USE = 19,
        NVML_ERROR_MEMORY = 20,
        NVML_ERROR_NO_DATA = 21,
        NVML_ERROR_VGPU_ECC_NOT_SUPPORTED = 22,
        NVML_ERROR_INSUFFICIENT_RESOURCES = 23,
        NVML_ERROR_FREQ_NOT_SUPPORTED = 24,
        NVML_ERROR_ARGUMENT_VERSION_MISMATCH = 25,
        NVML_ERROR_DEPRECATED = 26,
        NVML_ERROR_UNKNOWN = 999
    }

    /// <summary>
    /// GPU utilization information
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NvmlUtilization
    {
        public uint gpu;    // GPU utilization percentage
        public uint memory; // Memory utilization percentage
    }

    /// <summary>
    /// GPU memory information
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NvmlMemory
    {
        public ulong total; // Total memory in bytes
        public ulong free;  // Free memory in bytes
        public ulong used;  // Used memory in bytes
    }

    /// <summary>
    /// NVML interop wrapper for NVIDIA GPU monitoring
    /// </summary>
    public static class NvmlInterop
    {
        private const string NVML_DLL = "nvml.dll";

        [DllImport(NVML_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern NvmlReturn nvmlInit_v2();

        [DllImport(NVML_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern NvmlReturn nvmlShutdown();

        [DllImport(NVML_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern NvmlReturn nvmlDeviceGetCount_v2(out uint deviceCount);

        [DllImport(NVML_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern NvmlReturn nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

        [DllImport(NVML_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern NvmlReturn nvmlDeviceGetName(IntPtr device, [MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder name, uint length);

        [DllImport(NVML_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern NvmlReturn nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

        [DllImport(NVML_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern NvmlReturn nvmlDeviceGetMemoryInfo(IntPtr device, out NvmlMemory memory);

        [DllImport(NVML_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern NvmlReturn nvmlDeviceGetEncoderUtilization(IntPtr device, out uint encoderUtil, out uint samplingPeriod);

        [DllImport(NVML_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern NvmlReturn nvmlDeviceGetDecoderUtilization(IntPtr device, out uint decoderUtil, out uint samplingPeriod);
    }
}