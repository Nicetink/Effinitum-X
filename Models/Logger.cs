using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SystemOptimizer.Models
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SystemOptimizer", 
            "logs", 
            $"app_log_{DateTime.Now:yyyyMMdd}.txt");
            
        private static readonly object _lockObject = new object();
        
        static Logger()
        {
            try
            {
                // Создаем директорию для логов, если она не существует
                string logDirectory = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(logDirectory) && logDirectory != null)
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
            catch
            {
                // Игнорируем ошибки при создании директории для логов
            }
        }
        
        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }
        
        public static void LogWarning(string message)
        {
            Log("WARNING", message);
        }
        
        public static void LogError(string message, Exception exception = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(message);
            
            if (exception != null)
            {
                sb.AppendLine();
                sb.Append("Exception: ");
                sb.Append(exception.Message);
                sb.AppendLine();
                sb.Append("Stack Trace: ");
                sb.Append(exception.StackTrace);
            }
            
            Log("ERROR", sb.ToString());
        }
        
        public static async Task LogInfoAsync(string message)
        {
            await LogAsync("INFO", message);
        }
        
        public static async Task LogWarningAsync(string message)
        {
            await LogAsync("WARNING", message);
        }
        
        public static async Task LogErrorAsync(string message, Exception exception = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(message);
            
            if (exception != null)
            {
                sb.AppendLine();
                sb.Append("Exception: ");
                sb.Append(exception.Message);
                sb.AppendLine();
                sb.Append("Stack Trace: ");
                sb.Append(exception.StackTrace);
            }
            
            await LogAsync("ERROR", sb.ToString());
        }
        
        private static void Log(string level, string message)
        {
            try
            {
                lock (_lockObject)
                {
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Игнорируем ошибки при логировании
            }
        }
        
        private static async Task LogAsync(string level, string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                await File.AppendAllTextAsync(LogFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Игнорируем ошибки при логировании
            }
        }
    }
} 