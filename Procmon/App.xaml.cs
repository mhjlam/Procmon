using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Procmon.Services;
using Procmon.Views;

namespace Procmon
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Combined GUI/Console Application Entry Point
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Check if command-line arguments are provided
            if (e.Args.Length > 0)
            {
                // Run in console mode - suppress WPF startup events
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                
                var consoleService = new ConsoleService();
                int exitCode = consoleService.RunConsoleMode(e.Args);
                
                // Exit the application after console mode completes
                this.Shutdown(exitCode);
                return;
            }

            // No arguments provided, run in GUI mode
            base.OnStartup(e);
            
            // Set normal WPF shutdown behavior for GUI mode
            this.ShutdownMode = ShutdownMode.OnLastWindowClose;
            
            // Create and show the main window
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}