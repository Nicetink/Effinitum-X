using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Markup;
using System.Windows.Shapes;
using SystemOptimizer.Models;

namespace SystemOptimizer.Services
{
    public class UwpAppService
    {
        public async Task<List<UwpApp>> GetInstalledUwpAppsAsync()
        {
            var apps = new List<UwpApp>();
            
            await Task.Run(() => 
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-Command \"Get-AppxPackage | Select-Object Name, PackageFullName, Publisher, Version, InstallLocation | ConvertTo-Json\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    
                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();
                            
                            // Примечание: здесь должен быть полноценный разбор JSON
                            // Для примера делаем упрощенную обработку
                            
                            // Разбираем JSON вручную
                            string[] appEntries = output.Split(new[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                            
                            foreach (var entry in appEntries)
                            {
                                try
                                {
                                    var app = new UwpApp();
                                    
                                    // Извлекаем имя
                                    int nameStart = entry.IndexOf("\"Name\":") + 8;
                                    int nameEnd = entry.IndexOf(",", nameStart) - 1;
                                    app.Name = entry.Substring(nameStart, nameEnd - nameStart).Trim('"');
                                    
                                    // Извлекаем полное имя пакета
                                    int fullNameStart = entry.IndexOf("\"PackageFullName\":") + 19;
                                    int fullNameEnd = entry.IndexOf(",", fullNameStart) - 1;
                                    app.PackageFullName = entry.Substring(fullNameStart, fullNameEnd - fullNameStart).Trim('"');
                                    
                                    // Извлекаем издателя
                                    int publisherStart = entry.IndexOf("\"Publisher\":") + 13;
                                    int publisherEnd = entry.IndexOf(",", publisherStart) - 1;
                                    app.Publisher = entry.Substring(publisherStart, publisherEnd - publisherStart).Trim('"');
                                    
                                    // Извлекаем версию
                                    int versionStart = entry.IndexOf("\"Version\":") + 11;
                                    int versionEnd = entry.IndexOf(",", versionStart) - 1;
                                    app.Version = entry.Substring(versionStart, versionEnd - versionStart).Trim('"');
                                    
                                    // Извлекаем путь установки
                                    int locationStart = entry.IndexOf("\"InstallLocation\":") + 19;
                                    int locationEnd = entry.IndexOf("}", locationStart) - 1;
                                    string installLocation = entry.Substring(locationStart, locationEnd - locationStart).Trim('"');
                                    
                                    // Вычисляем размер
                                    long size = CalculateDirectorySize(installLocation);
                                    app.Size = FormatSize(size);
                                    
                                    // Добавляем команду удаления
                                    app.UninstallCommand = new RelayCommand(async (parameter) => 
                                    {
                                        await UninstallUwpAppAsync(app.PackageFullName);
                                    });
                                    
                                    apps.Add(app);
                                }
                                catch
                                {
                                    // Пропускаем записи, которые не удалось разобрать
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при получении списка UWP приложений: {ex.Message}", 
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
            
            return apps;
        }
        
        public async Task<bool> UninstallUwpAppAsync(string packageFullName)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Remove-AppxPackage -Package '{packageFullName}'\"",
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
                System.Windows.MessageBox.Show($"Ошибка при удалении приложения: {ex.Message}", 
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        private long CalculateDirectorySize(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    return 0;
                
                long size = 0;
                
                // Получаем размер всех файлов в директории
                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        size += fileInfo.Length;
                    }
                    catch
                    {
                        // Пропускаем файлы, к которым нет доступа
                    }
                }
                
                // Рекурсивно получаем размер подпапок
                foreach (var dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        size += CalculateDirectorySize(dir);
                    }
                    catch
                    {
                        // Пропускаем папки, к которым нет доступа
                    }
                }
                
                return size;
            }
            catch
            {
                return 0; // В случае ошибки возвращаем 0
            }
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
    
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool>? _canExecute;
        
        public event EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }
        
        public RelayCommand(Action<object> execute, Func<object, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter!);
        }
        
        public void Execute(object? parameter)
        {
            _execute(parameter!);
        }
    }
} 