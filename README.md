# Procmon

Procmon is a Windows application for monitoring system process performance in real-time. It supports both graphical user interface (GUI) and command-line interface (CLI) modes, providing flexible monitoring capabilities for CPU usage, memory consumption, and GPU utilization. The application features comprehensive sensor data collection, CSV logging, and real-time visualization with WPF-based charts and statistics.

## Features

### Dual Operation Modes

- **GUI Mode**: Interactive WPF interface with real-time charts, process selection dropdowns, and comprehensive statistics dashboard
- **CLI Mode**: Command-line operation with verbose output, CSV logging, and graceful interrupt handling
- **Unified Sensor System**: Identical performance monitoring capabilities across both modes
- **Cross-Mode Compatibility**: CSV output format compatible between GUI and CLI modes

### Performance Monitoring

- **CPU Usage**: Real-time processor utilization tracking per process
- **Memory Monitoring**: RAM usage in both absolute (MB) and percentage values
- **GPU Utilization**: Graphics processor core and video engine usage monitoring
- **VRAM Tracking**: Video memory consumption with detailed usage statistics
- **Process Detection**: Automatic process discovery with PID information
- **Exit Monitoring**: Automatic detection when monitored processes terminate

### Modern User Experience

- **WPF Interface**: Native Windows styling with responsive design
- **Real-time Charts**: Live performance visualization with multiple chart windows
- **Interactive Process Selection**: Dropdown-based process selection with search capabilities
- **Status Indicators**: Color-coded monitoring states and activity feedback

### Developer-Friendly Tools

- **CSV Export**: Structured data logging with timestamps for analysis
- **Command-line Integration**: Scriptable CLI interface for automation
- **Flexible Configuration**: Customizable monitoring intervals and duration settings

## User Guide

### Getting Started

#### GUI Mode (Default)

1. Launch Procmon without arguments: `Procmon.exe`
2. Select a process from the dropdown menu
3. Configure monitoring settings (interval, duration)
4. Click start to begin real-time monitoring
5. View live charts and statistics in the interface

#### CLI Mode

1. Launch with command-line arguments: `Procmon.exe [OPTIONS] [process_name]`
2. Use `-v` flag for verbose output during monitoring
3. Monitor data appears in real-time when verbose mode is enabled
4. Use Ctrl+C for graceful shutdown

### Command-Line Options

- `-d, --duration <seconds>`    Duration of monitoring session (0 for infinite, default: infinite)
- `-i, --interval <milliseconds>`  Interval between measurements (default: 100ms, minimum: 10ms)  
- `-f, --filename <path>`       Log file location and name (default: auto-generated)
- `-v, --verbose`              Print output to console during monitoring
- `-h, --help`                 Show help message and exit

**Note:** Windows-style arguments (`/d`, `/h`, `/?`) are also supported.

### Managing Monitoring

- **Process Selection**: Choose from running processes or specify by name
- **Real-time Updates**: Live performance data with automatic refresh
- **Data Export**: CSV files with comprehensive performance metrics
- **Graceful Shutdown**: Proper cleanup and data saving on exit

### Tips

- **Administrative Privileges**: May be required for monitoring certain system processes
- **Resource Usage**: Monitor system impact when using short intervals
- **Data Storage**: CSV files automatically timestamped to prevent overwrites

## CLI Examples

### Basic Usage

```bash
# Show help (works in same command prompt)
Procmon.exe --help
Procmon.exe -h
Procmon.exe /?

# Monitor notepad process with verbose output (Ctrl+C to stop)
Procmon.exe -v notepad

# Monitor for 5 minutes with custom interval and filename
Procmon.exe -d 300 -i 1000 -f "my_monitoring.csv" chrome

# Interactive process selection (no process name specified)
Procmon.exe -v

# Monitor specific process for 30 seconds, saving every 500ms
Procmon.exe --duration 30 --interval 500 --filename "quick_test.csv" firefox
```

### Advanced Scenarios

```bash
# Long-term monitoring with detailed logging
Procmon.exe --duration 3600 --interval 500 --verbose --filename "hourly_monitor.csv" "application.exe"

# Quick performance check
Procmon.exe -d 60 -i 100 -v chrome

# Background monitoring without console output
Procmon.exe -d 1800 -i 1000 -f "background_log.csv" notepad
```

