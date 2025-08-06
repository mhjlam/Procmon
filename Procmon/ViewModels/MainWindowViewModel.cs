using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Procmon.Models;
using Procmon.Services;
using Procmon.Helpers;

namespace Procmon.ViewModels
{
    /// <summary>
    /// ViewModel for the main window, handling all business logic and data binding
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Private Fields
        private readonly ProcessService processService;
        private readonly ExportService exportService;
        private MonitoringService monitoringService;
        private LoggingService loggingService;
        
        private DispatcherTimer monitoringTimer;
        private DispatcherTimer uiUpdateTimer;
        private DateTime startTime;
        private bool isPaused;
        private bool isMonitoring;
        private CancellationTokenSource cancellationTokenSource;
        private ProcessInfo selectedProcess;
        private bool disposed = false;

        // Chart data management
        private const int MAX_CHART_POINTS = 200;
        #endregion

        #region Public Properties
        public ObservableCollection<PerformanceDataPoint> PerformanceData { get; }
        public ObservableCollection<ProcessInfo> AvailableProcesses { get; }
        public MonitoringSettings Settings { get; }

        public ProcessInfo SelectedProcess
        {
            get => selectedProcess;
            set
            {
                if (selectedProcess != value)
                {
                    selectedProcess = value;
                    
                    // Update log file name timestamp when changing process
                    if (!isMonitoring)
                        UpdateLogFileTimestamp();
                    
                    OnPropertyChanged(nameof(SelectedProcess));
                    // Notify that command states may have changed
                    OnPropertyChanged(nameof(CanStart));
                }
            }
        }

