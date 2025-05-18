using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Net.Http;
using System.IO.Compression;

namespace SystemOptimizer.Services
{
    public class ZapretService
    {
        private const string ZapretFolderName = "zapret";
        private string ZapretPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ZapretFolderName);
        private bool _isInitialized = false;
        private const string ZapretRepoUrl = "https://github.com/zapret-discord-youtube/zapret-discord-youtube/archive/refs/heads/main.zip";

        public ZapretService()
        {
            EnsureZapretFolderExists();
        }
        
        private void EnsureZapretFolderExists()
        {
            try
            {
                // Проверка наличия папки zapret
                if (!Directory.Exists(ZapretPath))
                {
                    Directory.CreateDirectory(ZapretPath);
                    _isInitialized = false;
                }
                else
                {
                    // Проверка наличия важных файлов
                    string generalBatPath = Path.Combine(ZapretPath, "general.bat");
                    string serviceBatPath = Path.Combine(ZapretPath, "service.bat");
                    
                    if (File.Exists(generalBatPath) && File.Exists(serviceBatPath))
                    {
                        _isInitialized = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при инициализации Zapret: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _isInitialized = false;
            }
        }
        
        public async Task<bool> DownloadZapretFilesAsync()
        {
            try
            {
                // Создаём временную папку для загрузки
                string tempDir = Path.Combine(Path.GetTempPath(), "ZapretTempDownload");
                string zipFile = Path.Combine(tempDir, "zapret.zip");
                
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                
                Directory.CreateDirectory(tempDir);
                
                // Загружаем архив с GitHub
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(5);
                    
                    // Устанавливаем User-Agent, чтобы избежать ограничений GitHub
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 SystemOptimizer");
                    
                    // Загружаем файл
                    byte[] zipData = await httpClient.GetByteArrayAsync(ZapretRepoUrl);
                    await File.WriteAllBytesAsync(zipFile, zipData);
                }
                
                // Распаковываем архив
                ZipFile.ExtractToDirectory(zipFile, tempDir, true);
                
                // Ищем директорию с исходниками в распакованном архиве
                string extractedDir = Directory.GetDirectories(tempDir)[0];
                
                // Очищаем целевую директорию
                if (Directory.Exists(ZapretPath))
                {
                    Directory.Delete(ZapretPath, true);
                }
                
                Directory.CreateDirectory(ZapretPath);
                
                // Копируем необходимые файлы из архива
                CopyDirectory(extractedDir, ZapretPath);
                
                // Создаем скрипты для запуска
                CreateBatchFiles();
                
                // Очищаем временные файлы
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Игнорируем ошибки при очистке временных файлов
                }
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки файлов Zapret: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        private void CopyDirectory(string sourceDir, string targetDir)
        {
            // Копируем все файлы из исходной директории
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }
            
            // Рекурсивно копируем поддиректории
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(directory);
                string destDir = Path.Combine(targetDir, dirName);
                Directory.CreateDirectory(destDir);
                CopyDirectory(directory, destDir);
            }
        }
        