## Implementation & Architecture

### Core Architecture

Procmon follows a clean MVVM (Model-View-ViewModel) architecture built on .NET Framework 4.8 and WPF:

- **Models**: Data structures for performance metrics, process information, and monitoring settings
- **ViewModels**: Business logic and data binding for UI components with INotifyPropertyChanged implementation
- **Views**: WPF XAML interfaces with responsive charts and controls
- **Services**: Dependency-injected services for monitoring, logging, and process management
- **Sensors**: Hardware-specific implementations for CPU, RAM, GPU, and VRAM monitoring

### Key Components

- **Process Management**: WMI-based process enumeration with intelligent filtering and PID tracking
- **Performance Sensors**: Hardware abstraction layer for consistent metrics across different system configurations
- **Chart Visualization**: Real-time WPF charting with zoom, pan, and multi-series support
- **CSV Logging**: Thread-safe data export with customizable formatting and automatic file management
- **Console Integration**: AttachConsole() implementation for seamless command-line operation

### Performance Optimizations

- **Background Threading**: All monitoring operations run on background threads to maintain UI responsiveness
- **Smart Caching**: Process information cached with automatic refresh on process changes
- **Resource Management**: Proper disposal patterns for WMI objects, file handles, and graphics resources
- **Memory Efficiency**: Optimized data structures and buffer management for long-term monitoring

## Output Format

CSV files contain the following columns with precise timestamps:

- Timestamp (yyyy-MM-dd HH:mm:ss.fff)
- CPU Usage (%)
- RAM Usage (MB)
- RAM Usage (%)
- GPU Core Usage (%)
- GPU Video Usage (%)
- VRAM Usage (MB)
- VRAM Usage (%)

## Build Instructions

Building via Visual Studio is straightforward: clone the repository, open the solution file, build, and run!

### Prerequisites

- **Visual Studio 2019** or later with these workloads:
  - .NET Desktop Development
  - Windows Presentation Foundation (WPF)
- **.NET Framework 4.8** SDK
- **Windows 10** or later for full GPU monitoring support

### Dependencies

The project uses the following NuGet packages:

- **Mono.Options** (5.3.0.1) - Command-line argument parsing
- **System.Management** - WMI queries for process and hardware monitoring
- **Microsoft.Windows.Compatibility** - Enhanced Windows API access

## System Requirements

- **Operating System**: Windows 10 or later
- **Framework**: .NET Framework 4.8
- **Memory**: 512 MB RAM minimum
- **Storage**: 50 MB available disk space
- **Privileges**: Administrative privileges may be required for some processes

## Error Handling

The application provides comprehensive error handling for common issues:

- **Invalid Arguments**: Clear error messages with usage suggestions
- **Process Access**: Graceful handling of insufficient privileges with alternative recommendations
- **File System**: Permission and path validation with user-friendly error messages
- **Hardware Access**: Fallback mechanisms for unavailable performance counters
- **Network Issues**: Timeout handling for remote process monitoring

## Technical Notes

### Console Mode Operation

- **Same Command Prompt**: Uses `AttachConsole()` to stay in the original command prompt instead of opening a new window
- **Enhanced Help**: Comprehensive help with examples and proper formatting
- **Proper Ctrl+C Handling**: Graceful shutdown that returns control to the command prompt properly
- **Responsive Interruption**: Can be stopped quickly even with long intervals (sleeps in 50ms chunks)

### GUI Mode Features

- **Real-time Visualization**: Live performance charts with customizable time ranges
- **Process Management**: Interactive process selection with search and filter capabilities
- **Statistics Dashboard**: Comprehensive analytics including averages, peaks, and trends
- **Export Functionality**: Multiple export formats with customizable data ranges
- **Settings Persistence**: User preferences saved between sessions

### Cross-Mode Compatibility

- **Unified Data Format**: Identical CSV structure regardless of operation mode
- **Consistent Sensors**: Same performance monitoring algorithms in both modes
- **Shared Configuration**: Settings and preferences apply to both GUI and CLI modes

## License

This project is licensed under the [**CC BY-NC-SA 4.0**](https://creativecommons.org/licenses/by-nc-sa/4.0/) license.
