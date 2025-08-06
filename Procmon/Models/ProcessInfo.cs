using System.Diagnostics;

namespace Procmon.Models
{
    /// <summary>
    /// Represents information about a system process for UI display
    /// </summary>
    public class ProcessInfo
    {
        public Process Process { get; set; }
        public string DisplayName { get; set; }
        public string ProcessName { get; set; }
        public string WindowTitle { get; set; }

        public ProcessInfo()
        {
        }

        public ProcessInfo(Process process)
        {
            Process = process;
            ProcessName = process.ProcessName;
            WindowTitle = process.MainWindowTitle;
            DisplayName = $"{ProcessName} - {WindowTitle}";
        }

        /// <summary>
        /// Checks if the underlying process is still running
        /// </summary>
        public bool IsRunning()
        {
            try
            {
                return Process != null && !Process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the process ID safely
        /// </summary>
        public int GetProcessId()
        {
            try
            {
                return Process?.Id ?? -1;
            }
            catch
            {
                return -1;
            }
        }
    }
}