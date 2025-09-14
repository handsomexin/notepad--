using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SmartTextEditor.Services;
using SmartTextEditor.Themes;

namespace SmartTextEditor.Windows
{
    /// <summary>
    /// 备份管理窗口
    /// </summary>
    public partial class BackupManagerWindow : Window
    {
        private string _currentFilePath;
        private List<BackupManager.BackupInfo> _backups;

        public BackupManagerWindow(string filePath = null)
        {
            InitializeComponent();
            _currentFilePath = filePath;
            
            // 应用当前主题
            ApplyCurrentTheme();
            
            // 初始化界面
            InitializeWindow();
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                var theme = ThemeManager.GetCurrentThemeColors();
                
                this.Background = new SolidColorBrush(theme.WindowBackground);
                
                // 更新按钮样式
                ApplyButtonTheme(RestoreButton, theme);
                ApplyButtonTheme(DeleteButton, theme);
                ApplyButtonTheme(CloseButton, theme);
                ApplyButtonTheme(RefreshButton, theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题失败: {ex.Message}");
            }
        }

        private void ApplyButtonTheme(Button button, ThemeColors theme)
        {
            if (button != null)
            {
                button.Background = new SolidColorBrush(theme.ButtonBackground);
                button.Foreground = new SolidColorBrush(theme.TextForeground);
                button.BorderBrush = new SolidColorBrush(theme.BorderColor);
            }
        }

        private async void InitializeWindow()
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    FileNameText.Text = $"文件: {System.IO.Path.GetFileName(_currentFilePath)}";
                    await LoadBackupsAsync();
                }
                else
                {
                    FileNameText.Text = "全局备份管理";
                    await LoadAllBackupsAsync();
                }
                
                await UpdateStatsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化窗口失败: {ex.Message}", "错误");
            }
        }

        private async Task LoadBackupsAsync()
        {
            try
            {
                _backups = await BackupManager.GetBackupsAsync(_currentFilePath);
                BackupDataGrid.ItemsSource = _backups.OrderByDescending(b => b.CreateTime).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载备份失败: {ex.Message}", "错误");
            }
        }

        private async Task LoadAllBackupsAsync()
        {
            try
            {
                // 这里可以实现加载所有备份的功能
                // 暂时显示空列表
                _backups = new List<BackupManager.BackupInfo>();
                BackupDataGrid.ItemsSource = _backups;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载所有备份失败: {ex.Message}", "错误");
            }
        }

        private async Task UpdateStatsAsync()
        {
            try
            {
                var stats = await BackupManager.GetBackupStatsAsync();
                BackupStatsText.Text = $"备份数量: {stats.totalBackups} | 总大小: {stats.totalSize / 1024}KB | 最后备份: {stats.lastBackup:yyyy-MM-dd}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新统计信息失败: {ex.Message}");
            }
        }

        #region 事件处理

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshButton.IsEnabled = false;
                RefreshButton.Content = "刷新中...";
                
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    await LoadBackupsAsync();
                }
                else
                {
                    await LoadAllBackupsAsync();
                }
                
                await UpdateStatsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新失败: {ex.Message}", "错误");
            }
            finally
            {
                RefreshButton.IsEnabled = true;
                RefreshButton.Content = "刷新";
            }
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedBackup = BackupDataGrid.SelectedItem as BackupManager.BackupInfo;
                if (selectedBackup == null)
                {
                    MessageBox.Show("请先选择一个备份", "提示");
                    return;
                }

                var result = MessageBox.Show(
                    $"确定要恢复到 {selectedBackup.CreateTime:yyyy-MM-dd HH:mm:ss} 的备份吗？\\n这将覆盖当前文件内容。", 
                    "确认恢复", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var content = await BackupManager.RestoreBackupAsync(selectedBackup);
                    if (content != null)
                    {
                        MessageBox.Show("备份恢复成功", "提示");
                        // 可以在这里通知主窗口更新内容
                    }
                    else
                    {
                        MessageBox.Show("备份恢复失败", "错误");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"恢复备份失败: {ex.Message}", "错误");
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedBackup = BackupDataGrid.SelectedItem as BackupManager.BackupInfo;
                if (selectedBackup == null)
                {
                    MessageBox.Show("请先选择一个备份", "提示");
                    return;
                }

                var result = MessageBox.Show(
                    $"确定要删除 {selectedBackup.CreateTime:yyyy-MM-dd HH:mm:ss} 的备份吗？\\n此操作不可撤销。", 
                    "确认删除", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var success = await BackupManager.DeleteBackupAsync(selectedBackup);
                    if (success)
                    {
                        MessageBox.Show("备份删除成功", "提示");
                        await LoadBackupsAsync(); // 重新加载列表
                        await UpdateStatsAsync();
                    }
                    else
                    {
                        MessageBox.Show("备份删除失败", "错误");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除备份失败: {ex.Message}", "错误");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }

    /// <summary>
    /// 布尔值到字符串转换器
    /// </summary>
    public class BooleanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAutoBackup)
            {
                return isAutoBackup ? "自动" : "手动";
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}