using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Security.Principal;

namespace SystemOptimizer.Services
{
    public class WindowsToolsService
    {
        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        private async Task<bool> RunCmdAsAdminAsync(string command)
        {
            if (!IsAdministrator())
            {
                System.Windows.MessageBox.Show("Для выполнения этой операции требуются права администратора.", 
                               "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    Verb = "runas"
                };
                
                var process = Process.Start(psi);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    return process.ExitCode == 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка выполнения команды: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        public async Task<bool> ToggleWindowsDefenderAsync(bool enable)
        {
            string command;
            
            if (enable)
            {
                command = "powershell -Command \"Set-MpPreference -DisableRealtimeMonitoring $false; " +
                          "New-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows Defender' -Name DisableAntiSpyware -Value 0 -PropertyType DWORD -Force\"";
            }
            else
            {
                command = "powershell -Command \"Set-MpPreference -DisableRealtimeMonitoring $true; " +
                          "New-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows Defender' -Name DisableAntiSpyware -Value 1 -PropertyType DWORD -Force\"";
            }
            
            return await RunCmdAsAdminAsync(command);
        }
        
        public async Task<bool> ExcludeFolderFromDefenderAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                System.Windows.MessageBox.Show("Указан неверный путь к папке.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            string command = $"powershell -Command \"Add-MpPreference -ExclusionPath '{folderPath}'\"";
            return await RunCmdAsAdminAsync(command);
        }
        
        public async Task<bool> ToggleSmartScreenAsync(bool enable)
        {
            string value = enable ? "1" : "0";
            string command = $"reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v EnableSmartScreen /t REG_DWORD /d {value} /f";
            return await RunCmdAsAdminAsync(command);
        }
        
        public async Task<bool> ToggleWindowsUpdatesAsync(bool enable, int updateMode = 0)
        {
            string command;
            
            switch (updateMode)
            {
                case 0: // Проверять, но не загружать
                    command = "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 2 /f";
                    break;
                case 1: // Проверять и уведомлять
                    command = "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 3 /f";
                    break;
                case 2: // Полностью отключить
                    command = enable 
                        ? "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 0 /f"
                        : "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 1 /f";
                    break;
                default:
                    command = "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 4 /f";
                    break;
            }
            
            return await RunCmdAsAdminAsync(command);
        }
        
        public async Task<bool> ToggleOfficeTelemetryAsync(bool enable)
        {
            string value = enable ? "1" : "0";
            
            // Office 2016 и новее
            string command = $"reg add \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Office\\Common\\ClientTelemetry\" /v SendTelemetry /t REG_DWORD /d {value} /f && " +
                           $"reg add \"HKCU\\SOFTWARE\\Microsoft\\Office\\16.0\\Common\" /v sendcustomerdata /t REG_DWORD /d {value} /f && " +
                           $"reg add \"HKCU\\SOFTWARE\\Microsoft\\Office\\16.0\\Common\\General\" /v shownfirstrunoptin /t REG_DWORD /d {value} /f && " +
                           $"reg add \"HKCU\\SOFTWARE\\Microsoft\\Office\\16.0\\Common\" /v qmenable /t REG_DWORD /d {value} /f";
            
            return await RunCmdAsAdminAsync(command);
        }
        
        public async Task<bool> OptimizeWindowsServicesAsync()
        {
            // Список служб для отключения
            string servicesToDisable = "DiagTrack TrkWks dmwappushservice RetailDemo WerSvc wisvc WSearch XblAuthManager XblGameSave XboxNetApiSvc";
            string command = "powershell -Command \"";
            
            foreach (string service in servicesToDisable.Split(' '))
            {
                command += $"Set-Service -Name {service} -StartupType Disabled; Stop-Service -Force -Name {service}; ";
            }
            
            command += "\"";
            
            return await RunCmdAsAdminAsync(command);
        }
        
        public bool GetWindowsDefenderStatus()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("DisableAntiSpyware");
                        if (value != null)
                        {
                            return Convert.ToInt32(value) == 0;
                        }
                    }
                }
                
                // По умолчанию включен
                return true;
            }
            catch
            {
                // При ошибке считаем, что включен
                return true;
            }
        }
        
        public bool GetSmartScreenStatus()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("EnableSmartScreen");
                        if (value != null)
                        {
                            return Convert.ToInt32(value) == 1;
                        }
                    }
                }
                
                // По умолчанию включен
                return true;
            }
            catch
            {
                // При ошибке считаем, что включен
                return true;
            }
        }
        
        public bool GetWindowsUpdateStatus()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("NoAutoUpdate");
                        if (value != null)
                        {
                            return Convert.ToInt32(value) == 0;
                        }
                    }
                }
                
                // По умолчанию включен
                return true;
            }
            catch
            {
                // При ошибке считаем, что включен
                return true;
            }
        }
        
        public bool GetOfficeTelemetryStatus()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Office\Common\ClientTelemetry"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("SendTelemetry");
                        if (value != null)
                        {
                            return Convert.ToInt32(value) == 1;
                        }
                    }
                }
                
                // По умолчанию включен
                return true;
            }
            catch
            {
                // При ошибке считаем, что включен
                return true;
            }
        }
    }
} 