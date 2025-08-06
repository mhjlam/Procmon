using System;
using System.Threading;
using System.Threading.Tasks;
using Procmon.Models;
using Procmon.Sensors;

namespace Procmon.Services
{
    /// <summary>
    /// Service for managing performance monitoring operations
    /// </summary>
    public class MonitoringService : IDisposable
    {
        private readonly MonitoringSettings settings;
        private CpuSensor cpuSensor;
        private RamSensor ramSensor;
        private GpuCoreSensor gpuSensor;
        private GpuVideoSensor videoSensor;
        private GpuVramSensor vramSensor;
        
        private bool disposed = false;
        private bool sensorsInitialized = false;

        public bool AreSensorsInitialized => sensorsInitialized;

        public MonitoringService(MonitoringSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Initialize all enabled sensors for the target process
        /// </summary>
        public async Task InitializeSensorsAsync(ProcessInfo targetProcess, CancellationToken cancellationToken = default)
        {
            if (targetProcess?.Process == null)
                throw new ArgumentException("Target process is required", nameof(targetProcess));

            if (!targetProcess.IsRunning())
                throw new InvalidOperationException("Target process is not running");

            try
            {
                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (settings.MonitorCpu)
                    {
                        cpuSensor = new CpuSensor(targetProcess.Process);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    
                    if (settings.MonitorRam)
                    {
                        ramSensor = new RamSensor(targetProcess.Process);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    
                    if (settings.MonitorGpuCore)
                    {
                        gpuSensor = new GpuCoreSensor();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    
                    if (settings.MonitorGpuVideo)
                    {
                        videoSensor = new GpuVideoSensor();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    
                    if (settings.MonitorGpuVram)
                    {
                        vramSensor = new GpuVramSensor();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    
                }, cancellationToken).ConfigureAwait(false);

                sensorsInitialized = true;
            }
            catch (OperationCanceledException)
            {
                CleanupSensors();
                throw;
            }
            catch (Exception ex)
            {
                CleanupSensors();
                throw new InvalidOperationException($"Failed to initialize sensors: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Collect performance data from all enabled sensors
        /// </summary>
        public PerformanceDataPoint CollectDataPoint(ProcessInfo targetProcess)
        {
            if (!sensorsInitialized)
                throw new InvalidOperationException("Sensors are not initialized");

            if (targetProcess?.Process == null)
                throw new ArgumentException("Target process is required", nameof(targetProcess));

            try
            {
                var dataPoint = new PerformanceDataPoint();

                // Refresh process information only once
                targetProcess.Process.Refresh();

                // Add null checks for all sensors to prevent race conditions during disposal
                if (settings.MonitorCpu && cpuSensor != null)
                    dataPoint.CpuPercent = cpuSensor.NextValue();

                if (settings.MonitorRam && ramSensor != null)
                {
                    dataPoint.RamMB = ramSensor.NextValue();
                    // Protect against division by zero and null reference
                    if (ramSensor.TotalRam > 0)
                        dataPoint.RamPercent = (dataPoint.RamMB / ramSensor.TotalRam) * 100f;
                }

                if (settings.MonitorGpuCore && gpuSensor != null)
                    dataPoint.GpuCorePercent = gpuSensor.NextValue();

                if (settings.MonitorGpuVideo && videoSensor != null)
                    dataPoint.GpuVideoPercent = videoSensor.NextValue();

                if (settings.MonitorGpuVram && vramSensor != null)
                {
                    dataPoint.GpuVramMB = vramSensor.NextValue();
                    // Protect against division by zero and null reference
                    if (vramSensor.TotalVram > 0)
                        dataPoint.GpuVramPercent = (dataPoint.GpuVramMB / vramSensor.TotalVram) * 100f;
                }

                return dataPoint;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to collect performance data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the total RAM available for percentage calculations
        /// </summary>
        public float GetTotalRam()
        {
            return ramSensor?.TotalRam ?? 0f;
        }

        /// <summary>
        /// Get the total VRAM available for percentage calculations
        /// </summary>
        public float GetTotalVram()
        {
            return vramSensor?.TotalVram ?? 0f;
        }

        /// <summary>
        /// Check if any sensors are enabled
        /// </summary>
        public bool HasEnabledSensors()
        {
            return settings.MonitorCpu || settings.MonitorRam || settings.MonitorGpuCore || 
                   settings.MonitorGpuVideo || settings.MonitorGpuVram;
        }

        /// <summary>
        /// Clean up sensor references (sensors don't implement IDisposable)
        /// </summary>
        private void CleanupSensors()
        {
            try
            {
                // Mark sensors as not initialized first to prevent new data collection attempts
                sensorsInitialized = false;
                
                // Note: The original sensor classes don't implement IDisposable
                // We just clear the references to allow garbage collection
                cpuSensor = null;
                ramSensor = null;
                gpuSensor = null;
                videoSensor = null;
                vramSensor = null;
            }
            catch
            {
                // Ignore cleanup errors
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
                    CleanupSensors();
                }
                disposed = true;
            }
        }

        ~MonitoringService()
        {
            Dispose(false);
        }
    }
}