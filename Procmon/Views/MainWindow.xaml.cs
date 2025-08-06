using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Win32;
using Procmon.ViewModels;
using Procmon.Models;
using Procmon.Helpers;
using Procmon.Views;

namespace Procmon.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml - Now focused on UI concerns only
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel viewModel;
        private List<PerformanceDataPoint> chartDataPoints = new List<PerformanceDataPoint>();
        private bool autoScroll = true;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Initialize ViewModel first before setting up event handlers
                viewModel = new MainWindowViewModel();
                DataContext = viewModel;
                
                // Now that viewModel is initialized, set up event handlers
                InitializeEventHandlers();
                InitializeDataBindings();
                SetupSmartAutoScroll();
                
                // Initialize file input state with null check - now that LogToFileCheckBox is checked by default
                UpdateFileInputState();
            }
            catch (Exception ex)
            {
                // Show error message and provide fallback
                MessageBox.Show($"Error initializing main window: {ex.Message}\n\nSome features may not work correctly.", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // Try to create a basic ViewModel if it failed
                if (viewModel == null)
                {
                    try
                    {
                        viewModel = new MainWindowViewModel();
                        DataContext = viewModel;
                    }
                    catch
                    {
                        // If even basic initialization fails, we'll have to work with null checks
                        System.Diagnostics.Debug.WriteLine("Failed to create ViewModel. Operating in degraded mode.");
                    }
                }
            }
        }

        #region Event Handlers Setup
        
        private void InitializeEventHandlers()
        {
            // ViewModel events
            viewModel.DataPointCollected += OnDataPointCollected;
            viewModel.ErrorOccurred += OnErrorOccurred;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.DataCleared += OnDataCleared;
            
            // UI events
            this.Loaded += MainWindow_Loaded;
            ProcessComboBox.SelectionChanged += ProcessComboBox_SelectionChanged;
            LogToFileCheckBox.Checked += LogToFileCheckBox_CheckedChanged;
            LogToFileCheckBox.Unchecked += LogToFileCheckBox_CheckedChanged;
            
            // Tab control event for updating chart when switching to Charts tab
            DataTabControl.SelectionChanged += DataTabControl_SelectionChanged;
            
            // Text changed events for duration and interval
            DurationTextBox.TextChanged += DurationTextBox_TextChanged;
            IntervalTextBox.TextChanged += IntervalTextBox_TextChanged;
            
            // File name TextBox text changed event for auto-scroll
            FileNameTextBox.TextChanged += FileNameTextBox_TextChanged;
            
            // Sensor checkbox events to update settings
            CpuCheckBox.Checked += (s, e) => UpdateSensorSettings();
            CpuCheckBox.Unchecked += (s, e) => UpdateSensorSettings();
            RamCheckBox.Checked += (s, e) => UpdateSensorSettings();
            RamCheckBox.Unchecked += (s, e) => UpdateSensorSettings();
            GpuCoreCheckBox.Checked += (s, e) => UpdateSensorSettings();
            GpuCoreCheckBox.Unchecked += (s, e) => UpdateSensorSettings();
            GpuVideoCheckBox.Checked += (s, e) => UpdateSensorSettings();
            GpuVideoCheckBox.Unchecked += (s, e) => UpdateSensorSettings();
            GpuVramCheckBox.Checked += (s, e) => UpdateSensorSettings();
            GpuVramCheckBox.Unchecked += (s, e) => UpdateSensorSettings();
            
            // Chart events
            SetupChartEventHandlers();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update UI elements that need manual updates based on ViewModel property changes
            if (e.PropertyName == nameof(MainWindowViewModel.StatusText))
            {
                UpdatePauseButtonText();
            }
            // Auto-scroll log file textbox when LogFileName changes from ViewModel
            else if (e.PropertyName == "Settings.LogFileName" || e.PropertyName == "LogFileName")
            {
                ScrollLogFileTextToEnd();
            }
        }

        private void InitializeDataBindings()
        {
            // Bind data grid
            DataGrid.ItemsSource = viewModel.PerformanceData;
            
            // Bind ComboBox properly with two-way binding for SelectedItem
            ProcessComboBox.ItemsSource = viewModel.AvailableProcesses;
            
            // Set up two-way binding for selected process using DisplayMemberPath and SelectedValuePath from original
            var selectedProcessBinding = new System.Windows.Data.Binding("SelectedProcess")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.TwoWay,
                UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
            };
            ProcessComboBox.SetBinding(System.Windows.Controls.ComboBox.SelectedItemProperty, selectedProcessBinding);
            
            // Bind button states to ViewModel properties
            BindButtonStates();
        }

        private void BindButtonStates()
        {
            // Bind Start button
            var startBinding = new System.Windows.Data.Binding("CanStart")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            StartButton.SetBinding(System.Windows.Controls.Button.IsEnabledProperty, startBinding);

            // Bind Pause button
            var pauseBinding = new System.Windows.Data.Binding("CanPause")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            PauseButton.SetBinding(System.Windows.Controls.Button.IsEnabledProperty, pauseBinding);

            // Bind Stop button
            var stopBinding = new System.Windows.Data.Binding("CanStop")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            StopButton.SetBinding(System.Windows.Controls.Button.IsEnabledProperty, stopBinding);

            // Bind Clear Data button
            var clearBinding = new System.Windows.Data.Binding("CanClearData")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            ClearDataButton.SetBinding(System.Windows.Controls.Button.IsEnabledProperty, clearBinding);

            // Bind Export Data button
            var exportBinding = new System.Windows.Data.Binding("CanExportData")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            ExportDataButton.SetBinding(System.Windows.Controls.Button.IsEnabledProperty, exportBinding);
            
            // Bind status texts
            var statusBinding = new System.Windows.Data.Binding("StatusText")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            StatusTextBlock.SetBinding(System.Windows.Controls.TextBlock.TextProperty, statusBinding);
            
            var currentValuesBinding = new System.Windows.Data.Binding("CurrentValuesText")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            CurrentValuesTextBlock.SetBinding(System.Windows.Controls.TextBlock.TextProperty, currentValuesBinding);
            
            var timeRemainingBinding = new System.Windows.Data.Binding("TimeRemainingText")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            TimeRemainingTextBlock.SetBinding(System.Windows.Controls.TextBlock.TextProperty, timeRemainingBinding);
            
            var progressBinding = new System.Windows.Data.Binding("MonitoringProgress")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            MonitoringProgressBar.SetBinding(System.Windows.Controls.ProgressBar.ValueProperty, progressBinding);
            
            var progressVisibilityBinding = new System.Windows.Data.Binding("ProgressVisible")
            {
                Source = viewModel,
                Mode = System.Windows.Data.BindingMode.OneWay,
                Converter = new BooleanToVisibilityConverter()
            };
            MonitoringProgressBar.SetBinding(System.Windows.Controls.ProgressBar.VisibilityProperty, progressVisibilityBinding);
        }

        private void SetupChartEventHandlers()
        {
            if (ShowCpuCheckBox != null) 
            {
                ShowCpuCheckBox.Checked += ChartCheckBox_Changed;
                ShowCpuCheckBox.Unchecked += ChartCheckBox_Changed;
            }
            if (ShowRamCheckBox != null) 
            {
                ShowRamCheckBox.Checked += ChartCheckBox_Changed;
                ShowRamCheckBox.Unchecked += ChartCheckBox_Changed;
            }
            if (ShowGpuCoreCheckBox != null) 
            {
                ShowGpuCoreCheckBox.Checked += ChartCheckBox_Changed;
                ShowGpuCoreCheckBox.Unchecked += ChartCheckBox_Changed;
            }
            if (ShowGpuVideoCheckBox != null) 
            {
                ShowGpuVideoCheckBox.Checked += ChartCheckBox_Changed;
                ShowGpuVideoCheckBox.Unchecked += ChartCheckBox_Changed;
            }
            if (ShowGpuVramCheckBox != null) 
            {
                ShowGpuVramCheckBox.Checked += ChartCheckBox_Changed;
                ShowGpuVramCheckBox.Unchecked += ChartCheckBox_Changed;
            }
        }

        private void ChartCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Update chart immediately when checkboxes change
            if (chartDataPoints.Count >= 2)
            {
                UpdateChart();
            }
        }

        private void DataTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataTabControl?.SelectedItem is TabItem selectedTab)
            {
                switch (selectedTab.Header?.ToString())
                {
                    case "Data":
                        // Ensure auto-scroll is enabled and scroll to bottom when switching to Data tab
                        autoScroll = true;
                        if (DataGrid?.Items?.Count > 0)
                        {
                            DataGrid.ScrollIntoView(DataGrid.Items[DataGrid.Items.Count - 1]);
                        }
                        break;
                        
                    case "Charts":
                        // Update chart immediately when switching to Charts tab (if we have data)
                        // Don't throttle tab switching updates
                        if (chartDataPoints.Count >= 2)
                        {
                            UpdateChart();
                            lastChartUpdate = DateTime.Now; // Update throttle timestamp
                        }
                        break;
                        
                    case "Statistics":
                        // Trigger statistics update when switching to Statistics tab
                        UpdateStatistics(this, EventArgs.Empty);
                        break;
                }
            }
        }
        
        #endregion

        #region ViewModel Event Handlers
        
        private void OnDataPointCollected(object sender, PerformanceDataPoint dataPoint)
        {
            // Update integrated chart
            UpdateIntegratedChart(dataPoint);
            
            // Handle auto-scroll
            if (autoScroll && DataGrid.Items.Count > 0)
                DataGrid.ScrollIntoView(DataGrid.Items[DataGrid.Items.Count - 1]);
        }

        private void OnErrorOccurred(object sender, string errorMessage)
        {
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnDataCleared(object sender, EventArgs e)
        {
            // Clear chart data and update chart display
            chartDataPoints.Clear();
            UpdateChart();
            
            // Clear statistics display
            UpdateStatistics(this, EventArgs.Empty);
        }

        #endregion

        #region UI Event Handlers
        
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;
            
            try
            {
                bool success = await viewModel.StartMonitoringAsync();
                // Button states will be automatically updated via data binding
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start monitoring: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;
            
            await viewModel.StopMonitoringAsync();
            // Button states will be automatically updated via data binding
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;
            
            viewModel.PauseResumeMonitoring();
            
            // Update button text based on current state
            UpdatePauseButtonText();
        }

        private void UpdatePauseButtonText()
        {
            if (viewModel == null) return;
            
            // Update pause button text based on monitoring state
            PauseButton.Content = viewModel.StatusText.Contains("Paused") ? "Resume" : "Pause";
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ClearData();
            chartDataPoints.Clear();
            UpdateChart();
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            bool success = await viewModel.ExportDataAsync();
            if (success)
            {
                MessageBox.Show("Data exported successfully!", "Export Complete", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void RefreshProcessButton_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.RefreshProcessListAsync();
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = FileNameTextBox.Text
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                FileNameTextBox.Text = saveFileDialog.FileName;
                // Add null check to prevent null reference exceptions
                if (viewModel?.Settings != null)
                {
                    viewModel.Settings.LogFileName = saveFileDialog.FileName;
                }
                
                // Auto-scroll to end after setting new file path
                ScrollLogFileTextToEnd();
            }
        }

        private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel?.OpenLogFolder();
        }

        private void ProcessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add null check for viewModel
            if (viewModel == null) return;
            
            // Don't prompt during initial loading or if no data exists
            if (viewModel.PerformanceData.Count == 0 || ProcessComboBox.SelectedItem == null)
                return;

            // Don't prompt during monitoring
            if (!viewModel.CanClearData)
                return;

            // Warn user about data clearing when switching processes
            var result = MessageBox.Show(
                "Switching to a different process will clear all existing performance data.\n\n" +
                "Do you want to continue?",
                "Clear Existing Data?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                viewModel.ClearData();
                chartDataPoints.Clear();
                UpdateChart();
            }
            else
            {
                // User cancelled, revert selection to previous item
                if (e.RemovedItems.Count > 0)
                {
                    ProcessComboBox.SelectionChanged -= ProcessComboBox_SelectionChanged;
                    ProcessComboBox.SelectedItem = e.RemovedItems[0];
                    ProcessComboBox.SelectionChanged += ProcessComboBox_SelectionChanged;
                }
            }
        }

        private void LogToFileCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Add null check to prevent null reference exceptions during initialization
            if (viewModel?.Settings != null)
            {
                viewModel.Settings.LogToFile = LogToFileCheckBox.IsChecked == true;
            }
            UpdateFileInputState();
        }
        
        private void UpdateFileInputState()
        {
            bool isEnabled = LogToFileCheckBox?.IsChecked == true;
            if (FileNameTextBox != null) FileNameTextBox.IsEnabled = isEnabled;
            if (BrowseFileButton != null) BrowseFileButton.IsEnabled = isEnabled;
        }

        private void DurationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(DurationTextBox.Text, out int value) && value >= 0)
            {
                if (viewModel?.Settings != null)
                {
                    viewModel.Settings.DurationSeconds = value;
                }
            }
        }

        private void IntervalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(IntervalTextBox.Text, out int value) && value > 0)
            {
                if (viewModel?.Settings != null)
                {
                    viewModel.Settings.IntervalMilliseconds = value;
                    
                    // Update monitoring timer interval if currently monitoring
                    viewModel.UpdateMonitoringInterval();
                }
            }
        }

        private void FileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                // Auto-scroll to the end of the text when it becomes too long
                // This ensures the user can see the end of a long file path
                
                // Use Dispatcher to ensure this runs after text rendering is complete
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Set caret position to the end
                    textBox.CaretIndex = textBox.Text.Length;
                    
                    // Try to scroll to end using built-in method
                    textBox.ScrollToEnd();
                    
                    // Also try to find and manually scroll the internal ScrollViewer
                    var scrollViewer = FindScrollViewerInTextBox(textBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollToHorizontalOffset(scrollViewer.ScrollableWidth);
                        System.Diagnostics.Debug.WriteLine($"FileNameTextBox auto-scroll: ScrollableWidth={scrollViewer.ScrollableWidth}, HorizontalOffset={scrollViewer.HorizontalOffset}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("FileNameTextBox auto-scroll: ScrollViewer not found, using ScrollToEnd()");
                    }
                    
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Scrolls the log file textbox to the end to show the filename portion of long paths
        /// </summary>
        private void ScrollLogFileTextToEnd()
        {
            if (FileNameTextBox != null && !string.IsNullOrEmpty(FileNameTextBox.Text))
            {
                // Use Dispatcher to ensure this runs after the text binding is updated
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Set caret position to the end
                    FileNameTextBox.CaretIndex = FileNameTextBox.Text.Length;
                    
                    // Try to scroll to end using built-in method
                    FileNameTextBox.ScrollToEnd();
                    
                    // Also try to find and manually scroll the internal ScrollViewer
                    var scrollViewer = FindScrollViewerInTextBox(FileNameTextBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollToHorizontalOffset(scrollViewer.ScrollableWidth);
                        System.Diagnostics.Debug.WriteLine($"ScrollLogFileTextToEnd: ScrollableWidth={scrollViewer.ScrollableWidth}, HorizontalOffset={scrollViewer.HorizontalOffset}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ScrollLogFileTextToEnd: ScrollViewer not found, using ScrollToEnd()");
                    }
                    
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Finds the ScrollViewer inside a TextBox control
        /// </summary>
        private ScrollViewer FindScrollViewerInTextBox(TextBox textBox)
        {
            if (textBox == null) return null;
            
            // Find the PART_ContentHost ScrollViewer inside the TextBox template
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(textBox); i++)
            {
                var child = VisualTreeHelper.GetChild(textBox, i);
                
                // Look for the Border in the template
                if (child is Border border)
                {
                    // Find the ScrollViewer inside the Border
                    for (int j = 0; j < VisualTreeHelper.GetChildrenCount(border); j++)
                    {
                        var borderChild = VisualTreeHelper.GetChild(border, j);
                        if (borderChild is ScrollViewer scrollViewer && scrollViewer.Name == "PART_ContentHost")
                        {
                            return scrollViewer;
                        }
                    }
                }
            }
            return null;
        }
        
        #endregion

        #region Sensor Control Event Handlers
        
        private void SelectAllSensorsButton_Click(object sender, RoutedEventArgs e)
        {
            CpuCheckBox.IsChecked = true;
            RamCheckBox.IsChecked = true;
            GpuCoreCheckBox.IsChecked = true;
            GpuVideoCheckBox.IsChecked = true;
            GpuVramCheckBox.IsChecked = true;
            
            UpdateSensorSettings();
        }

        private void SelectNoneSensorsButton_Click(object sender, RoutedEventArgs e)
        {
            CpuCheckBox.IsChecked = false;
            RamCheckBox.IsChecked = false;
            GpuCoreCheckBox.IsChecked = false;
            GpuVideoCheckBox.IsChecked = false;
            GpuVramCheckBox.IsChecked = false;
            
            UpdateSensorSettings();
        }

        private void UpdateSensorSettings()
        {
            // Add null check to prevent null reference exceptions during initialization
            if (viewModel?.Settings == null) return;
            
            viewModel.Settings.MonitorCpu = CpuCheckBox.IsChecked == true;
            viewModel.Settings.MonitorRam = RamCheckBox.IsChecked == true;
            viewModel.Settings.MonitorGpuCore = GpuCoreCheckBox.IsChecked == true;
            viewModel.Settings.MonitorGpuVideo = GpuVideoCheckBox.IsChecked == true;
            viewModel.Settings.MonitorGpuVram = GpuVramCheckBox.IsChecked == true;
        }

        #endregion

        #region Duration/Interval Controls
        
        private void DurationUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(DurationTextBox.Text, out int value))
            {
                value = Math.Max(0, value + 10);
                DurationTextBox.Text = value.ToString();
                // Add null check to prevent null reference exceptions
                if (viewModel?.Settings != null)
                {
                    viewModel.Settings.DurationSeconds = value;
                }
            }
        }

        private void DurationDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(DurationTextBox.Text, out int value))
            {
                value = Math.Max(0, value - 10);
                DurationTextBox.Text = value.ToString();
                // Add null check to prevent null reference exceptions
                if (viewModel?.Settings != null)
                {
                    viewModel.Settings.DurationSeconds = value;
                }
            }
        }

        private void IntervalUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(IntervalTextBox.Text, out int value))
            {
                value = Math.Max(1, value + 10);
                IntervalTextBox.Text = value.ToString();
                // Add null check to prevent null reference exceptions
                if (viewModel?.Settings != null)
                {
                    viewModel.Settings.IntervalMilliseconds = value;
                    // Update monitoring timer interval if currently monitoring
                    viewModel.UpdateMonitoringInterval();
                }
            }
        }

        private void IntervalDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(IntervalTextBox.Text, out int value))
            {
                value = Math.Max(1, value - 10);
                IntervalTextBox.Text = value.ToString();
                // Add null check to prevent null reference exceptions
                if (viewModel?.Settings != null)
                {
                    viewModel.Settings.IntervalMilliseconds = value;
                    // Update monitoring timer interval if currently monitoring
                    viewModel.UpdateMonitoringInterval();
                }
            }
        }

        #endregion

        #region Statistics Panel
        
        private void UpdateStatistics(object sender, EventArgs e)
        {
            // Add null check to prevent null reference exceptions
            if (viewModel == null) return;
            
            StatisticsPanel.Children.Clear();

            // Create a WrapPanel to arrange statistics blocks side by side
            var wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            StatisticsPanel.Children.Add(wrapPanel);

            // Always show blocks for enabled sensors, even when no data
            var hasData = viewModel.PerformanceData.Count > 0;
            
            if (CpuCheckBox.IsChecked == true)
            {
                CreateStatisticsBlock(wrapPanel, "CPU Load", ChartHelper.GetSensorBrush("CPU"), 
                    hasData ? viewModel.GetCpuStatistics() : null);
            }

            if (RamCheckBox.IsChecked == true)
            {
                CreateStatisticsBlock(wrapPanel, "RAM Usage", ChartHelper.GetSensorBrush("RAM"), 
                    hasData ? viewModel.GetRamStatistics() : null);
            }

            if (GpuCoreCheckBox.IsChecked == true)
            {
                CreateStatisticsBlock(wrapPanel, "GPU Core", ChartHelper.GetSensorBrush("GPUCore"), 
                    hasData ? viewModel.GetGpuCoreStatistics() : null);
            }

            if (GpuVideoCheckBox.IsChecked == true)
            {
                CreateStatisticsBlock(wrapPanel, "GPU Video Engine", ChartHelper.GetSensorBrush("GPUVideo"), 
                    hasData ? viewModel.GetGpuVideoStatistics() : null);
            }

            if (GpuVramCheckBox.IsChecked == true)
            {
                CreateStatisticsBlock(wrapPanel, "GPU VRAM", ChartHelper.GetSensorBrush("GPUVRAM"), 
                    hasData ? viewModel.GetGpuVramStatistics() : null);
            }
        }

        private void CreateStatisticsBlock(WrapPanel parent, string title, SolidColorBrush brush, PerformanceStatistics stats)
        {
            var border = new Border
            {
                Margin = new Thickness(8, 6, 8, 6),
                Padding = new Thickness(16, 12, 16, 12),
                CornerRadius = new CornerRadius(6),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
                Background = brush,
                MinWidth = 200,
                MaxWidth = 280
            };

            // Add subtle shadow effect
            border.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 270,
                ShadowDepth = 2,
                BlurRadius = 6,
                Opacity = 0.25
            };

            var panel = new StackPanel { Orientation = Orientation.Vertical };
            border.Child = panel;
            parent.Children.Add(border);

            // Add header text
            var headerTextBlock = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
                Margin = new Thickness(0, 0, 0, 8),
                TextAlignment = TextAlignment.Center
            };
            panel.Children.Add(headerTextBlock);

            // Add stats or placeholder text
            if (stats != null)
            {
                AddStatText(panel, $"Average: {stats.Average}");
                AddStatText(panel, $"Maximum: {stats.Maximum}");
                AddStatText(panel, $"Minimum: {stats.Minimum}");
            }
            else
            {
                AddStatText(panel, "Average: -");
                AddStatText(panel, "Maximum: -");
                AddStatText(panel, "Minimum: -");
            }
        }

        private void AddStatText(StackPanel parent, string text)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                Margin = new Thickness(0, 1, 0, 1),
                TextAlignment = TextAlignment.Center
            };
            parent.Children.Add(textBlock);
        }

        #endregion

        #region Chart Management
        
        private DateTime lastChartUpdate = DateTime.MinValue;
        private const int CHART_UPDATE_THROTTLE_MS = 100; // Only update chart every 100ms max
        
        private void UpdateIntegratedChart(PerformanceDataPoint dataPoint)
        {
            chartDataPoints.Add(dataPoint);
            
            // Keep only the last 200 points for performance
            if (chartDataPoints.Count > 200)
            {
                chartDataPoints.RemoveAt(0);
            }
            
            // Only update chart if we're on Charts tab and have enough data
            if (DataTabControl?.SelectedIndex == 1 && chartDataPoints.Count >= 2)
            {
                // Throttle chart updates to prevent excessive redrawing
                var now = DateTime.Now;
                if ((now - lastChartUpdate).TotalMilliseconds >= CHART_UPDATE_THROTTLE_MS)
                {
                    UpdateChart();
                    lastChartUpdate = now;
                }
            }
        }

        private void ScheduleChartUpdate()
        {
            // This method can be used for delayed updates if needed
            if (chartDataPoints.Count >= 2)
            {
                UpdateChart();
            }
        }

        private void UpdateChart()
        {
            // Add null check for viewModel
            if (ChartCanvas == null || viewModel?.Settings == null) 
            {
                return;
            }

            // Always clear the canvas first, regardless of data count
            ChartCanvas.Children.Clear();

            // If we have no data or less than 2 points, just show empty chart with grid and axes
            if (chartDataPoints.Count < 2)
            {
                // Draw empty chart with grid and axes
                ChartHelper.DrawGrid(ChartCanvas);
                ChartHelper.DrawAxes(ChartCanvas);
                return;
            }

            // Create temporary settings based on checkbox states for chart display
            var chartSettings = new MonitoringSettings();
            chartSettings.MonitorCpu = ShowCpuCheckBox?.IsChecked == true;
            chartSettings.MonitorRam = ShowRamCheckBox?.IsChecked == true;
            chartSettings.MonitorGpuCore = ShowGpuCoreCheckBox?.IsChecked == true;
            chartSettings.MonitorGpuVideo = ShowGpuVideoCheckBox?.IsChecked == true;
            chartSettings.MonitorGpuVram = ShowGpuVramCheckBox?.IsChecked == true;

            ChartHelper.UpdateChart(ChartCanvas, chartDataPoints, chartSettings);
        }

        #endregion

        #region Window Events and Cleanup
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up chart canvas resize handler
            if (ChartCanvas != null)
            {
                ChartCanvas.SizeChanged += (s, args) => UpdateChart();
            }
            
            // Initialize statistics timer
            var statsTimer = new System.Windows.Threading.DispatcherTimer();
            statsTimer.Interval = TimeSpan.FromSeconds(2);
            statsTimer.Tick += UpdateStatistics;
            statsTimer.Start();
            
            // Initialize textbox values from settings
            if (viewModel?.Settings != null)
            {
                DurationTextBox.Text = viewModel.Settings.DurationSeconds.ToString();
                IntervalTextBox.Text = viewModel.Settings.IntervalMilliseconds.ToString();
                
                // Set default file name like in original: Procmon-{timestamp}.csv
                string defaultFileName = $"Procmon-{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                if (string.IsNullOrEmpty(viewModel.Settings.LogFileName))
                {
                    viewModel.Settings.LogFileName = defaultFileName;
                }
                
                var fileNameBinding = new System.Windows.Data.Binding("Settings.LogFileName")
                {
                    Source = viewModel,
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                };
                FileNameTextBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, fileNameBinding);
                
                // Auto-scroll the log file textbox to end after binding is set up
                ScrollLogFileTextToEnd();
            }
            
            // Initial setup for pause button text
            UpdatePauseButtonText();
            
            // Ensure ComboBox is properly populated after loading
            _ = EnsureProcessListPopulated();
        }
        
        private async Task EnsureProcessListPopulated()
        {
            // If process list is empty, try to refresh it
            if (viewModel != null && viewModel.AvailableProcesses.Count == 0)
            {
                await viewModel.RefreshProcessListAsync();
            }
        }
        
        private void SetupSmartAutoScroll()
        {
            if (DataGrid != null)
            {
                DataGrid.Loaded += (s, e) =>
                {
                    var scrollViewer = FindScrollViewer(DataGrid);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollChanged += DataGrid_ScrollChanged;
                    }
                };
            }
        }
        
        private ScrollViewer FindScrollViewer(DependencyObject depObj)
        {
            if (depObj == null) return null;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                
                if (child is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                
                var result = FindScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }
        
        private void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Check if we're at the bottom
                double scrollPosition = scrollViewer.VerticalOffset;
                double maxScroll = scrollViewer.ScrollableHeight;
                
                // Enable auto-scroll when at the very bottom (within 1 pixel tolerance)
                if (Math.Abs(scrollPosition - maxScroll) < 1.0)
                {
                    autoScroll = true;
                }
                // Disable auto-scroll when user scrolls up
                else if (scrollPosition < maxScroll)
                {
                    autoScroll = false;
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            viewModel?.Dispose();
            base.OnClosing(e);
        }

        #endregion
    }
}