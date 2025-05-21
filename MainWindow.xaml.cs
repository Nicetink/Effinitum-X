using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SystemOptimizer.Services;
using System.Windows.Threading;
using SystemOptimizer.Models;
using ModernWpf;
using Microsoft.Win32;
using ModernWpf.Controls;
using System.Diagnostics;

namespace SystemOptimizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SystemService _systemService;
        private ObservableCollection<KeyValuePair<string, string>> _systemInfoItems;
        private ObservableCollection<KeyValuePair<string, string>> _diskInfoItems;
        private ObservableCollection<FolderSizeInfo> _folderSizeItems;
        private ObservableCollection<ProcessViewModel> _processItems;
        private ObservableCollection<StartupItem> _startupItems;
        private Settings _settings;
        private UpdateManager _updateManager;

        public MainWindow()
        {
            try
            {
                // Initialize logger and record start of work
                Logger.LogInfo("Starting main window initialization");
                
            InitializeComponent();

                // Initialize services
            _systemService = new SystemService();
            _settings = Settings.Load();
                _updateManager = new UpdateManager();
                _updateManager.UpdateCheckCompleted += UpdateManager_UpdateCheckCompleted;
            
                // Initialize data collections
            _systemInfoItems = new ObservableCollection<KeyValuePair<string, string>>();
            _diskInfoItems = new ObservableCollection<KeyValuePair<string, string>>();
            _folderSizeItems = new ObservableCollection<FolderSizeInfo>();
            _processItems = new ObservableCollection<ProcessViewModel>();
            _startupItems = new ObservableCollection<StartupItem>();
            
                // Инициализируем мониторинг производительности
                InitializePerformanceMonitoring();
                
                // Привязка коллекций к элементам интерфейса
                if (lvSystemInfo != null) lvSystemInfo.ItemsSource = _systemInfoItems;
                if (lvDiskInfo != null) lvDiskInfo.ItemsSource = _diskInfoItems;
                if (lvFolderSize != null) lvFolderSize.ItemsSource = _folderSizeItems;
                if (dgProcesses != null) dgProcesses.ItemsSource = _processItems;
                if (lvStartupItems != null) lvStartupItems.ItemsSource = _startupItems;
                
                // Инициализация списка дисков
            LoadDrives();
            
                // Настройка обработки фильтрации процессов
                if (tbProcessFilter != null) tbProcessFilter.TextChanged += TbProcessFilter_TextChanged;
            
                // Применение темы из настроек
            ApplyThemeSettings();
            
                // Настройка отображения уровня очистки
                if (sliderCleanupLevel != null) sliderCleanupLevel.ValueChanged += SliderCleanupLevel_ValueChanged;
            UpdateCleanupLevelText();
            
                // Загрузка других настроек
                if (cbRunAtStartup != null) cbRunAtStartup.IsChecked = _settings.RunAtStartup;
                if (cbMinimizeToTray != null) cbMinimizeToTray.IsChecked = _settings.MinimizeToTray;
                if (cbEnableNotifications != null) cbEnableNotifications.IsChecked = _settings.EnableNotifications;
                if (cbCheckUpdatesAtStartup != null) cbCheckUpdatesAtStartup.IsChecked = _settings.CheckUpdatesAtStartup;
                if (sliderCleanupLevel != null) sliderCleanupLevel.Value = _settings.CleanupLevel;
                
                // Показ начальной страницы
            HideAllPages();
                if (welcomePage != null) welcomePage.Visibility = Visibility.Visible;
                
                // Регистрируем обработчики событий для мониторинга
                this.Loaded += MainWindow_Loaded;
                this.Activated += MainWindow_Activated;
                this.ContentRendered += MainWindow_ContentRendered;
                
                Logger.LogInfo("Main window initialization completed");
            }
            catch (Exception ex)
            {
                try
                {
                    Logger.LogError("Critical error during main window initialization", ex);
                }
                catch
                {
                    // If even the logger doesn't work, write to file
                    File.AppendAllText("critical_error.log", $"{DateTime.Now}: ERROR during initialization: {ex.Message}\n{ex.StackTrace}\n");
                }
                
                MessageBox.Show($"Critical error initializing main window:\n{ex.Message}\n\nCheck the log file for more information.", 
                               "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use Logger instead of direct file writing
                Logger.LogInfo("Main window Loaded event");
                
                // Final check for window visibility
                this.Visibility = Visibility.Visible;
                this.Activate();
                this.Focus();
                
                // Check for updates if enabled in settings
                if (_settings.CheckUpdatesAtStartup)
                {
                    CheckForUpdates();
                }
                
                Logger.LogInfo($"Window status - Visibility: {this.Visibility}, IsActive: {this.IsActive}");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in MainWindow_Loaded handler", ex);
            }
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            File.AppendAllText("app_log.txt", $"{DateTime.Now}: Событие Activated главного окна\n");
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            File.AppendAllText("app_log.txt", $"{DateTime.Now}: Событие ContentRendered главного окна\n");
            
            // Force UI update to ensure window is rendered correctly
            this.UpdateLayout();
        }
        
        private void LoadDrives()
        {
            try
            {
                if (cbDrives == null) return;
                
            cbDrives.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    string label = string.IsNullOrEmpty(drive.VolumeLabel) 
                        ? $"{drive.Name} ({drive.DriveType})" 
                        : $"{drive.Name} ({drive.VolumeLabel})";
                    cbDrives.Items.Add(label);
                }
            }
            
            if (cbDrives.Items.Count > 0)
            {
                cbDrives.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при загрузке дисков: {ex.Message}");
            }
        }

        #region Application Updates
        
        private async void CheckForUpdates()
        {
            try
            {
                Logger.LogInfo("Starting update check");
                var updateInfo = await _updateManager.CheckForUpdates();
                
                // Processing of check results happens in the UpdateCheckCompleted event handler
                Logger.LogInfo($"Update check completed. Current version: {updateInfo.CurrentVersion}, Latest version: {updateInfo.LatestVersion}");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error checking for updates", ex);
            }
        }
        
        private void UpdateManager_UpdateCheckCompleted(object sender, UpdateEventArgs e)
        {
            try
            {
                if (e.UpdateInfo.IsUpdateAvailable)
                {
                    Logger.LogInfo($"Обнаружено обновление: {e.UpdateInfo.LatestVersion}");
                    
                    var result = MessageBox.Show(
                        $"A new version of the application is available: {e.UpdateInfo.LatestVersion}\n" +
                        $"Your current version: {e.UpdateInfo.CurrentVersion}\n\n" +
                        $"Release notes:\n{e.UpdateInfo.ReleaseNotes}\n\n" +
                        "Do you want to update the application now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        InstallUpdate(e.UpdateInfo);
                    }
                    else
                    {
                        Logger.LogInfo("Пользователь отказался от установки обновления");
                    }
                }
                else if (!string.IsNullOrEmpty(e.UpdateInfo.ErrorMessage))
                {
                    Logger.LogWarning($"Ошибка при проверке обновлений: {e.UpdateInfo.ErrorMessage}");
                }
                else
                {
                    Logger.LogInfo("Обновлений не найдено, используется актуальная версия");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при обработке события обновления", ex);
            }
        }
        
        private async void InstallUpdate(UpdateInfo updateInfo)
        {
            try
            {
                Logger.LogInfo($"Начало установки обновления {updateInfo.LatestVersion}");
                
                // Create a simple update progress dialog
                ModernWpf.Controls.ContentDialog dialog = new ModernWpf.Controls.ContentDialog()
                {
                    Title = "Installing Update",
                    Content = "Downloading and installing update...\nPlease do not close the application.",
                    CloseButtonText = null,
                    IsPrimaryButtonEnabled = false,
                    IsSecondaryButtonEnabled = false
                };
                
                // Запускаем диалог и скачивание обновления параллельно
                var dialogTask = dialog.ShowAsync();
                
                bool success = await _updateManager.DownloadAndInstallUpdate(updateInfo);
                
                // Закрываем диалог
                dialog.Hide();
                
                if (success)
                {
                    Logger.LogInfo("Обновление успешно загружено и подготовлено к установке");
                    
                    MessageBox.Show(
                        "The update was successfully downloaded and will be installed the next time you start the application.",
                        "Update Ready",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    Logger.LogWarning("Не удалось загрузить или установить обновление");
                    
                    MessageBox.Show(
                        "Failed to download or install the update. Please try again later.",
                        "Update Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Произошла ошибка при установке обновления", ex);
                
                MessageBox.Show(
                    $"Произошла ошибка при установке обновления: {ex.Message}",
                    "Ошибка обновления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        #endregion

        #region Обработчики событий меню
        
        private void btnSystemInfo_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            if (systemInfoPage != null) systemInfoPage.Visibility = Visibility.Visible;
            LoadSystemInfo();
        }

        private void btnCleaner_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            if (cleanerPage != null) cleanerPage.Visibility = Visibility.Visible;
        }

        private void btnDiskAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            if (diskAnalyzerPage != null) diskAnalyzerPage.Visibility = Visibility.Visible;
        }

        private void btnProcesses_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            if (processesPage != null) processesPage.Visibility = Visibility.Visible;
            LoadProcesses();
        }

        private void btnStartup_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            if (startupPage != null) startupPage.Visibility = Visibility.Visible;
        }
        
        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            if (settingsPage != null) settingsPage.Visibility = Visibility.Visible;
        }
        
        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }
        
        private void MenuCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdatesManually();
        }
        
        private async void CheckForUpdatesManually()
        {
            try
            {
                ModernWpf.Controls.ContentDialog dialog = new ModernWpf.Controls.ContentDialog()
                {
                    Title = "Проверка обновлений",
                    Content = "Проверка наличия обновлений...",
                    CloseButtonText = "Отмена"
                };
                
                // Запускаем диалог и проверку обновлений параллельно
                var dialogTask = dialog.ShowAsync();
                var updateInfo = await _updateManager.CheckForUpdates();
                
                // Закрываем диалог
                dialog.Hide();
                
                if (updateInfo.IsUpdateAvailable)
                {
                    var result = MessageBox.Show(
                        $"Доступна новая версия приложения: {updateInfo.LatestVersion}\n" +
                        $"Ваша текущая версия: {updateInfo.CurrentVersion}\n\n" +
                        $"Примечания к выпуску:\n{updateInfo.ReleaseNotes}\n\n" +
                        "Хотите обновить приложение сейчас?",
                        "Доступно обновление",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        InstallUpdate(updateInfo);
                    }
                }
                else if (!string.IsNullOrEmpty(updateInfo.ErrorMessage))
                {
                    MessageBox.Show(
                        $"Не удалось проверить наличие обновлений: {updateInfo.ErrorMessage}",
                        "Ошибка проверки обновлений",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(
                        $"У вас установлена последняя версия приложения ({updateInfo.CurrentVersion}).",
                        "Обновления не найдены",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Произошла ошибка при проверке обновлений: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private void HideAllPages()
        {
            if (welcomePage != null) welcomePage.Visibility = Visibility.Collapsed;
            if (systemInfoPage != null) systemInfoPage.Visibility = Visibility.Collapsed;
            if (cleanerPage != null) cleanerPage.Visibility = Visibility.Collapsed;
            if (diskAnalyzerPage != null) diskAnalyzerPage.Visibility = Visibility.Collapsed;
            if (processesPage != null) processesPage.Visibility = Visibility.Collapsed;
            if (startupPage != null) startupPage.Visibility = Visibility.Collapsed;
            if (settingsPage != null) settingsPage.Visibility = Visibility.Collapsed;
            if (diskOptimizationPage != null) diskOptimizationPage.Visibility = Visibility.Collapsed;
            if (uwpAppsPage != null) uwpAppsPage.Visibility = Visibility.Collapsed;
            if (defenderPage != null) defenderPage.Visibility = Visibility.Collapsed;
            if (updatesPage != null) updatesPage.Visibility = Visibility.Collapsed;
            if (zapretPage != null) zapretPage.Visibility = Visibility.Collapsed;
            if (enhancedOptimizerPage != null) enhancedOptimizerPage.Visibility = Visibility.Collapsed;
        }
        
        #endregion
        
        #region Загрузка информации о системе
        
        private async void LoadSystemInfo()
        {
            try
            {
                // Clear collections
                _systemInfoItems.Clear();
                _diskInfoItems.Clear();
                
                // Get system info
                var systemInfo = await _systemService.GetSystemInfo();
                
                // Populate system info
                foreach (var item in systemInfo)
                {
                    if (!item.Key.StartsWith("Disk"))
                    {
                        _systemInfoItems.Add(item);
                    }
                }
                
                // Populate disk info
                foreach (var item in systemInfo)
                {
                    if (item.Key.StartsWith("Disk"))
                    {
                        _diskInfoItems.Add(item);
                    }
                }
                
                // Update memory progress bar
                if (systemInfo.TryGetValue("Total RAM", out string? totalRamStr) && 
                    systemInfo.TryGetValue("Used RAM", out string? usedRamStr) &&
                    totalRamStr != null && usedRamStr != null)
                {
                    double totalRam = double.Parse(totalRamStr.Replace(" GB", "").Trim());
                    double usedRam = double.Parse(usedRamStr.Replace(" GB", "").Trim());
                    
                    double percentage = (usedRam / totalRam) * 100;
                    pbMemory.Value = percentage;
                    
                    tbMemoryUsage.Text = $"Used: {usedRam:F2} GB of {totalRam:F2} GB ({percentage:F1}%)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading system information: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion
        
        #region Очистка системы
        
        private async void btnStartCleaning_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStartCleaning.IsEnabled = false;
                cleanupProgress.IsActive = true;
                cleanupProgress.Visibility = Visibility.Visible;
                tbCleaningResult.Text = "Cleaning in progress...";
                
                var result = await _systemService.CleanTemporaryFiles(
                    cbTempFiles.IsChecked ?? false,
                    cbRecycleBin.IsChecked ?? false,
                    cbBrowserCache.IsChecked ?? false,
                    cbWindowsLogs.IsChecked ?? false,
                    cbWindowsUpdate.IsChecked ?? false,
                    _settings.CleanupLevel);
                
                if (result.Success)
                {
                    tbCleaningResult.Text = $"Cleanup completed successfully. {result.FormattedBytesFreed} of disk space freed.";
                }
                else
                {
                    tbCleaningResult.Text = $"Error during cleanup: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                tbCleaningResult.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btnStartCleaning.IsEnabled = true;
                cleanupProgress.IsActive = false;
                cleanupProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        #endregion
        
        #region Анализ диска
        
        private async void btnAnalyzeDisk_Click(object sender, RoutedEventArgs e)
        {
            if (cbDrives.SelectedItem == null)
            {
                MessageBox.Show("Please select a drive to analyze", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                btnAnalyzeDisk.IsEnabled = false;
                diskAnalysisProgress.IsActive = true;
                diskAnalysisProgress.Visibility = Visibility.Visible;
                
                // Get selected drive
                string selectedDrive = cbDrives.SelectedItem.ToString()?.Split(' ')[0] ?? "";
                
                // Clear list
                _folderSizeItems.Clear();
                
                // Analyze disk
                var folderSizes = await _systemService.ScanDiskUsage(selectedDrive);
                
                // Sort results by size (largest to smallest)
                var sortedResults = folderSizes.OrderByDescending(x => x.Value);
                
                // Populate list
                foreach (var item in sortedResults)
                {
                    _folderSizeItems.Add(new FolderSizeInfo
                    {
                        Path = item.Key,
                        Size = FormatSize(item.Value),
                        SizeBytes = item.Value
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error analyzing disk: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnAnalyzeDisk.IsEnabled = true;
                diskAnalysisProgress.IsActive = false;
                diskAnalysisProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private string FormatSize(long size)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double dSize = size;
            int unitIndex = 0;
            
            while (dSize >= 1024 && unitIndex < units.Length - 1)
            {
                dSize /= 1024;
                unitIndex++;
            }
            
            return $"{dSize:F2} {units[unitIndex]}";
        }
        
        #endregion
        
        #region Управление процессами
        
        private async void LoadProcesses()
        {
            try
            {
                btnRefreshProcesses.IsEnabled = false;
                
                // Clear list
                _processItems.Clear();
                
                // Load processes
                var processes = await _systemService.GetRunningProcesses();
                
                // Populate list
                foreach (var process in processes)
                {
                    var vm = new ProcessViewModel
                    {
                        Id = process.Id,
                        Name = process.Name,
                        MemoryUsage = Math.Round(process.MemoryUsage, 1),
                        CpuUsage = Math.Round(process.CpuUsage, 1),
                        StartTime = process.StartTime
                    };
                    
                    vm.KillProcessCommand = new RelayCommand(param => KillProcess(vm.Id));
                    
                    _processItems.Add(vm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading processes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnRefreshProcesses.IsEnabled = true;
            }
        }
        
        private void btnRefreshProcesses_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }
        
        private void TbProcessFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = tbProcessFilter.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(filter))
            {
                dgProcesses.Items.Filter = null;
            }
            else
            {
                dgProcesses.Items.Filter = item =>
                {
                    if (item is ProcessViewModel process)
                    {
                        return process.Name.ToLower().Contains(filter);
                    }
                    return false;
                };
            }
        }
        
        private void KillProcess(int processId)
        {
            try
            {
                if (MessageBox.Show($"Are you sure you want to terminate the process with ID {processId}?", 
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    bool result = _systemService.KillProcess(processId);
                    
                    if (result)
                    {
                        // Remove process from list
                        var process = _processItems.FirstOrDefault(p => p.Id == processId);
                        if (process != null)
                        {
                            _processItems.Remove(process);
                        }
                        
                        MessageBox.Show("Process terminated successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to terminate the process", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error terminating process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion
        
        #region Оптимизация автозагрузки
        
        private async void btnOptimizeStartup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnOptimizeStartup.IsEnabled = false;
                startupProgress.IsActive = true;
                startupProgress.Visibility = Visibility.Visible;
                tbStartupResult.Text = "Optimizing startup items...";
                
                bool result = await _systemService.OptimizeStartup();
                
                if (result)
                {
                    tbStartupResult.Text = "Startup items have been optimized successfully. Unnecessary programs have been disabled.";
                    
                    // Refresh the list
                    await Task.Delay(1000); // Short delay to ensure registry changes have been applied
                    await _systemService.GetStartupItems();
                }
                else
                {
                    tbStartupResult.Text = "Error occurred during startup optimization.";
                }
            }
            catch (Exception ex)
            {
                tbStartupResult.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btnOptimizeStartup.IsEnabled = true;
                startupProgress.IsActive = false;
                startupProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        #endregion

        #region Startup Management
        
        private async void btnLoadStartupItems_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnLoadStartupItems.IsEnabled = false;
                startupProgress.IsActive = true;
                startupProgress.Visibility = Visibility.Visible;
                
                // Clear list
                _startupItems.Clear();
                
                // Get startup items
                var items = await _systemService.GetStartupItems();
                
                // Sort by name
                var sortedItems = items.OrderBy(i => i.Name);
                
                // Populate list
                foreach (var item in sortedItems)
                {
                    _startupItems.Add(item);
                }
                
                tbStartupResult.Text = $"Loaded {_startupItems.Count} startup items.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading startup items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                tbStartupResult.Text = "Error loading startup items.";
            }
            finally
            {
                btnLoadStartupItems.IsEnabled = true;
                startupProgress.IsActive = false;
                startupProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private void StartupItem_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ModernWpf.Controls.ToggleSwitch toggleSwitch && toggleSwitch.DataContext is StartupItem item)
            {
                try
                {
                    bool result = _systemService.ToggleStartupItem(item, toggleSwitch.IsOn);
                    
                    if (!result)
                    {
                        // If failed, revert the toggle
                        toggleSwitch.IsOn = !toggleSwitch.IsOn;
                        
                        MessageBox.Show($"Failed to {(toggleSwitch.IsOn ? "enable" : "disable")} the startup item.", 
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    // If an exception occurs, revert the toggle
                    toggleSwitch.IsOn = !toggleSwitch.IsOn;
                    
                    MessageBox.Show($"Error toggling startup item: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        #endregion

        #region Settings
        
        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            
            if (sender == rbLightTheme && rbLightTheme.IsChecked == true)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                _settings.AppTheme = Models.ThemeMode.Light;
            }
            else if (sender == rbDarkTheme && rbDarkTheme.IsChecked == true)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                _settings.AppTheme = Models.ThemeMode.Dark;
            }
            else if (sender == rbSystemTheme && rbSystemTheme.IsChecked == true)
            {
                ThemeManager.Current.ApplicationTheme = null; // Use system setting
                _settings.AppTheme = Models.ThemeMode.System;
            }
        }
        
        private void SliderCleanupLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            
            _settings.CleanupLevel = (int)sliderCleanupLevel.Value;
            UpdateCleanupLevelText();
        }
        
        private void UpdateCleanupLevelText()
        {
            if (sliderCleanupLevel == null || tbCleanupLevel == null) return;
            
            switch ((int)sliderCleanupLevel.Value)
            {
                case 1:
                    tbCleanupLevel.Text = "Basic";
                    break;
                case 2:
                    tbCleanupLevel.Text = "Normal";
                    break;
                case 3:
                    tbCleanupLevel.Text = "Aggressive";
                    break;
            }
        }
        
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
        {
            _settings.RunAtStartup = cbRunAtStartup.IsChecked ?? false;
            _settings.MinimizeToTray = cbMinimizeToTray.IsChecked ?? false;
            _settings.EnableNotifications = cbEnableNotifications.IsChecked ?? false;
                _settings.CheckUpdatesAtStartup = cbCheckUpdatesAtStartup.IsChecked ?? true;
            
            // Apply run at startup setting
            ApplyRunAtStartup(_settings.RunAtStartup);
            
            // Save settings
            _settings.Save();
            
                Logger.LogInfo("Settings successfully saved");
                MessageBox.Show("Settings successfully saved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error saving settings", ex);
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ApplyRunAtStartup(bool runAtStartup)
        {
            try
            {
                // Set/Remove from registry
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (runAtStartup)
                        {
                            // Add to startup
                            string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            key.SetValue("SystemOptimizer", executablePath);
                        }
                        else
                        {
                            // Remove from startup
                            key.DeleteValue("SystemOptimizer", false);
                        }
                    }
                }
            }
            catch
            {
                // Silent error - we'll try again next time
            }
        }
        
        #endregion

        private void ApplyThemeSettings()
        {
            switch (_settings.AppTheme)
            {
                case Models.ThemeMode.Light:
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                    rbLightTheme.IsChecked = true;
                    break;
                case Models.ThemeMode.Dark:
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    rbDarkTheme.IsChecked = true;
                    break;
                case Models.ThemeMode.System:
                    ThemeManager.Current.ApplicationTheme = null; // Use system setting
                    rbSystemTheme.IsChecked = true;
                    break;
            }
        }

        private void RefreshSystemInfo_Click(object sender, RoutedEventArgs e)
        {
            LoadSystemInfo();
        }

        private void btnDiskOptimization_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            diskOptimizationPage.Visibility = Visibility.Visible;
            LoadDrivesForOptimization();
        }
        
        private void LoadDrivesForOptimization()
        {
            cbOptimizeDrives.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    string label = string.IsNullOrEmpty(drive.VolumeLabel) 
                        ? $"{drive.Name} ({drive.DriveType})" 
                        : $"{drive.Name} ({drive.VolumeLabel})";
                    cbOptimizeDrives.Items.Add(label);
                }
            }
            
            if (cbOptimizeDrives.Items.Count > 0)
            {
                cbOptimizeDrives.SelectedIndex = 0;
            }
        }
        
        private async void btnStartOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (cbOptimizeDrives.SelectedItem == null)
            {
                MessageBox.Show("Please select a disk for optimization", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                btnStartOptimization.IsEnabled = false;
                diskOptimizationProgress.IsActive = true;
                diskOptimizationProgress.Visibility = Visibility.Visible;
                tbOptimizationResult.Text = "Оптимизация диска...";
                
                // Получаем выбранный диск
                string selectedDrive = cbOptimizeDrives.SelectedItem.ToString()?.Split(' ')[0] ?? "";
                
                // Используем улучшенный метод оптимизации
                var result = await _systemService.AdvancedDiskOptimization(
                    selectedDrive[0].ToString(),
                    cbCheckDisk.IsChecked ?? false,
                    cbDefragment.IsChecked ?? false,
                    cbCleanupDisk.IsChecked ?? false,
                    cbTrimSSD.IsChecked ?? false);
                
                if (result.Success)
                {
                    string message = "Оптимизация завершена успешно.";
                    
                    if (result.CleanupSpaceFreed > 0)
                    {
                        message += $" Освобождено {result.FormattedSpaceFreed} дискового пространства.";
                    }
                    
                    if (result.DefragmentSuccess)
                    {
                        message += " Дефрагментация завершена успешно.";
                    }
                    
                    tbOptimizationResult.Text = message;
                }
                else
                {
                    tbOptimizationResult.Text = "Во время оптимизации произошла ошибка. Убедитесь, что у вас есть права администратора.";
                }
            }
            catch (Exception ex)
            {
                tbOptimizationResult.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                btnStartOptimization.IsEnabled = true;
                diskOptimizationProgress.IsActive = false;
                diskOptimizationProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (e.Uri != null)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = e.Uri.AbsoluteUri,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open the link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            
            e.Handled = true;
        }
        
        private void btnUwpApps_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            uwpAppsPage.Visibility = Visibility.Visible;
            LoadUwpApps();
        }
        
        private void btnLoadUwpApps_Click(object sender, RoutedEventArgs e)
        {
            LoadUwpApps();
        }
        
        private void btnDefender_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            defenderPage.Visibility = Visibility.Visible;
            CheckDefenderStatus();
        }
        
        private void btnUpdates_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            updatesPage.Visibility = Visibility.Visible;
            CheckUpdatesStatus();
        }
        
        private void btnZapret_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            zapretPage.Visibility = Visibility.Visible;
            CheckZapretStatus();
        }
        
        private void NavigationView_SelectionChanged(object sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs e)
        {
            var selectedItem = e.SelectedItem as ModernWpf.Controls.NavigationViewItem;
            if (selectedItem != null)
            {
                string tag = selectedItem.Tag?.ToString();
                
                // Останавливаем мониторинг производительности, если он был запущен
                StopPerformanceMonitoring();
                
                switch (tag)
                {
                    case "welcome":
                        HideAllPages();
                        welcomePage.Visibility = Visibility.Visible;
                        break;
                    case "systemInfo":
                        HideAllPages();
                        systemInfoPage.Visibility = Visibility.Visible;
                        LoadSystemInfo();
                        break;
                    case "cleaner":
                        HideAllPages();
                        cleanerPage.Visibility = Visibility.Visible;
                        break;
                    case "diskAnalyzer":
                        HideAllPages();
                        diskAnalyzerPage.Visibility = Visibility.Visible;
                        break;
                    case "diskOptimization":
                        HideAllPages();
                        diskOptimizationPage.Visibility = Visibility.Visible;
                        LoadDrivesForOptimization();
                        break;
                    case "enhancedOptimizer":
                        HideAllPages();
                        enhancedOptimizerPage.Visibility = Visibility.Visible;
                        StartPerformanceMonitoring();
                        break;
                    case "processes":
                        HideAllPages();
                        processesPage.Visibility = Visibility.Visible;
                        LoadProcesses();
                        break;
                    case "startup":
                        HideAllPages();
                        startupPage.Visibility = Visibility.Visible;
                        break;
                    case "settings":
                        HideAllPages();
                        settingsPage.Visibility = Visibility.Visible;
                        break;
                    case "uwpApps":
                        HideAllPages();
                        uwpAppsPage.Visibility = Visibility.Visible;
                        LoadUwpApps();
                        break;
                    case "defender":
                        HideAllPages();
                        defenderPage.Visibility = Visibility.Visible;
                        CheckDefenderStatus();
                        break;
                    case "updates":
                        HideAllPages();
                        updatesPage.Visibility = Visibility.Visible;
                        CheckUpdatesStatus();
                        break;
                    case "zapret":
                        HideAllPages();
                        zapretPage.Visibility = Visibility.Visible;
                        CheckZapretStatus();
                        break;
                }
            }
        }
        
        #region UWP Apps
        
        private ObservableCollection<UwpApp> _uwpApps = new ObservableCollection<UwpApp>();
        private ObservableCollection<UwpApp> _filteredUwpApps = new ObservableCollection<UwpApp>();
        
        private async void LoadUwpApps()
        {
            try
            {
                btnLoadUwpApps.IsEnabled = false;
                tbUwpFilter.IsEnabled = false;
                lvUwpApps.ItemsSource = null;
                
                _uwpApps.Clear();
                _filteredUwpApps.Clear();
                
                var apps = await _systemService.GetUwpApps();
                
                foreach (var app in apps.OrderBy(a => a.Name))
                {
                    _uwpApps.Add(app);
                    _filteredUwpApps.Add(app);
                }
                
                lvUwpApps.ItemsSource = _filteredUwpApps;
                
                btnLoadUwpApps.IsEnabled = true;
                tbUwpFilter.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading UWP apps: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnLoadUwpApps.IsEnabled = true;
                tbUwpFilter.IsEnabled = true;
            }
        }
        
        private void TbUwpFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterUwpApps();
        }
        
        private void FilterUwpApps()
        {
            string filter = tbUwpFilter.Text.ToLower().Trim();
            
            _filteredUwpApps.Clear();
            
            foreach (var app in _uwpApps)
            {
                if (string.IsNullOrEmpty(filter) || 
                    app.Name.ToLower().Contains(filter) || 
                    app.Publisher.ToLower().Contains(filter))
                {
                    _filteredUwpApps.Add(app);
                }
            }
        }
        
        private async void BtnUninstallUwp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string packageName)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить это приложение?\n\n{packageName}",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        bool success = await _systemService.UninstallUwpApp(packageName);
                        
                        if (success)
                        {
                            MessageBox.Show("Application successfully uninstalled.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadUwpApps(); // Обновляем список
                        }
                        else
                        {
                            MessageBox.Show("Failed to uninstall the application. Make sure you have the necessary permissions.", 
                                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion
        
        #region Windows Defender
        
        private async void CheckDefenderStatus()
        {
            tbDefenderStatus.Text = "Проверка статуса Windows Defender...";
            defenderProgress.IsActive = true;
            defenderProgress.Visibility = Visibility.Visible;
            
            try
            {
                // Проверяем текущий статус через реестр
                bool isEnabled = true;
                
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("DisableAntiSpyware");
                        if (value != null && (int)value == 1)
                        {
                            isEnabled = false;
                        }
                    }
                }
                
                tbDefenderStatus.Text = isEnabled ? 
                    "Windows Defender активен и защищает ваш компьютер." : 
                    "Windows Defender отключен.";
                    
                btnDisableDefender.IsEnabled = isEnabled;
                btnEnableDefender.IsEnabled = !isEnabled;
            }
            catch (Exception ex)
            {
                tbDefenderStatus.Text = $"Не удалось определить статус: {ex.Message}";
            }
            finally
            {
                defenderProgress.IsActive = false;
                defenderProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void btnDisableDefender_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Отключение Windows Defender может сделать ваш компьютер уязвимым для вирусов и вредоносных программ. Продолжить?",
                "Предупреждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.No)
                return;
                
            try
            {
                btnDisableDefender.IsEnabled = false;
                btnEnableDefender.IsEnabled = false;
                defenderProgress.IsActive = true;
                defenderProgress.Visibility = Visibility.Visible;
                tbDefenderResult.Text = "Отключение Windows Defender...";
                
                bool success = await _systemService.DisableWindowsDefender();
                
                if (success)
                {
                    tbDefenderResult.Text = "Windows Defender успешно отключен.";
                    tbDefenderStatus.Text = "Windows Defender отключен.";
                    btnDisableDefender.IsEnabled = false;
                    btnEnableDefender.IsEnabled = true;
                }
                else
                {
                    tbDefenderResult.Text = "Не удалось отключить Windows Defender. Убедитесь, что у вас есть права администратора.";
                    CheckDefenderStatus();
                }
            }
            catch (Exception ex)
            {
                tbDefenderResult.Text = $"Ошибка: {ex.Message}";
                CheckDefenderStatus();
            }
            finally
            {
                defenderProgress.IsActive = false;
                defenderProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void btnEnableDefender_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnDisableDefender.IsEnabled = false;
                btnEnableDefender.IsEnabled = false;
                defenderProgress.IsActive = true;
                defenderProgress.Visibility = Visibility.Visible;
                tbDefenderResult.Text = "Включение Windows Defender...";
                
                bool success = await _systemService.EnableWindowsDefender();
                
                if (success)
                {
                    tbDefenderResult.Text = "Windows Defender успешно включен.";
                    tbDefenderStatus.Text = "Windows Defender активен и защищает ваш компьютер.";
                    btnDisableDefender.IsEnabled = true;
                    btnEnableDefender.IsEnabled = false;
                }
                else
                {
                    tbDefenderResult.Text = "Не удалось включить Windows Defender. Убедитесь, что у вас есть права администратора.";
                    CheckDefenderStatus();
                }
            }
            catch (Exception ex)
            {
                tbDefenderResult.Text = $"Ошибка: {ex.Message}";
                CheckDefenderStatus();
            }
            finally
            {
                defenderProgress.IsActive = false;
                defenderProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        #endregion
        
        #region Windows Updates
        
        private async void CheckUpdatesStatus()
        {
            tbUpdatesStatus.Text = "Проверка статуса обновлений Windows...";
            updatesProgress.IsActive = true;
            updatesProgress.Visibility = Visibility.Visible;
            
            try
            {
                // Проверяем текущий статус через реестр и службу
                bool isEnabled = true;
                
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("NoAutoUpdate");
                        if (value != null && (int)value == 1)
                        {
                            isEnabled = false;
                        }
                    }
                }
                
                // Проверяем статус службы Windows Update
                using (var sc = new System.ServiceProcess.ServiceController("wuauserv"))
                {
                    if (sc.Status == System.ServiceProcess.ServiceControllerStatus.Stopped)
                    {
                        isEnabled = false;
                    }
                }
                
                tbUpdatesStatus.Text = isEnabled ? 
                    "Обновления Windows включены." : 
                    "Обновления Windows отключены.";
                    
                btnDisableUpdates.IsEnabled = isEnabled;
                btnEnableUpdates.IsEnabled = !isEnabled;
            }
            catch (Exception ex)
            {
                tbUpdatesStatus.Text = $"Не удалось определить статус: {ex.Message}";
            }
            finally
            {
                updatesProgress.IsActive = false;
                updatesProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void btnDisableUpdates_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Отключение обновлений Windows может сделать ваш компьютер уязвимым для атак. Продолжить?",
                "Предупреждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.No)
                return;
                
            try
            {
                btnDisableUpdates.IsEnabled = false;
                btnEnableUpdates.IsEnabled = false;
                updatesProgress.IsActive = true;
                updatesProgress.Visibility = Visibility.Visible;
                tbUpdatesResult.Text = "Отключение обновлений Windows...";
                
                bool success = await _systemService.DisableWindowsUpdates();
                
                if (success)
                {
                    tbUpdatesResult.Text = "Обновления Windows успешно отключены.";
                    tbUpdatesStatus.Text = "Обновления Windows отключены.";
                    btnDisableUpdates.IsEnabled = false;
                    btnEnableUpdates.IsEnabled = true;
                }
                else
                {
                    tbUpdatesResult.Text = "Не удалось отключить обновления Windows. Убедитесь, что у вас есть права администратора.";
                    CheckUpdatesStatus();
                }
            }
            catch (Exception ex)
            {
                tbUpdatesResult.Text = $"Ошибка: {ex.Message}";
                CheckUpdatesStatus();
            }
            finally
            {
                updatesProgress.IsActive = false;
                updatesProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void btnEnableUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnDisableUpdates.IsEnabled = false;
                btnEnableUpdates.IsEnabled = false;
                updatesProgress.IsActive = true;
                updatesProgress.Visibility = Visibility.Visible;
                tbUpdatesResult.Text = "Включение обновлений Windows...";
                
                bool success = await _systemService.EnableWindowsUpdates();
                
                if (success)
                {
                    tbUpdatesResult.Text = "Обновления Windows успешно включены.";
                    tbUpdatesStatus.Text = "Обновления Windows включены.";
                    btnDisableUpdates.IsEnabled = true;
                    btnEnableUpdates.IsEnabled = false;
                }
                else
                {
                    tbUpdatesResult.Text = "Не удалось включить обновления Windows. Убедитесь, что у вас есть права администратора.";
                    CheckUpdatesStatus();
                }
            }
            catch (Exception ex)
            {
                tbUpdatesResult.Text = $"Ошибка: {ex.Message}";
                CheckUpdatesStatus();
            }
            finally
            {
                updatesProgress.IsActive = false;
                updatesProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        #endregion
        
        #region Zapret-Discord-YouTube
        
        private async void CheckZapretStatus()
        {
            tbZapretStatus.Text = "Проверка статуса Zapret-Discord-YouTube...";
            zapretProgress.IsActive = true;
            zapretProgress.Visibility = Visibility.Visible;
            
            try
            {
                // Используем новый подробный статус
                var status = await _systemService.CheckDetailedZapretStatus();
                
                tbZapretStatus.Text = status.IsInstalled ? "Установлен" : "Не установлен";
                tbZapretVersion.Text = status.Version;
                tbZapretRunning.Text = status.IsRunning ? "Да" : "Нет";
                
                tbDiscordStatus.Text = status.DiscordStatus ? "Включен" : "Отключен";
                tbYouTubeStatus.Text = status.YouTubeStatus ? "Включен" : "Отключен";
                
                toggleDiscord.IsEnabled = status.IsInstalled;
                toggleDiscord.IsChecked = status.DiscordStatus;
                
                toggleYouTube.IsEnabled = status.IsInstalled;
                toggleYouTube.IsChecked = status.YouTubeStatus;
                
                btnInstallZapret.IsEnabled = !status.IsInstalled;
                btnUninstallZapret.IsEnabled = status.IsInstalled;
            }
            catch (Exception ex)
            {
                tbZapretStatus.Text = $"Не удалось определить статус: {ex.Message}";
            }
            finally
            {
                zapretProgress.IsActive = false;
                zapretProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void btnRefreshZapretStatus_Click(object sender, RoutedEventArgs e)
        {
            CheckZapretStatus();
        }
        
        private async void toggleDiscord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                toggleDiscord.IsEnabled = false;
                zapretProgress.IsActive = true;
                zapretProgress.Visibility = Visibility.Visible;
                
                bool enable = toggleDiscord.IsChecked == true;
                tbZapretResult.Text = enable ? "Включение обхода блокировки Discord..." : "Отключение обхода блокировки Discord...";
                
                bool success = await _systemService.ToggleZapretDiscord(enable);
                
                if (success)
                {
                    tbZapretResult.Text = enable 
                        ? "Обход блокировки Discord успешно включен." 
                        : "Обход блокировки Discord успешно отключен.";
                    
                    tbDiscordStatus.Text = enable ? "Включен" : "Отключен";
                }
                else
                {
                    tbZapretResult.Text = "Не удалось изменить настройки Discord. Убедитесь, что у вас есть права администратора.";
                    toggleDiscord.IsChecked = !enable; // Возвращаем предыдущее состояние
                }
            }
            catch (Exception ex)
            {
                tbZapretResult.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                toggleDiscord.IsEnabled = true;
                zapretProgress.IsActive = false;
                zapretProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void toggleYouTube_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                toggleYouTube.IsEnabled = false;
                zapretProgress.IsActive = true;
                zapretProgress.Visibility = Visibility.Visible;
                
                bool enable = toggleYouTube.IsChecked == true;
                tbZapretResult.Text = enable ? "Включение обхода блокировки YouTube..." : "Отключение обхода блокировки YouTube...";
                
                bool success = await _systemService.ToggleZapretYouTube(enable);
                
                if (success)
                {
                    tbZapretResult.Text = enable 
                        ? "Обход блокировки YouTube успешно включен." 
                        : "Обход блокировки YouTube успешно отключен.";
                    
                    tbYouTubeStatus.Text = enable ? "Включен" : "Отключен";
                }
                else
                {
                    tbZapretResult.Text = "Не удалось изменить настройки YouTube. Убедитесь, что у вас есть права администратора.";
                    toggleYouTube.IsChecked = !enable; // Возвращаем предыдущее состояние
                }
            }
            catch (Exception ex)
            {
                tbZapretResult.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                toggleYouTube.IsEnabled = true;
                zapretProgress.IsActive = false;
                zapretProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void btnInstallZapret_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnInstallZapret.IsEnabled = false;
                btnUninstallZapret.IsEnabled = false;
                zapretProgress.IsActive = true;
                zapretProgress.Visibility = Visibility.Visible;
                tbZapretResult.Text = "Установка Zapret-Discord-YouTube...";
                
                bool success = await _systemService.InstallZapretDiscordYoutube();
                
                if (success)
                {
                    tbZapretResult.Text = "Zapret-Discord-YouTube успешно установлен.";
                    CheckZapretStatus();
                }
                else
                {
                    tbZapretResult.Text = "Не удалось установить Zapret-Discord-YouTube. Убедитесь, что у вас есть права администратора и подключение к Интернету.";
                    CheckZapretStatus();
                }
            }
            catch (Exception ex)
            {
                tbZapretResult.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                zapretProgress.IsActive = false;
                zapretProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void btnUninstallZapret_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnInstallZapret.IsEnabled = false;
                btnUninstallZapret.IsEnabled = false;
                zapretProgress.IsActive = true;
                zapretProgress.Visibility = Visibility.Visible;
                tbZapretResult.Text = "Удаление Zapret-Discord-YouTube...";
                
                bool success = await _systemService.UninstallZapretDiscordYoutube();
                
                if (success)
                {
                    tbZapretResult.Text = "Zapret-Discord-YouTube успешно удален.";
                    CheckZapretStatus();
                }
                else
                {
                    tbZapretResult.Text = "Не удалось удалить Zapret-Discord-YouTube. Убедитесь, что у вас есть права администратора.";
                    CheckZapretStatus();
                }
            }
            catch (Exception ex)
            {
                tbZapretResult.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                zapretProgress.IsActive = false;
                zapretProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        #endregion
        
        #region Enhanced System Optimizer
        
        private SystemPerformanceInfo _performanceInfo;
        private DispatcherTimer _performanceTimer;
        
        private void InitializePerformanceMonitoring()
        {
            _performanceInfo = new SystemPerformanceInfo();
            
            // Проверяем, что коллекции инициализированы правильно
            if (_performanceInfo.CpuHistory == null)
                _performanceInfo.CpuHistory = new ObservableCollection<PerformancePoint>();
            
            if (_performanceInfo.MemoryHistory == null)
                _performanceInfo.MemoryHistory = new ObservableCollection<PerformancePoint>();
            
            if (_performanceInfo.DiskHistory == null)
                _performanceInfo.DiskHistory = new ObservableCollection<PerformancePoint>();
            
            _performanceTimer = new DispatcherTimer();
            _performanceTimer.Interval = TimeSpan.FromSeconds(1);
            _performanceTimer.Tick += PerformanceTimer_Tick;
        }
        
        private async void PerformanceTimer_Tick(object sender, EventArgs e)
        {
            await RefreshPerformanceData();
        }
        
        private async Task RefreshPerformanceData()
        {
            try
            {
                var info = await _systemService.GetSystemPerformance();
                
                // Проверка на null для элементов интерфейса
                if (tbCpuUsage == null || tbMemoryUsagePercent == null || tbDiskUsage == null)
                    return;
                
                // Обновляем текстовые поля
                tbCpuUsage.Text = $"{info.CpuUsage:F1}%";
                tbMemoryUsagePercent.Text = $"{info.MemoryUsage:F1}%";
                tbDiskUsage.Text = $"{info.DiskUsage:F1}%";
                
                // Обновляем график
                DrawPerformanceGraph(info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обновлении данных производительности: {ex.Message}");
            }
        }
        
        private void DrawPerformanceGraph(SystemPerformanceInfo info)
        {
            if (info == null || performanceGraph == null) return;
            
            // Очищаем холст
            performanceGraph.Children.Clear();
            
            // Если коллекции не инициализированы или нет данных, выходим
            if (info.CpuHistory == null || info.MemoryHistory == null || 
                info.DiskHistory == null || info.CpuHistory.Count == 0) 
            {
                return;
            }
            
            // Получаем размеры холста
            double width = performanceGraph.ActualWidth;
            double height = performanceGraph.ActualHeight;
            
            // Рисуем сетку
            DrawGridLines(width, height);
            
            // Рисуем графики
            if (info.CpuHistory.Count > 0)
                DrawLine(info.CpuHistory, width, height, Colors.Red);
            
            if (info.MemoryHistory.Count > 0)
                DrawLine(info.MemoryHistory, width, height, Colors.Blue);
            
            if (info.DiskHistory.Count > 0)
                DrawLine(info.DiskHistory, width, height, Colors.Green);
            
            // Добавляем легенду
            DrawLegend(width, height);
        }
        
        private void DrawGridLines(double width, double height)
        {
            // Горизонтальные линии
            for (int i = 0; i <= 10; i++)
            {
                Line line = new Line();
                line.X1 = 0;
                line.X2 = width;
                line.Y1 = line.Y2 = i * (height / 10);
                line.Stroke = new SolidColorBrush(Colors.Gray);
                line.StrokeThickness = i % 5 == 0 ? 0.5 : 0.25;
                line.Opacity = 0.5;
                performanceGraph.Children.Add(line);
                
                // Добавляем метки для основных линий
                if (i % 5 == 0)
                {
                    TextBlock text = new TextBlock();
                    text.Text = $"{100 - i * 10}%";
                    text.FontSize = 10;
                    text.Foreground = new SolidColorBrush(Colors.Gray);
                    Canvas.SetLeft(text, 5);
                    Canvas.SetTop(text, i * (height / 10) - 10);
                    performanceGraph.Children.Add(text);
                }
            }
            
            // Вертикальные линии
            for (int i = 0; i <= 6; i++)
            {
                Line line = new Line();
                line.X1 = line.X2 = i * (width / 6);
                line.Y1 = 0;
                line.Y2 = height;
                line.Stroke = new SolidColorBrush(Colors.Gray);
                line.StrokeThickness = 0.25;
                line.Opacity = 0.5;
                performanceGraph.Children.Add(line);
            }
        }
        
        private void DrawLine(ObservableCollection<PerformancePoint> points, double width, double height, Color color)
        {
            if (points == null || points.Count < 2 || performanceGraph == null) return;
            
            Polyline polyline = new Polyline();
            polyline.Stroke = new SolidColorBrush(color);
            polyline.StrokeThickness = 2;
            
            int count = points.Count;
            double xStep = width / Math.Min(60, Math.Max(1, count - 1));
            
            for (int i = 0; i < count; i++)
            {
                if (points[i] == null) continue;
                
                double x = i * xStep;
                double y = height - (points[i].Value / 100 * height);
                polyline.Points.Add(new Point(x, y));
            }
            
            performanceGraph.Children.Add(polyline);
        }
        
        private void DrawLegend(double width, double height)
        {
            Canvas legend = new Canvas();
            Canvas.SetLeft(legend, width - 100);
            Canvas.SetTop(legend, 10);
            legend.Width = 90;
            legend.Height = 80;
            legend.Background = new SolidColorBrush(Color.FromArgb(128, 240, 240, 240));
            
            // CPU
            Ellipse cpuDot = new Ellipse();
            cpuDot.Width = cpuDot.Height = 8;
            cpuDot.Fill = new SolidColorBrush(Colors.Red);
            Canvas.SetLeft(cpuDot, 5);
            Canvas.SetTop(cpuDot, 10);
            legend.Children.Add(cpuDot);
            
            TextBlock cpuText = new TextBlock();
            cpuText.Text = "CPU";
            cpuText.Foreground = new SolidColorBrush(Colors.Black);
            Canvas.SetLeft(cpuText, 20);
            Canvas.SetTop(cpuText, 7);
            legend.Children.Add(cpuText);
            
            // Memory
            Ellipse memDot = new Ellipse();
            memDot.Width = memDot.Height = 8;
            memDot.Fill = new SolidColorBrush(Colors.Blue);
            Canvas.SetLeft(memDot, 5);
            Canvas.SetTop(memDot, 30);
            legend.Children.Add(memDot);
            
            TextBlock memText = new TextBlock();
            memText.Text = "Memory";
            memText.Foreground = new SolidColorBrush(Colors.Black);
            Canvas.SetLeft(memText, 20);
            Canvas.SetTop(memText, 27);
            legend.Children.Add(memText);
            
            // Disk
            Ellipse diskDot = new Ellipse();
            diskDot.Width = diskDot.Height = 8;
            diskDot.Fill = new SolidColorBrush(Colors.Green);
            Canvas.SetLeft(diskDot, 5);
            Canvas.SetTop(diskDot, 50);
            legend.Children.Add(diskDot);
            
            TextBlock diskText = new TextBlock();
            diskText.Text = "Disk";
            diskText.Foreground = new SolidColorBrush(Colors.Black);
            Canvas.SetLeft(diskText, 20);
            Canvas.SetTop(diskText, 47);
            legend.Children.Add(diskText);
            
            performanceGraph.Children.Add(legend);
        }
        
        private async void btnRefreshPerformance_Click(object sender, RoutedEventArgs e)
        {
            await RefreshPerformanceData();
        }
        
        private async void StartPerformanceMonitoring()
        {
            if (_performanceTimer == null)
            {
                InitializePerformanceMonitoring();
            }
            
            _performanceTimer.Start();
            await RefreshPerformanceData();
        }
        
        private void StopPerformanceMonitoring()
        {
            if (_performanceTimer != null)
            {
                _performanceTimer.Stop();
            }
        }
        
        private async void btnStartEnhancedOptimization_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStartEnhancedOptimization.IsEnabled = false;
                enhancedOptimizerProgress.IsActive = true;
                enhancedOptimizerProgress.Visibility = Visibility.Visible;
                tbEnhancedOptimizerResult.Text = "Выполняется оптимизация системы...";
                
                // Определяем уровень оптимизации
                int optimizationLevel = 1; // Light
                if (rbMediumOptimization.IsChecked == true) optimizationLevel = 2; // Medium
                if (rbAggresiveOptimization.IsChecked == true) optimizationLevel = 3; // Aggressive
                
                // Запускаем оптимизацию
                var result = await _systemService.OptimizeSystem(
                    cbOptimizePerformance.IsChecked ?? false,
                    cbOptimizeDisk.IsChecked ?? false,
                    cbOptimizeMemory.IsChecked ?? false,
                    cbOptimizeStartup.IsChecked ?? false,
                    cbOptimizeBrowser.IsChecked ?? false,
                    cbOptimizeNetwork.IsChecked ?? false,
                    optimizationLevel
                );
                
                if (result.Success)
                {
                    string message = "Оптимизация завершена успешно!";
                    
                    if (result.SpaceFreed > 0)
                    {
                        message += $" Освобождено {result.FormattedSpaceFreed} дискового пространства.";
                    }
                    
                    tbEnhancedOptimizerResult.Text = message;
                    
                    // Отображаем список примененных оптимизаций
                    if (result.OptimizationsApplied.Count > 0)
                    {
                        lvOptimizationActions.Items.Clear();
                        foreach (var optimization in result.OptimizationsApplied)
                        {
                            lvOptimizationActions.Items.Add(optimization);
                        }
                        lvOptimizationActions.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    tbEnhancedOptimizerResult.Text = $"Во время оптимизации произошла ошибка: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                tbEnhancedOptimizerResult.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                btnStartEnhancedOptimization.IsEnabled = true;
                enhancedOptimizerProgress.IsActive = false;
                enhancedOptimizerProgress.Visibility = Visibility.Collapsed;
            }
        }
        
        #endregion
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool>? _canExecute;
        
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
        
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}