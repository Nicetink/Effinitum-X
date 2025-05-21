using System;
using System.Windows;
using System.IO;
using SystemOptimizer.Models;

namespace SystemOptimizer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            
            // Initialize and show SplashScreen
            var splashScreen = new SplashScreen();
            if (splashScreen != null)
            {
                splashScreen.Show();
                
                // Log successful launch
                try
                {
                    Logger.LogInfo("Application launched, loading screen displayed");
                }
                catch { /* Ignore logging errors */ }
                
                // Prevent main window from opening automatically
                // Instead of setting null, we use a different approach
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
            else
            {
                throw new InvalidOperationException("Failed to create loading screen");
            }
        }
        catch (Exception ex)
        {
            try
            {
                Logger.LogError("Critical application initialization error", ex);
            }
            catch 
            {
                // If even the logger doesn't work, write directly to file
                try
                {
                    File.AppendAllText("critical_error.log", $"{DateTime.Now}: CRITICAL ERROR: {ex.Message}\n{ex.StackTrace}\n");
                }
                catch { /* Ignore logging errors */ }
            }
            
            MessageBox.Show($"Critical application initialization error: {ex.Message}", 
                           "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

