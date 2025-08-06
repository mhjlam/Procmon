using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Procmon.Models;

namespace Procmon.Helpers
{
    /// <summary>
    /// Helper class for chart operations and rendering
    /// </summary>
    public static class ChartHelper
    {
        private const double CHART_MARGIN = 40;
        
        /// <summary>
        /// Modern color palette for chart lines
        /// </summary>
        public static readonly Dictionary<string, Color> ChartColors = new Dictionary<string, Color>
        {
            { "CPU", Color.FromRgb(231, 76, 60) },      // Modern Red
            { "RAM", Color.FromRgb(52, 152, 219) },     // Modern Blue
            { "GPUCore", Color.FromRgb(46, 204, 113) }, // Modern Green
            { "GPUVideo", Color.FromRgb(243, 156, 18) }, // Modern Orange
            { "GPUVRAM", Color.FromRgb(155, 89, 182) }   // Modern Purple
        };

        /// <summary>
        /// Draw grid lines on the chart canvas
        /// </summary>
        public static void DrawGrid(Canvas canvas)
        {
            if (canvas == null) return;
            
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            // Vertical grid lines
            for (int i = 0; i <= 10; i++)
            {
                double x = CHART_MARGIN + (i * (canvasWidth - CHART_MARGIN * 2) / 10);
                var line = new Line
                {
                    X1 = x, Y1 = CHART_MARGIN,
                    X2 = x, Y2 = canvasHeight - CHART_MARGIN,
                    Stroke = new SolidColorBrush(Color.FromRgb(70, 70, 73)),
                    StrokeThickness = 0.5,
                    Opacity = 0.5
                };
                canvas.Children.Add(line);
            }

            // Horizontal grid lines
            for (int i = 0; i <= 10; i++)
            {
                double y = CHART_MARGIN + (i * (canvasHeight - CHART_MARGIN * 2) / 10);
                var line = new Line
                {
                    X1 = CHART_MARGIN, Y1 = y,
                    X2 = canvasWidth - CHART_MARGIN, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(70, 70, 73)),
                    StrokeThickness = 0.5,
                    Opacity = 0.5
                };
                canvas.Children.Add(line);
            }
        }

        /// <summary>
        /// Draw axes on the chart canvas
        /// </summary>
        public static void DrawAxes(Canvas canvas)
        {
            if (canvas == null) return;
            
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            // Y-axis
            var yAxis = new Line
            {
                X1 = CHART_MARGIN, Y1 = CHART_MARGIN,
                X2 = CHART_MARGIN, Y2 = canvasHeight - CHART_MARGIN,
                Stroke = new SolidColorBrush(Color.FromRgb(176, 176, 176)),
                StrokeThickness = 2
            };
            canvas.Children.Add(yAxis);

            // X-axis
            var xAxis = new Line
            {
                X1 = CHART_MARGIN, Y1 = canvasHeight - CHART_MARGIN,
                X2 = canvasWidth - CHART_MARGIN, Y2 = canvasHeight - CHART_MARGIN,
                Stroke = new SolidColorBrush(Color.FromRgb(176, 176, 176)),
                StrokeThickness = 2
            };
            canvas.Children.Add(xAxis);
        }

        /// <summary>
        /// Draw a data line on the chart canvas
        /// </summary>
        public static void DrawDataLine(Canvas canvas, List<double> values, string sensorType)
        {
            if (canvas == null || values.Count < 2) return;

            double canvasWidth = canvas.ActualWidth - (CHART_MARGIN * 2);
            double canvasHeight = canvas.ActualHeight - (CHART_MARGIN * 2);
            
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            var brush = new SolidColorBrush(ChartColors.ContainsKey(sensorType) ? ChartColors[sensorType] : Colors.Gray);
            
            var polyline = new Polyline
            {
                Stroke = brush,
                StrokeThickness = 2,
                Fill = null,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            for (int i = 0; i < values.Count; i++)
            {
                double x = CHART_MARGIN + (i * canvasWidth / (values.Count - 1));
                double y = CHART_MARGIN + canvasHeight - (values[i] * canvasHeight / 100.0);
                
                // Clamp y values
                y = Math.Max(CHART_MARGIN, Math.Min(canvasHeight + CHART_MARGIN, y));
                
                polyline.Points.Add(new Point(x, y));
            }

            canvas.Children.Add(polyline);
        }

        /// <summary>
        /// Update chart with performance data
        /// </summary>
        public static void UpdateChart(Canvas canvas, List<PerformanceDataPoint> dataPoints, MonitoringSettings settings)
        {
            if (canvas == null) return;

            // Always clear the canvas first
            canvas.Children.Clear();
            
            // Always draw grid and axes for consistency
            DrawGrid(canvas);
            DrawAxes(canvas);
            
            // Only draw data lines if we have enough data points
            if (dataPoints == null || dataPoints.Count < 2) return;

            // Draw data lines based on enabled sensors
            if (settings.MonitorCpu)
                DrawDataLine(canvas, dataPoints.Select(d => (double)d.CpuPercent).ToList(), "CPU");
            
            if (settings.MonitorRam)
                DrawDataLine(canvas, dataPoints.Select(d => (double)d.RamPercent).ToList(), "RAM");
            
            if (settings.MonitorGpuCore)
                DrawDataLine(canvas, dataPoints.Select(d => (double)d.GpuCorePercent).ToList(), "GPUCore");
            
            if (settings.MonitorGpuVideo)
                DrawDataLine(canvas, dataPoints.Select(d => (double)d.GpuVideoPercent).ToList(), "GPUVideo");
            
            if (settings.MonitorGpuVram)
                DrawDataLine(canvas, dataPoints.Select(d => (double)d.GpuVramPercent).ToList(), "GPUVRAM");
        }

        /// <summary>
        /// Get color brush for a specific sensor type
        /// </summary>
        public static SolidColorBrush GetSensorBrush(string sensorType)
        {
            return new SolidColorBrush(ChartColors.ContainsKey(sensorType) ? ChartColors[sensorType] : Colors.Gray);
        }

        /// <summary>
        /// Convert canvas coordinates to data values
        /// </summary>
        public static double CanvasToDataValue(double canvasY, double canvasHeight)
        {
            var dataHeight = canvasHeight - (CHART_MARGIN * 2);
            var adjustedY = canvasY - CHART_MARGIN;
            return 100.0 - (adjustedY / dataHeight * 100.0);
        }

        /// <summary>
        /// Convert data values to canvas coordinates
        /// </summary>
        public static double DataToCanvasValue(double dataValue, double canvasHeight)
        {
            var dataHeight = canvasHeight - (CHART_MARGIN * 2);
            return CHART_MARGIN + dataHeight - (dataValue * dataHeight / 100.0);
        }
    }
}