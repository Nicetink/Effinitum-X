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

        public MainWindow()
        {
            InitializeComponent();
            _systemService = new SystemService();
            _settings = Settings.Load();
            
            _systemInfoItems = new ObservableCollection<KeyValuePair<string, string>>();
            _diskInfoItems = new ObservableCollection<KeyValuePair<string, string>>();
            _folderSizeItems = new ObservableCollection<FolderSizeInfo>();
            _processItems = new ObservableCollection<ProcessViewModel>();
            _startupItems = new ObservableCollection<StartupItem>();
            
            lvSystemInfo.ItemsSource = _systemInfoItems;
            lvDiskInfo.ItemsSource = _diskInfoItems;
            lvFolderSize.ItemsSource = _folderSizeItems;
            dgProcesses.ItemsSource = _processItems;
            lvStartupItems.ItemsSource = _startupItems;
            
            // Initialize drives combobox
            LoadDrives();
            
            // Set up process filtering
            tbProcessFilter.TextChanged += TbProcessFilter_TextChanged;
            
            // Set up theme based on settings
            ApplyThemeSettings();
            
            // Set up cleanup level display
            sliderCleanupLevel.ValueChanged += SliderCleanupLevel_ValueChanged;
            UpdateCleanupLevelText();
            
            // Load other settings
            cbRunAtStartup.IsChecked = _settings.RunAtStartup;
            cbMinimizeToTray.IsChecked = _settings.MinimizeToTray;
            cbEnableNotifications.IsChecked = _settings.EnableNotifications;
            sliderCleanupLevel.Value = _settings.CleanupLevel;
            
            // Show welcome page by default
            HideAllPages();
            welcomePage.Visibility = Visibility.Visible;
        }
        
        private void LoadDrives()
        {
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

        #region Обработчики событий меню
        
        private void btnSystemInfo_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            systemInfoPage.Visibility = Visibility.Visible;
            LoadSystemInfo();
        }

        private void btnCleaner_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            cleanerPage.Visibility = Visibility.Visible;
        }

        private void btnDiskAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            diskAnalyzerPage.Visibility = Visibility.Visible;
        }

        private void btnProcesses_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            processesPage.Visibility = Visibility.Visible;
            LoadProcesses();
        }

        private void btnStartup_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            startupPage.Visibility = Visibility.Visible;
        }
        
        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            settingsPage.Visibility = Visibility.Visible;
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
        
        private void HideAllPages()
        {
            welcomePage.Visibility = Visibility.Collapsed;
            systemInfoPage.Visibility = Visibility.Collapsed;
            cleanerPage.Visibility = Visibility.Collapsed;
            diskAnalyzerPage.Visibility = Visibility.Collapsed;
            processesPage.Visibility = Visibility.Collapsed;
            startupPage.Visibility = Visibility.Collapsed;
            settingsPage.Visibility = Visibility.Collapsed;
            diskOptimizationPage.Visibility = Visibility.Collapsed;
            uwpAppsPage.Visibility = Visibility.Collapsed;
            defenderPage.Visibility = Visibility.Collapsed;
            updatesPage.Visibility = Visibility.Collapsed;
            zapretPage.Visibility = Visibility.Collapsed;
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
            _settings.RunAtStartup = cbRunAtStartup.IsChecked ?? false;
            _settings.MinimizeToTray = cbMinimizeToTray.IsChecked ?? false;
            _settings.EnableNotifications = cbEnableNotifications.IsChecked ?? false;
            
            // Apply run at startup setting
            ApplyRunAtStartup(_settings.RunAtStartup);
            
            // Save settings
            _settings.Save();
            
            MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Пожалуйста, выберите диск для оптимизации", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                
                if (cbCheckDisk.IsChecked == true)
                {
                    tbOptimizationResult.Text = "Проверка диска на ошибки...";
                    await _systemService.CheckDiskHealth(selectedDrive[0].ToString());
                }
                
                var result = await _systemService.OptimizeDisk(
                    selectedDrive[0].ToString(), 
                    cbDefragment.IsChecked ?? false, 
                    cbCleanupDisk.IsChecked ?? false);
                
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
                    MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (e.SelectedItemContainer != null)
            {
                string tag = e.SelectedItemContainer.Tag?.ToString() ?? string.Empty;
                
                HideAllPages();
                
                switch (tag)
                {
                    case "welcome":
                        welcomePage.Visibility = Visibility.Visible;
                        break;
                    case "systemInfo":
                        systemInfoPage.Visibility = Visibility.Visible;
                        LoadSystemInfo();
                        break;
                    case "cleaner":
                        cleanerPage.Visibility = Visibility.Visible;
                        break;
                    case "diskAnalyzer":
                        diskAnalyzerPage.Visibility = Visibility.Visible;
                        break;
                    case "processes":
                        processesPage.Visibility = Visibility.Visible;
                        LoadProcesses();
                        break;
                    case "startup":
                        startupPage.Visibility = Visibility.Visible;
                        break;
                    case "diskOptimization":
                        diskOptimizationPage.Visibility = Visibility.Visible;
                        LoadDrivesForOptimization();
                        break;
                    case "uwpApps":
                        uwpAppsPage.Visibility = Visibility.Visible;
                        LoadUwpApps();
                        break;
                    case "defender":
                        defenderPage.Visibility = Visibility.Visible;
                        CheckDefenderStatus();
                        break;
                    case "updates":
                        updatesPage.Visibility = Visibility.Visible;
                        CheckUpdatesStatus();
                        break;
                    case "zapret":
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
                            MessageBox.Show("Приложение успешно удалено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadUwpApps(); // Обновляем список
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить приложение. Убедитесь, что у вас есть необходимые права.", 
                                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                bool isInstalled = File.Exists(@"C:\Program Files\Zapret-Discord-YouTube\unins000.exe");
                
                tbZapretStatus.Text = isInstalled ? 
                    "Zapret-Discord-YouTube установлен." : 
                    "Zapret-Discord-YouTube не установлен.";
                
                btnInstallZapret.IsEnabled = !isInstalled;
                btnUninstallZapret.IsEnabled = isInstalled;
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
        
        private async void btnInstallZapret_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnInstallZapret.IsEnabled = false;
                btnUninstallZapret.IsEnabled = false;
                zapretProgress.IsActive = true;
                zapretProgress.Visibility = Visibility.Visible;
                tbZapretResult.Text = "Установка Zapret-Discord-YouTube...";
                
                // Здесь будет код загрузки и установки программы
                tbZapretResult.Text = "Функция будет реализована в следующей версии.";
                CheckZapretStatus();
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
                
                // Здесь будет код удаления программы
                tbZapretResult.Text = "Функция будет реализована в следующей версии.";
                CheckZapretStatus();
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