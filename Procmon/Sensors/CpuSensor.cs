using System;
using System.Diagnostics;

namespace Procmon.Sensors
{
    /// <summary>
    /// Sensor for monitoring CPU usage of a specific process
    /// </summary>
    public class CpuSensor
    {
        private readonly Process process;
        private readonly PerformanceCounter counter;
        private DateTime lastTime;
        private TimeSpan lastTotalProcessorTime;
        private bool firstMeasurement = true;

        public string Name => "CPU Load";

        public CpuSensor(Process process)
        {
            this.process = process ?? throw new ArgumentNullException(nameof(process));
            
            try
            {
                // Try to create a performance counter for the specific process
                counter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
            }
            catch
            {
                // If that fails, we'll use the manual calculation method
                counter = null;
            }
            
            lastTime = DateTime.UtcNow;
            try
            {
                lastTotalProcessorTime = process.TotalProcessorTime;
            }
            catch
            {
                lastTotalProcessorTime = TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Get the next CPU usage value as a percentage
        /// </summary>
        public float NextValue()
        {
            try
            {
                if (counter != null)
                {
                    // Use performance counter if available
                    return counter.NextValue();
                }
                else
                {
                    // Manual calculation method
                    if (process.HasExited)
                        return 0f;

                    var currentTime = DateTime.UtcNow;
                    var currentTotalProcessorTime = process.TotalProcessorTime;

                    if (firstMeasurement)
                    {
                        firstMeasurement = false;
                        lastTime = currentTime;
                        lastTotalProcessorTime = currentTotalProcessorTime;
                        return 0f;
                    }

                    var cpuUsedMs = (currentTotalProcessorTime - lastTotalProcessorTime).TotalMilliseconds;
                    var totalMsPassed = (currentTime - lastTime).TotalMilliseconds;
                    var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                    lastTime = currentTime;
                    lastTotalProcessorTime = currentTotalProcessorTime;

                    return (float)(cpuUsageTotal * 100);
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