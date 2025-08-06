using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Procmon.Models;
using Procmon.Helpers;

namespace Procmon.Views
{
    /// <summary>
    /// Performance chart window - focused on chart display only
    /// </summary>
    public partial class PerformanceChartWindow : Window
    {
        private List<PerformanceDataPoint> dataPoints = new List<PerformanceDataPoint>();
        private const int MAX_DATA_POINTS = 200;

        public PerformanceChartWindow()
        {
            InitializeComponent();
            
            // Wait for the window to be loaded before initializing the chart
            this.Loaded += PerformanceChartWindow_Loaded;
        }

        private void PerformanceChartWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Chart window loaded. Canvas: {ChartCanvas != null}, " +
                    $"Size: {ChartCanvas?.ActualWidth}x{ChartCanvas?.ActualHeight}");
                
                InitializeChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing chart: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Chart Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeChart()
        {
            if (ChartCanvas == null)
            {
                MessageBox.Show("Chart canvas is not available. Please check the XAML.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ChartCanvas.ActualWidth <= 0 || ChartCanvas.ActualHeight <= 0)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    InitializeChart();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            ChartHelper.DrawGrid(ChartCanvas);
            ChartHelper.DrawAxes(ChartCanvas);
        }

        public void AddDataPoint(PerformanceDataPoint dataPoint)
        {
            dataPoints.Add(dataPoint);
            
            // Keep only the last MAX_DATA_POINTS for performance
            if (dataPoints.Count > MAX_DATA_POINTS)
            {
                dataPoints.RemoveAt(0);
            }
            
            UpdateChart();
        }

        public void SetDataPoints(IEnumerable<PerformanceDataPoint> points)
        {
            var pointsList = points.ToList();
            if (pointsList.Count > MAX_DATA_POINTS)
            {
                dataPoints = pointsList.Skip(pointsList.Count - MAX_DATA_POINTS).ToList();
            }
            else
            {
                dataPoints = pointsList;
            }
            
            if (this.IsLoaded && ChartCanvas != null)
            {
                UpdateChart();
            }
        }

        private void UpdateChart(object sender = null, RoutedEventArgs e = null)
        {
            if (ChartCanvas == null || !this.IsLoaded)
                return;

            ChartCanvas.Children.Clear();
            
            if (dataPoints.Count < 2) return;

            ChartHelper.DrawGrid(ChartCanvas);
            ChartHelper.DrawAxes(ChartCanvas);
            
            // Draw data lines with modern colors based on checkbox states
            if (ShowCpuCheckBox?.IsChecked == true)
                ChartHelper.DrawDataLine(ChartCanvas, dataPoints.Select(d => (double)d.CpuPercent).ToList(), "CPU");
            
            if (ShowRamCheckBox?.IsChecked == true)
                ChartHelper.DrawDataLine(ChartCanvas, dataPoints.Select(d => (double)d.RamPercent).ToList(), "RAM");
            
            if (ShowGpuCoreCheckBox?.IsChecked == true)
                ChartHelper.DrawDataLine(ChartCanvas, dataPoints.Select(d => (double)d.GpuCorePercent).ToList(), "GPUCore");
            
            if (ShowGpuVideoCheckBox?.IsChecked == true)
                ChartHelper.DrawDataLine(ChartCanvas, dataPoints.Select(d => (double)d.GpuVideoPercent).ToList(), "GPUVideo");
            
            if (ShowGpuVramCheckBox?.IsChecked == true)
                ChartHelper.DrawDataLine(ChartCanvas, dataPoints.Select(d => (double)d.GpuVramPercent).ToList(), "GPUVRAM");

            UpdateLegendVisibility();
            UpdateAxisLabels();
        }

        private void UpdateLegendVisibility()
        {
            if (CpuLegend != null)
                CpuLegend.Visibility = ShowCpuCheckBox?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (RamLegend != null)
                RamLegend.Visibility = ShowRamCheckBox?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (GpuCoreLegend != null)
                GpuCoreLegend.Visibility = ShowGpuCoreCheckBox?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (GpuVideoLegend != null)
                GpuVideoLegend.Visibility = ShowGpuVideoCheckBox?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (GpuVramLegend != null)
                GpuVramLegend.Visibility = ShowGpuVramCheckBox?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateAxisLabels()
        {
            // Y-axis labels (0% to 100%) 
            if (YAxisLabels != null)
            {
                YAxisLabels.Children.Clear();
                for (int i = 0; i <= 10; i++)
                {
                    var label = new TextBlock
                    {
                        Text = $"{100 - (i * 10)}%",
                        FontSize = 10,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                        Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176)),
                        FontWeight = FontWeights.Medium
                    };
                    YAxisLabels.Children.Add(label);
                }
            }

            // X-axis labels (time)
            if (XAxisLabels != null)
            {
                XAxisLabels.Children.Clear();
                if (dataPoints.Count > 0)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        int index = (int)(i * (dataPoints.Count - 1) / 4.0);
                        if (index < dataPoints.Count)
                        {
                            var timestamp = DateTime.Parse(dataPoints[index].Timestamp);
                            var label = new TextBlock
                            {
                                Text = timestamp.ToString("HH:mm:ss"),
                                FontSize = 10,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Top,
                                Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176)),
                                FontWeight = FontWeights.Medium,
                                Margin = new Thickness(0, 4, 0, 0)
                            };
                            XAxisLabels.Children.Add(label);
                        }
                    }
                }
            }
        }

        public void ClearChartButton_Click(object sender, RoutedEventArgs e)
        {
            dataPoints.Clear();
            UpdateChart();
        }

        public void ClearData()
        {
            dataPoints.Clear();
            if (this.IsLoaded && ChartCanvas != null)
            {
                UpdateChart();
            }
        }

        public void ForceInitializeChart()
        {
            try
            {
                if (ChartCanvas == null)
                {
                    MessageBox.Show("ChartCanvas is null. The XAML may not have loaded properly.", 
                        "Chart Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (ChartCanvas.ActualWidth <= 0 || ChartCanvas.ActualHeight <= 0)
                {
                    this.UpdateLayout();
                    
                    if (ChartCanvas.ActualWidth <= 0 || ChartCanvas.ActualHeight <= 0)
                    {
                        MessageBox.Show($"Chart canvas has invalid dimensions: {ChartCanvas.ActualWidth}x{ChartCanvas.ActualHeight}", 
                            "Chart Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                InitializeChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ForceInitializeChart: {ex.Message}", "Chart Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (IsLoaded && ChartCanvas != null)
            {
                UpdateChart();
            }
        }
    }
}