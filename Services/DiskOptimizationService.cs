using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SystemOptimizer.Services
{
    public class DiskOptimizationService
    {
        public async Task<bool> OptimizeDiskAsync(string driveLetter, bool checkErrors = true, bool defragmentDisk = true, bool cleanSystemFiles = true)
        {
            try
            {
                if (string.IsNullOrEmpty(driveLetter) || driveLetter.Length < 1)
                {
                    System.Windows.MessageBox.Show("Необходимо указать корректную букву диска.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                
                // Убираем все лишнее из буквы диска, оставляя только букву
                driveLetter = driveLetter.Trim().Substring(0, 1);
                
                // Проверка на ошибки
                if (checkErrors)
                {
                    bool checkResult = await CheckDiskForErrorsAsync(driveLetter);
                    if (!checkResult)
                    {
                        System.Windows.MessageBox.Show($"Не удалось выполнить проверку диска {driveLetter}:.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                
                // Очистка системных файлов
                if (cleanSystemFiles)
                {
                    bool cleanResult = await CleanSystemFilesAsync(driveLetter);
                    if (!cleanResult)
                    {
                        System.Windows.MessageBox.Show($"Не удалось выполнить очистку системных файлов на диске {driveLetter}:.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                
                // Дефрагментация / TRIM
                if (defragmentDisk)
                {
                    bool defragResult = await DefragmentDiskAsync(driveLetter);
                    if (!defragResult)
                    {
                        System.Windows.MessageBox.Show($"Не удалось выполнить дефрагментацию/оптимизацию диска {driveLetter}:.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка оптимизации диска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        private async Task<bool> CheckDiskForErrorsAsync(string driveLetter)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Repair-Volume -DriveLetter {driveLetter} -Scan\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                
                var process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    return process.ExitCode == 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при проверке диска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        private async Task<bool> DefragmentDiskAsync(string driveLetter)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Optimize-Volume -DriveLetter {driveLetter}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                
                var process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    return process.ExitCode == 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при дефрагментации диска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        private async Task<bool> CleanSystemFilesAsync(string driveLetter)
        {
            try
            {
                // Очистка временных файлов системы
                bool result = await Task.Run(() => 
                {
                    // Запуск Disk Cleanup (cleanmgr) с параметрами
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "cleanmgr.exe",
                        Arguments = $"/d {driveLetter}: /sagerun:1",
                        UseShellExecute = true
                    };
                    
                    var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                    return false;
                });
                
                return result;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при очистке системных файлов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        public Dictionary<string, string> GetDrives()
        {
            Dictionary<string, string> drives = new Dictionary<string, string>();
            
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        string key = $"{drive.Name} ({drive.VolumeLabel})";
                        string info = $"{FormatSize(drive.TotalFreeSpace)} свободно из {FormatSize(drive.TotalSize)}";
                        
                        drives.Add(key, info);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при получении списка дисков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return drives;
        }
        
        private string FormatSize(long byteCount)
        {
            if (byteCount < 1024)
                return $"{byteCount} B";
            
            if (byteCount < 1048576) // 1024 * 1024
                return $"{Math.Round(byteCount / 1024.0, 2)} KB";
            
            if (byteCount < 1073741824) // 1024 * 1024 * 1024
                return $"{Math.Round(byteCount / 1048576.0, 2)} MB";
            
            return $"{Math.Round(byteCount / 1073741824.0, 2)} GB";
        }
    }
} 