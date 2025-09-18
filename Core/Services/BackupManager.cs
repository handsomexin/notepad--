using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTextEditor.Services
{
    /// <summary>
    /// 备份管理器 - 提供文件备份、版本管理和恢复功能
    /// </summary>
    public static class BackupManager
    {
        private static readonly string BackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "SmartTextEditor", "Backups");

        private const int MaxBackupVersions = 10; // 最多保留10个版本
        private const int MaxBackupDays = 30; // 最多保留30天

        /// <summary>
        /// 备份文件信息
        /// </summary>
        public class BackupInfo
        {
            public string OriginalFilePath { get; set; }
            public string BackupFilePath { get; set; }
            public DateTime CreateTime { get; set; }
            public long FileSize { get; set; }
            public string Version { get; set; }
            public bool IsAutoBackup { get; set; }
            
            // 用于显示的属性
            public string BackupType => IsAutoBackup ? "自动" : "手动";
        }

        static BackupManager()
        {
            InitializeBackupDirectory();
        }

        private static void InitializeBackupDirectory()
        {
            try
            {
                if (!Directory.Exists(BackupDirectory))
                {
                    Directory.CreateDirectory(BackupDirectory);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化备份目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建文件备份
        /// </summary>
        /// <param name="filePath">要备份的文件路径</param>
        /// <param name="content">文件内容</param>
        /// <param name="isAutoBackup">是否为自动备份</param>
        /// <returns>备份是否成功</returns>
        public static async Task<bool> CreateBackupAsync(string filePath, string content, bool isAutoBackup = false)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || content == null)
                    return false;

                var fileName = Path.GetFileName(filePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{fileName}.{timestamp}.bak";
                var backupFilePath = Path.Combine(BackupDirectory, backupFileName);

                // 创建备份文件
                await File.WriteAllTextAsync(backupFilePath, content, Encoding.UTF8);

                // 保存备份信息
                await SaveBackupInfoAsync(new BackupInfo
                {
                    OriginalFilePath = filePath,
                    BackupFilePath = backupFilePath,
                    CreateTime = DateTime.Now,
                    FileSize = content.Length,
                    Version = timestamp,
                    IsAutoBackup = isAutoBackup
                });

                // 清理旧备份
                await CleanupOldBackupsAsync(filePath);

                System.Diagnostics.Debug.WriteLine($"备份创建成功: {backupFileName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建备份失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取文件的所有备份
        /// </summary>
        /// <param name="filePath">原文件路径</param>
        /// <returns>备份列表</returns>
        public static async Task<List<BackupInfo>> GetBackupsAsync(string filePath)
        {
            try
            {
                var backups = new List<BackupInfo>();
                var fileName = Path.GetFileName(filePath);

                if (!Directory.Exists(BackupDirectory))
                    return backups;

                var backupFiles = Directory.GetFiles(BackupDirectory, $"{fileName}.*.bak")
                    .OrderByDescending(f => File.GetCreationTime(f));

                foreach (var backupFile in backupFiles)
                {
                    var info = await GetBackupInfoAsync(backupFile);
                    if (info != null && info.OriginalFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        backups.Add(info);
                    }
                }

                return backups;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取备份列表失败: {ex.Message}");
                return new List<BackupInfo>();
            }
        }

        /// <summary>
        /// 恢复备份文件
        /// </summary>
        /// <param name="backupInfo">备份信息</param>
        /// <returns>恢复的文件内容</returns>
        public static async Task<string> RestoreBackupAsync(BackupInfo backupInfo)
        {
            try
            {
                if (backupInfo == null || !File.Exists(backupInfo.BackupFilePath))
                    return null;

                var content = await File.ReadAllTextAsync(backupInfo.BackupFilePath, Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"备份恢复成功: {backupInfo.BackupFilePath}");
                return content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复备份失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 删除指定备份
        /// </summary>
        /// <param name="backupInfo">备份信息</param>
        /// <returns>删除是否成功</returns>
        public static async Task<bool> DeleteBackupAsync(BackupInfo backupInfo)
        {
            try
            {
                if (backupInfo == null || !File.Exists(backupInfo.BackupFilePath))
                    return false;

                File.Delete(backupInfo.BackupFilePath);
                await RemoveBackupInfoAsync(backupInfo.BackupFilePath);

                System.Diagnostics.Debug.WriteLine($"备份删除成功: {backupInfo.BackupFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除备份失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理过期的备份文件
        /// </summary>
        /// <param name="filePath">原文件路径</param>
        private static async Task CleanupOldBackupsAsync(string filePath)
        {
            try
            {
                var backups = await GetBackupsAsync(filePath);
                
                // 按时间排序，保留最新的版本
                var sortedBackups = backups.OrderByDescending(b => b.CreateTime).ToList();

                // 删除超过版本限制的备份
                for (int i = MaxBackupVersions; i < sortedBackups.Count; i++)
                {
                    await DeleteBackupAsync(sortedBackups[i]);
                }

                // 删除超过时间限制的备份
                var cutoffDate = DateTime.Now.AddDays(-MaxBackupDays);
                var expiredBackups = sortedBackups.Where(b => b.CreateTime < cutoffDate).ToList();
                
                foreach (var backup in expiredBackups)
                {
                    await DeleteBackupAsync(backup);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理旧备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取备份统计信息
        /// </summary>
        /// <returns>备份统计</returns>
        public static async Task<(int totalBackups, long totalSize, DateTime lastBackup)> GetBackupStatsAsync()
        {
            try
            {
                if (!Directory.Exists(BackupDirectory))
                    return (0, 0, DateTime.MinValue);

                var backupFiles = await Task.Run(() => Directory.GetFiles(BackupDirectory, "*.bak"));
                var totalBackups = backupFiles.Length;
                var totalSize = await Task.Run(() => backupFiles.Sum(f => new FileInfo(f).Length));
                var lastBackup = await Task.Run(() => backupFiles.Length > 0 ? 
                    backupFiles.Max(f => File.GetCreationTime(f)) : DateTime.MinValue);

                return (totalBackups, totalSize, lastBackup);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取备份统计失败: {ex.Message}");
                return (0, 0, DateTime.MinValue);
            }
        }

        /// <summary>
        /// 保存备份信息到元数据文件
        /// </summary>
        private static async Task SaveBackupInfoAsync(BackupInfo info)
        {
            try
            {
                var metaFile = info.BackupFilePath + ".meta";
                var metaData = System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(metaFile, metaData, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存备份信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从元数据文件读取备份信息
        /// </summary>
        public static async Task<BackupInfo> GetBackupInfoAsync(string backupFilePath)
        {
            try
            {
                var metaFile = backupFilePath + ".meta";
                if (!File.Exists(metaFile))
                {
                    // 如果没有元数据文件，创建基本信息
                    var fileInfo = new FileInfo(backupFilePath);
                    return new BackupInfo
                    {
                        BackupFilePath = backupFilePath,
                        CreateTime = fileInfo.CreationTime,
                        FileSize = fileInfo.Length,
                        Version = fileInfo.CreationTime.ToString("yyyyMMdd_HHmmss"),
                        IsAutoBackup = false
                    };
                }

                var metaData = await File.ReadAllTextAsync(metaFile, Encoding.UTF8);
                return System.Text.Json.JsonSerializer.Deserialize<BackupInfo>(metaData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取备份信息失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 删除备份信息
        /// </summary>
        private static async Task RemoveBackupInfoAsync(string backupFilePath)
        {
            try
            {
                var metaFile = backupFilePath + ".meta";
                if (File.Exists(metaFile))
                {
                    await Task.Run(() => File.Delete(metaFile));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除备份信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理所有过期备份
        /// </summary>
        public static async Task CleanupAllExpiredBackupsAsync()
        {
            try
            {
                if (!Directory.Exists(BackupDirectory))
                    return;

                var backupFiles = Directory.GetFiles(BackupDirectory, "*.bak");
                var cutoffDate = DateTime.Now.AddDays(-MaxBackupDays);

                foreach (var backupFile in backupFiles)
                {
                    var fileInfo = new FileInfo(backupFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        var info = await GetBackupInfoAsync(backupFile);
                        if (info != null)
                        {
                            await DeleteBackupAsync(info);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理所有过期备份失败: {ex.Message}");
            }
        }
    }
}