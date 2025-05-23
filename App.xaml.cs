using System;
using System.Windows;
using System.IO;
using SystemOptimizer.Models;
using ModernWpf;
using System.Threading.Tasks;

#nullable enable

namespace SystemOptimizer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private Settings _settings = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Регистрируем обработчики необработанных исключений
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        
        try
        {
            base.OnStartup(e);
            
            // Загружаем настройки
            _settings = Settings.Load();
            
            // Применяем тему из настроек
            ApplyTheme();
            
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
            
            System.Windows.MessageBox.Show($"Critical application initialization error: {ex.Message}", 
                           "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ApplyTheme()
    {
        switch (_settings.AppTheme)
        {
            case SystemOptimizer.Models.ThemeMode.Light:
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                break;
            case SystemOptimizer.Models.ThemeMode.Dark:
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                break;
            case SystemOptimizer.Models.ThemeMode.System:
                ThemeManager.Current.ApplicationTheme = null; // Use system setting
                break;
        }
    }
    
    // Обработчик исключений в UI потоке
    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException("UI Thread Exception", e.Exception);
        e.Handled = true; // Помечаем как обработанное, чтобы приложение не завершалось
    }

    // Обработчик необработанных исключений в домене приложения
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        HandleException("Application Domain Exception", e.ExceptionObject as Exception);
    }

    // Обработчик необработанных исключений в задачах
    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException("Task Exception", e.Exception);
        e.SetObserved(); // Помечаем как отслеженное, чтобы не было влияния на поток
    }

    // Общий метод обработки исключений
    private void HandleException(string source, Exception? ex)
    {
        try
        {
            // Логгируем исключение
            Logger.LogError($"Unhandled exception: {source}", ex);
            
            // Записываем в отдельный файл для критических ошибок
            string errorFile = "unhandled_exceptions.log";
            File.AppendAllText(errorFile, $"{DateTime.Now} - {source}: {ex?.Message}\n{ex?.StackTrace}\n\n");
            
            // Показываем сообщение пользователю только для исключений UI потока
            if (source == "UI Thread Exception")
            {
                System.Windows.MessageBox.Show($"An error occurred in the application:\n{ex?.Message}\n\nThe error has been logged. Please restart the application if necessary.",
                    "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch
        {
            // Если не удалось обработать исключение, то пытаемся записать напрямую в файл
            try
            {
                File.AppendAllText("critical_error.log", $"{DateTime.Now} - Failed to handle exception: {source} - {ex?.Message}\n{ex?.StackTrace}\n\n");
            }
            catch
            {
                // Игнорируем все ошибки в обработчике исключений
            }
        }
    }
}

