using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Procmon.Models;

namespace Procmon.Services
{
    /// <summary>
    /// Service for managing system processes and process selection
    /// </summary>
    public class ProcessService
    {
        private readonly HashSet<string> systemProcesses;

        public ProcessService()
        {
            // List of Windows system processes to exclude from the process list
            systemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "csrss", "wininit", "winlogon", "services", "lsass", "svchost", "spoolsv", 
                "explorer", "dwm", "audiodg", "conhost", "smss", "winlogon", "lsm",
                "dllhost", "taskhost", "taskhostw", "RuntimeBroker", "SearchUI",
                "ShellExperienceHost", "ApplicationFrameHost", "SystemSettings",
                "WinStore.App", "backgroundTaskHost", "UserOOBEBroker", "LockApp",
                "TextInputHost"
            };
        }

        /// <summary>
        /// Gets a list of available processes suitable for monitoring
        /// </summary>
        public List<ProcessInfo> GetAvailableProcesses()
        {
            var currentProcessName = Process.GetCurrentProcess().ProcessName.ToLower();
            var processes = new List<ProcessInfo>();

            try
            {
                var systemProcesses = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                    .Where(p => !p.ProcessName.ToLower().Equals(currentProcessName))
                    .Where(p => !this.systemProcesses.Contains(p.ProcessName))
                    .OrderBy(p => p.ProcessName)
                    .ToList();

                foreach (var process in systemProcesses)
                {
                    try
                    {
                        // Try to access the process module to verify we have access
                        var processModule = process.MainModule;
                        processes.Add(new ProcessInfo(process));
                    }
                    catch (Exception)
                    {
                        // Skip processes we can't access
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve system processes", ex);
            }

            return processes;
        }

        /// <summary>
        /// Validates that a process is still available for monitoring
        /// </summary>
        public bool IsProcessValid(ProcessInfo processInfo)
        {
            if (processInfo?.Process == null)
                return false;

            try
            {
                return !processInfo.Process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Refreshes process information to get current state
        /// </summary>
        public void RefreshProcess(ProcessInfo processInfo)
        {
            if (processInfo?.Process == null)
                return;

            try
            {
                processInfo.Process.Refresh();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to refresh process {processInfo.ProcessName}", ex);
            }
        }

        /// <summary>
        /// Gets detailed information about a process
        /// </summary>
        public string GetProcessDetails(ProcessInfo processInfo)
        {
            if (processInfo?.Process == null)
                return "No process information available";

            try
            {
                var process = processInfo.Process;
                return $"Process: {process.ProcessName}\n" +
                       $"PID: {process.Id}\n" +
                       $"Window Title: {process.MainWindowTitle}\n" +
                       $"Start Time: {process.StartTime:yyyy-MM-dd HH:mm:ss}\n" +
                       $"Working Set: {process.WorkingSet64 / (1024 * 1024):F1} MB";
            }
            catch (Exception ex)
            {
                return $"Error retrieving process details: {ex.Message}";
            }
        }
    }
}