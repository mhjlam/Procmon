using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Procmon.Models;
using Procmon.Sensors;

namespace Procmon.Services
{
    /// <summary>
    /// Service for handling command-line operations
    /// </summary>
    public class ConsoleService
    {
        private StreamWriter writer;
        private volatile bool shouldStop = false;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        private void Log(string message, bool writeToConsole = false)
        {
            writer?.WriteLine(message);

            if (writeToConsole)
            {
                Console.WriteLine(message);
            }

            writer?.Flush();
        }

        public int RunConsoleMode(string[] args)
        {
            bool consoleAttached = false;
            
            try
            {
                // Try to attach to parent console first (for same prompt behavior)
                consoleAttached = AttachConsole(ATTACH_PARENT_PROCESS);
                
                if (!consoleAttached)
                {
                    // If that fails, allocate a new console
                    if (GetConsoleWindow() == IntPtr.Zero)
                    {
                        AllocConsole();
                    }
                }

                bool showHelp = false;
                bool verbose = false;

                TimeSpan timeRemaining;
                DateTime startLogTime = DateTime.Now;
                TimeSpan logDuration = TimeSpan.Zero; // Default to infinite (0 duration)
                TimeSpan logInterval = new TimeSpan(0, 0, 0, 0, 100);

                string processName = "explorer";
                string logFileName = string.Format("Procmon-{0:yyyyMMdd_HHmms}.csv", DateTime.Now);

                List<string> nonOptionalArgs = new List<string>();

                int j = 1;
                while (File.Exists(logFileName))
                {
                    logFileName = Path.GetFileNameWithoutExtension(logFileName) + $"-{j++}.csv";
                }

                // Enhanced argument parsing to handle edge cases better
                for (int i = 0; i < args.Length; i++)
                {
                    string originalArg = args[i]; // Keep original for error messages
                    string arg = originalArg?.ToLower()?.Trim() ?? "";
                    
                    // Skip empty arguments
                    if (string.IsNullOrWhiteSpace(originalArg))
                        continue;
                    
                    // Handle special cases for malformed arguments
                    if (originalArg == "-" || originalArg == "/" || originalArg == "--")
                    {
                        Console.Error.WriteLine($"Error: Invalid argument '{originalArg}' - missing option");
                        Console.Error.WriteLine("Use -h or --help for usage information");
                        SendNewlineToParentConsole(consoleAttached);
                        return 1;
                    }
                    
                    switch (arg)
                    {
                        case "-d":
                        case "--duration":
                        case "/d":
                            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int duration))
                            {
                                logDuration = duration > 0 ? new TimeSpan(0, 0, duration) : TimeSpan.Zero;
                                i++; // Skip the next argument
                            }
                            else
                            {
                                Console.Error.WriteLine($"Error: {originalArg} requires a numeric value");
                                if (i + 1 >= args.Length)
                                    Console.Error.WriteLine("No value provided after the argument");
                                else
                                    Console.Error.WriteLine($"'{args[i + 1]}' is not a valid number");
                                SendNewlineToParentConsole(consoleAttached);
                                return 1;
                            }
                            break;
                        case "-i":
                        case "--interval":
                        case "/i":
                            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int interval))
                            {
                                logInterval = new TimeSpan(0, 0, 0, 0, interval);
                                i++; // Skip the next argument
                            }
                            else
                            {
                                Console.Error.WriteLine($"Error: {originalArg} requires a numeric value");
                                if (i + 1 >= args.Length)
                                    Console.Error.WriteLine("No value provided after the argument");
                                else
                                    Console.Error.WriteLine($"'{args[i + 1]}' is not a valid number");
                                SendNewlineToParentConsole(consoleAttached);
                                return 1;
                            }
                            break;
                        case "-f":
                        case "--filename":
                        case "/f":
                            if (i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]))
                            {
                                logFileName = args[i + 1];
                                i++; // Skip the next argument
                            }
                            else
                            {
                                Console.Error.WriteLine($"Error: {originalArg} requires a filename");
                                SendNewlineToParentConsole(consoleAttached);
                                return 1;
                            }
                            break;
                        case "-v":
                        case "--verbose":
                        case "/v":
                            verbose = true;
                            break;
                        case "-h":
                        case "--help":
                        case "/h":
                        case "/?":
                            showHelp = true;
                            break;
                        default:
                            // Handle arguments that start with - or / but are unrecognized
                            if (originalArg.StartsWith("-") || originalArg.StartsWith("/"))
                            {
                                Console.Error.WriteLine($"Error: Unknown argument '{originalArg}'");
                                Console.Error.WriteLine("Use -h or --help for usage information");
                                SendNewlineToParentConsole(consoleAttached);
                                return 1;
                            }
                            else
                            {
                                // Treat as process name
                                nonOptionalArgs.Add(originalArg);
                            }
                            break;
                    }
                }

                // Validate filename early
                try
                {
                    var fileInfo = new FileInfo(logFileName);
                    // Check if directory exists and is writable
                    var directory = Path.GetDirectoryName(Path.GetFullPath(logFileName));
                    if (!Directory.Exists(directory))
                    {
                        Console.Error.WriteLine($"Error: Directory does not exist: {directory}");
                        SendNewlineToParentConsole(consoleAttached);
                        return 1;
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error: Invalid log file '{logFileName}': {e.Message}");
                    SendNewlineToParentConsole(consoleAttached);
                    return 1;
                }

                // Append .csv extension to specified log file if necessary
                if (Path.GetExtension(logFileName) != ".csv")
                {
                    logFileName += ".csv";
                }

                if (showHelp)
                {
                    Console.WriteLine("Procmon - Process Performance Monitor");
                    Console.WriteLine();
                    Console.WriteLine("Usage: Procmon [OPTIONS] [process_name]");
                    Console.WriteLine();
                    Console.WriteLine("Run performance monitor on specified process.");
                    Console.WriteLine("By default this program runs infinitely until manually stopped.");
                    Console.WriteLine();
                    Console.WriteLine("Options:");
                    Console.WriteLine("  -d, --duration <seconds>      Duration of the session (in seconds, 0 for infinite)");
                    Console.WriteLine("  -i, --interval <milliseconds> Interval between recording steps (in milliseconds)");
                    Console.WriteLine("  -f, --filename <path>         Location and name of the log file");
                    Console.WriteLine("  -v, --verbose                 Print logging output to screen");
                    Console.WriteLine("  -h, --help                    Show this message and exit");
                    Console.WriteLine();
                    Console.WriteLine("Examples:");
                    Console.WriteLine("  Procmon                   Start GUI mode");
                    Console.WriteLine("  Procmon -h                Show this help");
                    Console.WriteLine("  Procmon -v notepad        Monitor notepad with verbose output");
                    Console.WriteLine("  Procmon -d 300 -i 1000 -f \"log.csv\" chrome");
                    Console.WriteLine("                                Monitor chrome for 5 minutes, log every second");
                    Console.WriteLine();
                    
                    // Send proper newline to return control to parent console
                    SendNewlineToParentConsole(consoleAttached);
                    return 0;
                }

                // Validate that at least basic parameters are reasonable
                if (logInterval.TotalMilliseconds < 10)
                {
                    Console.Error.WriteLine("Error: Interval too small (minimum 10ms)");
                    SendNewlineToParentConsole(consoleAttached);
                    return 1;
                }

                if (logDuration.TotalSeconds < 0)
                {
                    Console.Error.WriteLine("Error: Duration cannot be negative");
                    SendNewlineToParentConsole(consoleAttached);
                    return 1;
                }

                // Allow user to select process from list if no process name was given
                uint selection = 0;
                Process targetProcess;
                List<Process> processes = new List<Process>();

                foreach (Process process in Process.GetProcesses().Where(p => p.MainWindowTitle.Length > 0).OrderBy(p => p.ProcessName).ToList())
                {
                    try
                    {
                        ProcessModule processModule = process.MainModule;
                        processes.Add(process);
                    }
                    catch (Exception) { }
                }

                if (nonOptionalArgs.Count == 0)
                {
                    Console.WriteLine("Select the target process to monitor:");
                    Console.WriteLine();

                    for (int i = 0; i < processes.Count; ++i)
                    {
                        try
                        {
                            Console.WriteLine("[{0,2}]  {1,-20}  {2}", i + 1, processes[i].ProcessName, processes[i].MainModule.FileName);
                        }
                        catch (Exception) { }
                    }

                    Console.WriteLine();
                    Console.Write("Enter process number: ");
                    string selectionString = Console.ReadLine();
                    if (!uint.TryParse(selectionString, out selection))
                    {
                        Console.Error.WriteLine("Error: Invalid input - must be a number");
                        SendNewlineToParentConsole(consoleAttached);
                        return 1;
                    }

                    if (selection > processes.Count || selection == 0)
                    {
                        Console.Error.WriteLine("Error: Invalid selection - number out of range");
                        SendNewlineToParentConsole(consoleAttached);
                        return 1;
                    }

                    processName = processes[(int)selection - 1].ProcessName;
                }
                else if (nonOptionalArgs.Count > 0)
                {
                    processName = nonOptionalArgs[0];
                }

                // Make sure that process is currently running
                List<Process> matchingProcesses = processes.FindAll(x => x.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

                if (matchingProcesses.Any(p => p.MainWindowTitle.Length == 0))
                {
                    Console.Error.WriteLine($"Error: Access is denied to target process '{processName}'.");
                    SendNewlineToParentConsole(consoleAttached);
                    return 1;
                }

                if (matchingProcesses.Count == 0)
                {
                    Console.Error.WriteLine($"Error: Target process '{processName}' is not currently running.");
                    Console.WriteLine("Available processes:");
                    foreach (var proc in processes.Take(10))
                    {
                        Console.WriteLine($"  {proc.ProcessName}");
                    }
                    if (processes.Count > 10)
                        Console.WriteLine($"  ... and {processes.Count - 10} more");
                    SendNewlineToParentConsole(consoleAttached);
                    return 1;
                }

                // Ask for more input if more than one process with target name is running
                selection = 0;
                if (matchingProcesses.Count > 1)
                {
                    Console.WriteLine($"Multiple processes named '{processName}' are running. Select the target process to monitor:");
                    Console.WriteLine();

                    for (int i = 0; i < matchingProcesses.Count; ++i)
                    {
                        try
                        {
                            Console.WriteLine("[{0,2}]  PID: {1,-8}  {2}", i + 1, matchingProcesses[i].Id, matchingProcesses[i].MainModule.FileName);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("[{0,2}]  PID: {1,-8}  <Access Denied>", i + 1, matchingProcesses[i].Id);
                        }
                    }

                    Console.WriteLine();
                    Console.Write("Enter process number: ");
                    ConsoleKeyInfo cki = Console.ReadKey(true);
                    Console.WriteLine(cki.KeyChar);
                    
                    if (char.IsDigit(cki.KeyChar))
                    {
                        selection = (uint)(cki.KeyChar - '0') - 1;
                        if (selection >= matchingProcesses.Count)
                        {
                            Console.Error.WriteLine("Error: Invalid selection");
                            SendNewlineToParentConsole(consoleAttached);
                            return 1;
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: Invalid input - must be a number");
                        SendNewlineToParentConsole(consoleAttached);
                        return 1;
                    }
                }

                targetProcess = matchingProcesses[(int)selection];
                processName = targetProcess.ProcessName;

                // Obtain load sensors for target process
                CpuSensor cpuSensor = new CpuSensor(targetProcess);
                RamSensor ramSensor = new RamSensor(targetProcess);
                GpuCoreSensor gpuSensor = new GpuCoreSensor();
                GpuVideoSensor videoSensor = new GpuVideoSensor();
                GpuVramSensor vramSensor = new GpuVramSensor();

                DateTime now = DateTime.Now;
                startLogTime = now;
                logFileName = string.Format("Procmon-{0}-{1:yyyyMMdd_HHmms}.csv", processName, now);

                bool fileExists = File.Exists(logFileName);
                writer = new StreamWriter(new FileStream(logFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite));

                if (!fileExists)
                {
                    // Log header
                    string header = "\"Timestamp\",";
                    header += "\"" + cpuSensor.Name + " (%)\",";
                    header += "\"" + ramSensor.Name + " (MB)\",";
                    header += "\"" + ramSensor.Name + " (%)\",";
                    header += "\"" + gpuSensor.Name + " (%)\",";
                    header += "\"" + videoSensor.Name + " (%)\",";
                    header += "\"" + vramSensor.Name + " (MB)\",";
                    header += "\"" + vramSensor.Name + " (%)\"";

                    Log(header, verbose);
                }

                // Determine if monitoring is infinite
                bool isInfinite = (logDuration == TimeSpan.Zero);

                if (verbose)
                {
                    Console.WriteLine($"Monitoring process: {processName} (PID: {targetProcess.Id})");
                    Console.WriteLine($"Log file: {logFileName}");
                    Console.WriteLine($"Interval: {logInterval.TotalMilliseconds}ms");
                    
                    if (isInfinite)
                    {
                        Console.WriteLine("Duration: Infinite (Press Ctrl+C to stop)");
                    }
                    else
                    {
                        Console.WriteLine($"Duration: {logDuration.TotalSeconds} seconds");
                    }
                    Console.WriteLine("Starting monitoring...");
                    Console.WriteLine();
                }

                // Set up Ctrl+C handler for graceful shutdown
                Console.CancelKeyPress += (sender, e) =>
                {
                    if (verbose)
                        Console.WriteLine("\nShutting down gracefully...");
                    e.Cancel = true; // Prevent immediate termination
                    shouldStop = true; // Signal the monitoring loop to stop
                };

                while (!shouldStop)
                {
                    // Exit if process ends during monitoring
                    if (targetProcess.HasExited)
                    {
                        if (verbose)
                            Console.WriteLine("Target process has exited. Stopping monitoring.");
                        break;
                    }

                    // For timed monitoring, check if duration has passed
                    if (!isInfinite)
                    {
                        timeRemaining = logDuration - (DateTime.Now - startLogTime);
                        if (timeRemaining <= TimeSpan.Zero)
                        {
                            if (verbose)
                                Console.WriteLine("Monitoring duration completed.");
                            break;
                        }
                    }

                    // Get new counter values for target process
                    targetProcess.Refresh();

                    // Log sensor data
                    float cpu = cpuSensor.NextValue();
                    float ram = ramSensor.NextValue();
                    float gpu = gpuSensor.NextValue();
                    float video = videoSensor.NextValue();
                    float vram = vramSensor.NextValue();

                    string value = "\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",";
                    value += "\"" + cpu + "\",";
                    value += "\"" + ram + "\",";
                    value += "\"" + ram / ramSensor.TotalRam * 100f + "\",";
                    value += "\"" + gpu + "\",";
                    value += "\"" + video + "\",";
                    value += "\"" + vram + "\",";
                    value += "\"" + vram / vramSensor.TotalVram * 100f + "\"";

                    Log(value, verbose);

                    // Use a more responsive sleep mechanism that can be interrupted
                    int sleepTime = Math.Max(10, logInterval.Milliseconds);
                    int sleptTime = 0;
                    
                    while (sleptTime < sleepTime && !shouldStop)
                    {
                        Thread.Sleep(Math.Min(50, sleepTime - sleptTime)); // Sleep in 50ms chunks
                        sleptTime += 50;
                    }
                }

                // Clean shutdown
                writer?.Close();
                
                // Show stop message immediately, before any WPF shutdown processes
                if (shouldStop && verbose)
                {
                    Console.WriteLine("Monitoring stopped by user.");
                }
                
                // Send proper newline to return control to parent console
                SendNewlineToParentConsole(consoleAttached);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Details: {ex.InnerException.Message}");
                }
                writer?.Close();
                SendNewlineToParentConsole(consoleAttached);
                return 1;
            }
        }

        /// <summary>
        /// Send a newline to the parent console to ensure proper prompt return
        /// This is necessary for WPF applications that attach to parent console
        /// </summary>
        private void SendNewlineToParentConsole(bool consoleAttached)
        {
            if (consoleAttached)
            {
                try
                {
                    // Send a newline to ensure the parent console prompt returns properly
                    Console.Write("\r\n");
                    Console.Out.Flush();
                }
                catch
                {
                    // Ignore any errors when trying to send newline
                }
            }
        }
    }
}