        private string statusText = "Ready";
        public string StatusText
        {
            get => statusText;
            set
            {
                statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        private string timeRemainingText = "";
        public string TimeRemainingText
        {
            get => timeRemainingText;
            set
            {
                timeRemainingText = value;
                OnPropertyChanged(nameof(TimeRemainingText));
            }
        }

        private string currentValuesText = "Ready to monitor";
        public string CurrentValuesText
        {
            get => currentValuesText;
            set
            {
                currentValuesText = value;
                OnPropertyChanged(nameof(CurrentValuesText));
            }
        }

        private double monitoringProgress = 0;
        public double MonitoringProgress
        {
            get => monitoringProgress;
            set
            {
                monitoringProgress = value;
                OnPropertyChanged(nameof(MonitoringProgress));
            }
        }

        private bool progressVisible = false;
        public bool ProgressVisible
        {
            get => progressVisible;
            set
            {
                progressVisible = value;
                OnPropertyChanged(nameof(ProgressVisible));
            }
        }

        // Command states
        public bool CanStart => !isMonitoring && SelectedProcess != null;
        public bool CanPause => isMonitoring;
        public bool CanStop => isMonitoring;
        public bool CanClearData => !isMonitoring && PerformanceData.Count > 0;
        public bool CanExportData => (!isMonitoring || isPaused) && PerformanceData.Count > 0;
        public bool CanConfigureSettings => !isMonitoring;
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<PerformanceDataPoint> DataPointCollected;
        public event EventHandler MonitoringStarted;
        public event EventHandler MonitoringStopped;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler DataCleared;
        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            processService = new ProcessService();
            exportService = new ExportService();
            
            PerformanceData = new ObservableCollection<PerformanceDataPoint>();
            AvailableProcesses = new ObservableCollection<ProcessInfo>();
            Settings = new MonitoringSettings();
            
            // Set default log file location to logs folder
            string logsDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
            string defaultLogFile = Path.Combine(logsDirectory, $"Procmon-{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            Settings.LogFileName = defaultLogFile;
            
            InitializeTimers();
            _ = RefreshProcessListAsync();
        }
        #endregion

        #region Public Methods
        
        public async Task RefreshProcessListAsync()
        {
            try
            {
                var processes = await Task.Run(() => processService.GetAvailableProcesses());
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableProcesses.Clear();
                    foreach (var process in processes)
                    {
                        AvailableProcesses.Add(process);
                    }
                    
                    if (AvailableProcesses.Any())
                    {
                        SelectedProcess = AvailableProcesses.First();
                    }
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to refresh process list: {ex.Message}");
            }
        }

        public async Task<bool> StartMonitoringAsync()
        {
            if (isMonitoring || SelectedProcess == null) return false;

            try
            {
                // Validate settings
                if (!Settings.IsValid(out string errorMessage))
                {
                    OnErrorOccurred(errorMessage);
                    return false;
                }

                // Check if process is still running
                if (!processService.IsProcessValid(SelectedProcess))
                {
                    OnErrorOccurred("Selected process is no longer running");
                    await RefreshProcessListAsync();
                    return false;
                }

                // *** IMMEDIATELY SET MONITORING STATE TO DISABLE CONFIGURATION ***
                // This prevents users from changing settings during initialization
                isMonitoring = true;
                StatusText = "Initializing...";
                
                // Immediately notify UI that configuration should be disabled
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanConfigureSettings));

                cancellationTokenSource = new CancellationTokenSource();

                // Initialize services
                monitoringService = new MonitoringService(Settings);
                loggingService = new LoggingService(Settings);

                // Initialize sensors
                await monitoringService.InitializeSensorsAsync(SelectedProcess, cancellationTokenSource.Token);
                
                // Initialize logging
                if (Settings.LogToFile)
                {
                    loggingService.InitializeLogging(SelectedProcess.ProcessName);
                }

                // Start monitoring
                startTime = DateTime.Now;
                isPaused = false;

                monitoringTimer.Interval = Settings.GetInterval();
                monitoringTimer.Start();
                uiUpdateTimer.Start();

                ProgressVisible = !Settings.IsInfiniteMode;
                StatusText = Settings.IsInfiniteMode ? 
                    $"Monitoring {SelectedProcess.ProcessName}" : 
                    $"Monitoring {SelectedProcess.ProcessName}";

                MonitoringStarted?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (OperationCanceledException)
            {
                // Reset state on cancellation
                isMonitoring = false;
                StatusText = "Monitoring cancelled";
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanConfigureSettings));
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to start monitoring: {ex.Message}");
                // Reset state on error and cleanup
                isMonitoring = false;
                StatusText = "Ready";
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanConfigureSettings));
                await StopMonitoringAsync();
                return false;
            }
        }

        public async Task StopMonitoringAsync()
        {
            if (!isMonitoring) return;

            try
            {
                // Stop timers first to prevent new data collection attempts
                monitoringTimer?.Stop();
                uiUpdateTimer?.Stop();
                
                // Set state flags
                isMonitoring = false;
                isPaused = false;
                
                // Cancel any ongoing operations
                cancellationTokenSource?.Cancel();
                
                // Wait a brief moment to allow any in-flight timer ticks to complete
                await Task.Delay(50);
                
                // Dispose services on background thread to avoid blocking UI
                await Task.Run(() =>
                {
                    try
                    {
                        loggingService?.Dispose();
                        monitoringService?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't throw to avoid breaking the stop operation
                        System.Diagnostics.Debug.WriteLine($"Error disposing services: {ex.Message}");
                    }
                });
                
                // Update log file timestamp for next session
                UpdateLogFileTimestamp();
                
                StatusText = "Stopped";
                TimeRemainingText = "";
                ProgressVisible = false;
                MonitoringProgress = 0;

                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanClearData));
                OnPropertyChanged(nameof(CanExportData));
                OnPropertyChanged(nameof(CanConfigureSettings));

                MonitoringStopped?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error stopping monitoring: {ex.Message}");
            }
            finally
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        public void PauseResumeMonitoring()
        {
            if (!isMonitoring) return;

            isPaused = !isPaused;
            StatusText = isPaused ? "Paused" : 
                $"Monitoring {SelectedProcess?.ProcessName}";

            OnPropertyChanged(nameof(CanExportData));
            OnPropertyChanged(nameof(StatusText)); // Notify UI that status changed
        }

        public void ClearData()
        {
            if (isMonitoring) return;

            PerformanceData.Clear();
            CurrentValuesText = "Data cleared";
            
            OnPropertyChanged(nameof(CanClearData));
            OnPropertyChanged(nameof(CanExportData));
            
            // Notify UI that data was cleared so charts and statistics can be cleared too
            DataCleared?.Invoke(this, EventArgs.Empty);
        }

        public async Task<bool> ExportDataAsync(string fileName = null)
        {
            if (PerformanceData.Count == 0) return false;

            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = exportService.GetSupportedFormatsFilter(),
                        DefaultExt = "csv",
                        FileName = exportService.GenerateDefaultFileName()
                    };

                    if (saveFileDialog.ShowDialog() != true)
                        return false;

                    fileName = saveFileDialog.FileName;
                }

                await Task.Run(() => exportService.ExportData(fileName, PerformanceData));
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to export data: {ex.Message}");
                return false;
            }
        }

        public bool ValidateLogFile(out string errorMessage)
        {
            return LoggingService.ValidateLogFile(Settings.LogFileName, out errorMessage);
        }

        public PerformanceStatistics GetCpuStatistics()
        {
            return StatisticsHelper.CalculateCpuStats(PerformanceData);
        }

        public PerformanceStatistics GetRamStatistics()
        {
            return StatisticsHelper.CalculateRamStats(PerformanceData);
        }

        public PerformanceStatistics GetGpuCoreStatistics()
        {
            return StatisticsHelper.CalculateGpuCoreStats(PerformanceData);
        }

        public PerformanceStatistics GetGpuVideoStatistics()
        {
            return StatisticsHelper.CalculateGpuVideoStats(PerformanceData);
        }

        public PerformanceStatistics GetGpuVramStatistics()
        {
            return StatisticsHelper.CalculateGpuVramStats(PerformanceData);
        }

        public void UpdateMonitoringInterval()
        {
            // Update monitoring timer interval if currently running
            if (isMonitoring && monitoringTimer != null)
            {
                monitoringTimer.Interval = Settings.GetInterval();
            }
        }

        /// <summary>
        /// Update the log file name with a new timestamp
        /// </summary>
        public void UpdateLogFileTimestamp()
        {
            if (!string.IsNullOrEmpty(Settings.LogFileName))
            {
                // Use logs folder relative to current directory
                string logsDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
                string extension = Path.GetExtension(Settings.LogFileName) ?? ".csv";
                
                // Extract process name from current filename if present
                string processName = SelectedProcess?.ProcessName ?? "unknown";
                
                // Generate new filename in logs folder
                string newFileName = $"Procmon-{processName}-{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                Settings.LogFileName = Path.Combine(logsDirectory, newFileName);
            }
        }

        /// <summary>
        /// Get the directory where log files are stored
        /// </summary>
        public string GetLogFileDirectory()
        {
            try
            {
                // If we have a current log file from logging service, use its directory
                if (loggingService?.CurrentLogFile != null)
                {
                    return Path.GetDirectoryName(loggingService.CurrentLogFile);
                }
                
                // Otherwise, use the configured log file's directory
                if (!string.IsNullOrEmpty(Settings.LogFileName))
                {
                    string directory = Path.GetDirectoryName(Settings.LogFileName);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        return Path.GetFullPath(directory);
                    }
                }
                
                // Default to logs folder
                string logsDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
                return logsDirectory;
            }
            catch
            {
                // Fallback to logs directory if there's any error
                return Path.Combine(Environment.CurrentDirectory, "logs");
            }
        }

        /// <summary>
        /// Open the folder containing log files
        /// </summary>
        public void OpenLogFolder()
        {
            try
            {
                string logDirectory = GetLogFileDirectory();
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                // Open the folder in Windows Explorer
                Process.Start("explorer.exe", $"\"{logDirectory}\"");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to open log folder: {ex.Message}");
            }
        }
        #endregion

        #region Private Methods
        
        private void InitializeTimers()
        {
            monitoringTimer = new DispatcherTimer();
            monitoringTimer.Tick += MonitoringTimer_Tick;
            
            uiUpdateTimer = new DispatcherTimer();
            uiUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            uiUpdateTimer.Tick += UiUpdateTimer_Tick;
        }

        private void MonitoringTimer_Tick(object sender, EventArgs e)
        {
            if (!isMonitoring || isPaused || !processService.IsProcessValid(SelectedProcess))
                return;

            try
            {
                // Double-check that monitoring service is still available (race condition protection)
                if (monitoringService?.AreSensorsInitialized != true)
                    return;
                    
                // Collect data point synchronously for consistent timing
                var dataPoint = monitoringService.CollectDataPoint(SelectedProcess);
                
                // Check again before updating UI (in case stop was called during data collection)
                if (!isMonitoring)
                    return;
                
                // Update UI directly on the UI thread (we're already on it since this is a DispatcherTimer)
                PerformanceData.Add(dataPoint);
                
                // Keep only last 1000 entries for performance
                if (PerformanceData.Count > 1000)
                    PerformanceData.RemoveAt(0);

                UpdateCurrentValuesDisplay(dataPoint);
                DataPointCollected?.Invoke(this, dataPoint);

                // Log data point if logging is enabled and still monitoring
                if (isMonitoring)
                    loggingService?.LogDataPoint(dataPoint);

                // Check if monitoring duration exceeded (only for timed monitoring)
                if (!Settings.IsInfiniteMode && isMonitoring)
                {
                    var elapsed = DateTime.Now - startTime;
                    if (elapsed.TotalSeconds >= Settings.DurationSeconds)
                    {
                        // Use BeginInvoke to avoid blocking the timer
                        Application.Current.Dispatcher.BeginInvoke(new Action(async () => await StopMonitoringAsync()));
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error collecting data: {ex.Message}");
            }
        }

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!isMonitoring || isPaused) return;

            var elapsed = DateTime.Now - startTime;

            if (Settings.IsInfiniteMode)
            {
                var dataRate = StatisticsHelper.CalculateDataRate(PerformanceData.Count, elapsed);
                TimeRemainingText = $"Elapsed: {StatisticsHelper.FormatElapsedTime(elapsed)} | Points: {PerformanceData.Count} | Rate: {dataRate:F1}/min";
            }
            else
            {
                var remaining = Settings.GetDuration() - elapsed;
                var dataRate = StatisticsHelper.CalculateDataRate(PerformanceData.Count, elapsed);

                if (remaining.TotalSeconds > 0)
                {
                    TimeRemainingText = $"Time: {StatisticsHelper.FormatRemainingTime(remaining)} | Points: {PerformanceData.Count} | Rate: {dataRate:F1}/min";
                    MonitoringProgress = (elapsed.TotalSeconds / Settings.DurationSeconds) * 100;
                }
                else
                {
                    TimeRemainingText = $"Time: Expired | Points: {PerformanceData.Count} | Rate: {dataRate:F1}/min";
                    MonitoringProgress = 100;
                }
            }
        }

        private void UpdateCurrentValuesDisplay(PerformanceDataPoint dataPoint)
        {
            var displayText = "";

            if (Settings.MonitorCpu)
                displayText += $"CPU: {dataPoint.CpuPercent:F1}%  ";

            if (Settings.MonitorRam)
                displayText += $"RAM: {dataPoint.RamMB:F0}MB ({dataPoint.RamPercent:F1}%)  ";

            if (Settings.MonitorGpuCore)
                displayText += $"GPU Core: {dataPoint.GpuCorePercent:F1}%  ";

            if (Settings.MonitorGpuVideo)
                displayText += $"GPU Video: {dataPoint.GpuVideoPercent:F1}%  ";

            if (Settings.MonitorGpuVram)
                displayText += $"GPU VRAM: {dataPoint.GpuVramMB:F0}MB ({dataPoint.GpuVramPercent:F1}%)";

            CurrentValuesText = displayText.Trim();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnErrorOccurred(string errorMessage)
        {
            ErrorOccurred?.Invoke(this, errorMessage);
        }

        #endregion

        #region IDisposable Implementation
        
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
                    monitoringTimer?.Stop();
                    uiUpdateTimer?.Stop();
                    
                    cancellationTokenSource?.Cancel();
                    cancellationTokenSource?.Dispose();
                    
                    loggingService?.Dispose();
                    monitoringService?.Dispose();
                }
                disposed = true;
            }
        }

        ~MainWindowViewModel()
        {
            Dispose(false);
        }

        #endregion
    }
}