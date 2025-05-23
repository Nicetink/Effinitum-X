using System;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;
using SystemOptimizer.Models;

namespace SystemOptimizer
{
    public partial class SplashScreen : Window
    {
        private DispatcherTimer timer;
        private string[] loadingMessages = new string[]
        {
            "Loading components...",
            "Checking system...",
            "Initializing modules...",
            "Preparing interface...",
            "Almost ready..."
        };
        private int messageIndex = 0;
        
        public SplashScreen()
        {
            try
            {
                InitializeComponent();
                
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1.2);
                timer.Tick += Timer_Tick;
                timer.Start();
                
                // Center window on screen
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                
                // Set window on top of others
                Topmost = true;
                
                // Log splash screen creation
                Logger.LogInfo("Splash screen created and started");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error creating splash screen", ex);
                
                System.Windows.MessageBox.Show($"Error creating loading window: {ex.Message}", 
                               "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Close window in case of error
                this.Close();
            }
        }
        
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (messageIndex >= loadingMessages.Length)
                {
                    // If all messages are shown, create main window
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // Create and show main window
                            var mainWindow = new MainWindow();
                            if (mainWindow != null)
                            {
                                mainWindow.Show();
                                
                                // Stop timer and close splash screen
                                timer.Stop();
                                Close();
                                
                                // Set main window as application's main window
                                System.Windows.Application.Current.MainWindow = mainWindow;
                                
                                // Change application shutdown mode
                                System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                                
                                // Log successful main window creation
                                Logger.LogInfo("Main window created and displayed");
                            }
                            else
                            {
                                throw new InvalidOperationException("Failed to create application main window");
                            }
                        }
                        catch (Exception ex)
                        {
                            timer.Stop();
                            Logger.LogError("Error creating main window", ex);
                            
                            System.Windows.MessageBox.Show($"Error creating main window: {ex.Message}", 
                                          "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            
                            // Close application on error
                            System.Windows.Application.Current.Shutdown();
                        }
                    });
                }
                else
                {
                    // Display next loading message
                    tbStatus.Text = loadingMessages[messageIndex++];
                    
                    // Update progress bar
                    double progress = (double)messageIndex / loadingMessages.Length;
                    pbProgress.Value = progress * 100;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in splash screen timer", ex);
                
                // In case of error, stop timer and close splash screen
                timer.Stop();
                Close();
                
                System.Windows.MessageBox.Show($"Loading error: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Close application on error
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
} 