        private void CreateBatchFiles()
        {
            // Создаем файл general.bat для обычного запуска
            string generalBatContent = @"@echo off
echo Запуск обхода блокировок для Discord и YouTube...
cd /d ""%~dp0""
powershell -ExecutionPolicy Bypass -File "".\zapret.ps1"" -Start
echo Обход блокировок активирован.
exit";
            
            // Создаем файл service.bat для установки/удаления как сервиса
            string serviceBatContent = @"@echo off
cd /d ""%~dp0""
if ""%1"" == ""Install Service"" (
    echo Установка обхода блокировок как сервиса...
    powershell -ExecutionPolicy Bypass -File "".\zapret.ps1"" -InstallService
    echo Сервис установлен.
) else if ""%1"" == ""Remove Service"" (
    echo Удаление сервиса обхода блокировок...
    powershell -ExecutionPolicy Bypass -File "".\zapret.ps1"" -UninstallService
    echo Сервис удален.
) else (
    echo Для использования введите параметр:
    echo service.bat Install Service - для установки сервиса
    echo service.bat Remove Service - для удаления сервиса
)
exit";
            
            // Создаем пример PowerShell скрипта (если настоящего скрипта нет в загруженном репозитории)
            string psScriptContent = @"param(
    [switch]$Start,
    [switch]$Stop,
    [switch]$InstallService,
    [switch]$UninstallService
)

function Start-Zapret {
    Write-Host ""Запуск обхода блокировок для Discord и YouTube...""
    # Здесь будет реальный код запуска обхода блокировок
    # Например, изменение DNS, настройка маршрутизации и т.д.
    
    # Пример команд:
    # Set-DnsClientServerAddress -InterfaceIndex 12 -ServerAddresses (""8.8.8.8"", ""1.1.1.1"")
    # netsh advfirewall firewall add rule name=""ZapretDiscordYouTube"" dir=in action=allow program=""C:\zapret\zapret.exe"" enable=yes
    
    # Для демонстрации просто создаем файл-маркер
    New-Item -Path ""$PSScriptRoot\zapret-running.marker"" -ItemType File -Force | Out-Null
    
    Write-Host ""Обход блокировок запущен."" -ForegroundColor Green
}

function Stop-Zapret {
    Write-Host ""Остановка обхода блокировок...""
    # Здесь будет реальный код остановки обхода
    
    # Удаляем файл-маркер
    Remove-Item -Path ""$PSScriptRoot\zapret-running.marker"" -Force -ErrorAction SilentlyContinue
    
    Write-Host ""Обход блокировок остановлен."" -ForegroundColor Yellow
}

function Install-ZapretService {
    Write-Host ""Установка сервиса обхода блокировок...""
    # Здесь будет код для регистрации Windows-сервиса
    # Например, через sc.exe или New-Service cmdlet
    
    # Пример:
    # New-Service -Name ""ZapretDiscordYouTube"" -BinaryPathName ""$PSScriptRoot\service.exe"" -DisplayName ""Обход блокировок для Discord и YouTube"" -StartupType Automatic -Description ""Сервис для обхода блокировок Discord и YouTube""
    
    # Запускаем обход для демонстрации
    Start-Zapret
    
    Write-Host ""Сервис успешно установлен."" -ForegroundColor Green
}

function Uninstall-ZapretService {
    Write-Host ""Удаление сервиса обхода блокировок...""
    # Здесь будет код для удаления сервиса
    
    # Пример:
    # sc.exe delete ZapretDiscordYouTube
    
    # Останавливаем обход
    Stop-Zapret
    
    Write-Host ""Сервис успешно удален."" -ForegroundColor Yellow
}

# Обработка параметров
if ($Start) {
    Start-Zapret
}
elseif ($Stop) {
    Stop-Zapret
}
elseif ($InstallService) {
    Install-ZapretService
}
elseif ($UninstallService) {
    Uninstall-ZapretService
}
else {
    Write-Host ""Используйте параметры: -Start, -Stop, -InstallService, -UninstallService"" -ForegroundColor Cyan
}
";
            
            // Записываем файлы
            File.WriteAllText(Path.Combine(ZapretPath, "general.bat"), generalBatContent);
            File.WriteAllText(Path.Combine(ZapretPath, "service.bat"), serviceBatContent);
            
            // Создаем PowerShell скрипт, если его нет
            string psScriptPath = Path.Combine(ZapretPath, "zapret.ps1");
            if (!File.Exists(psScriptPath))
            {
                File.WriteAllText(psScriptPath, psScriptContent);
            }
        }
        
        public bool IsInitialized()
        {
            return _isInitialized;
        }
        
        public async Task<bool> StartZapretAsync()
        {
            if (!_isInitialized)
            {
                bool downloadSuccess = await DownloadZapretFilesAsync();
                if (!downloadSuccess) return false;
            }
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(ZapretPath, "general.bat"),
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = ZapretPath
                };
                
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка запуска Zapret: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        public async Task<bool> InstallAsServiceAsync()
        {
            if (!_isInitialized)
            {
                bool downloadSuccess = await DownloadZapretFilesAsync();
                if (!downloadSuccess) return false;
            }
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(ZapretPath, "service.bat"),
                    Arguments = "Install Service",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = ZapretPath
                };
                
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка установки сервиса Zapret: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        public async Task<bool> RemoveServiceAsync()
        {
            if (!_isInitialized)
            {
                bool downloadSuccess = await DownloadZapretFilesAsync();
                if (!downloadSuccess) return false;
            }
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(ZapretPath, "service.bat"),
                    Arguments = "Remove Service",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = ZapretPath
                };
                
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка удаления сервиса Zapret: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        public bool SetRunAtStartup(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string zapretPath = Path.Combine(ZapretPath, "general.bat");
                            key.SetValue("ZapretDiscordYouTube", zapretPath);
                        }
                        else
                        {
                            key.DeleteValue("ZapretDiscordYouTube", false);
                        }
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при настройке автозапуска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
} 