using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SmartTextEditor.Themes;

namespace SmartTextEditor.Services
{
    /// <summary>
    /// 配置管理器 - 负责保存和加载用户设置
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartTextEditor");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

        /// <summary>
        /// 用户配置
        /// </summary>
        public class UserConfig
        {
            public string SelectedTheme { get; set; } = "Dark";
            public bool AutoSave { get; set; } = true;
            public int AutoSaveInterval { get; set; } = 10;
            public string LastOpenDirectory { get; set; } = "";
            public bool RememberWindowSize { get; set; } = true;
            public double WindowWidth { get; set; } = 1200;
            public double WindowHeight { get; set; } = 800;
            public bool RestoreSession { get; set; } = true;
            public List<SessionTab> LastSession { get; set; } = new List<SessionTab>();
            public int ActiveTabIndex { get; set; } = 0;
        }
        
        /// <summary>
        /// 会话标签页信息
        /// </summary>
        public class SessionTab
        {
            public string FileName { get; set; } = "";
            public string FilePath { get; set; } = "";
            public string Content { get; set; } = "";
            public string Encoding { get; set; } = "UTF-8";
            public bool IsModified { get; set; } = false;
            public int CursorPosition { get; set; } = 0;
            public int SelectionStart { get; set; } = 0;
            public int SelectionLength { get; set; } = 0;
        }

        /// <summary>
        /// 保存主题设置
        /// </summary>
        public static void SaveTheme(ThemeType themeType)
        {
            try
            {
                var config = LoadConfig();
                config.SelectedTheme = themeType.ToString();
                SaveConfig(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存主题设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载保存的主题
        /// </summary>
        public static ThemeType LoadTheme()
        {
            try
            {
                var config = LoadConfig();
                if (Enum.TryParse<ThemeType>(config.SelectedTheme, out var themeType))
                {
                    return themeType;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载主题设置失败: {ex.Message}");
            }
            
            return ThemeType.Dark; // 默认主题
        }

        /// <summary>
        /// 保存会话信息
        /// </summary>
        public static void SaveSession(List<SessionTab> sessionTabs, int activeTabIndex)
        {
            try
            {
                var config = LoadConfig();
                config.LastSession = sessionTabs ?? new List<SessionTab>();
                config.ActiveTabIndex = activeTabIndex;
                SaveConfig(config);
                System.Diagnostics.Debug.WriteLine($"已保存会话：{sessionTabs?.Count ?? 0}个标签页");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存会话失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载会话信息
        /// </summary>
        public static (List<SessionTab> tabs, int activeIndex) LoadSession()
        {
            try
            {
                var config = LoadConfig();
                if (config.RestoreSession && config.LastSession != null)
                {
                    System.Diagnostics.Debug.WriteLine($"已加载会话：{config.LastSession.Count}个标签页");
                    return (config.LastSession, config.ActiveTabIndex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载会话失败: {ex.Message}");
            }
            
            return (new List<SessionTab>(), 0);
        }

        /// <summary>
        /// 清除会话信息
        /// </summary>
        public static void ClearSession()
        {
            try
            {
                var config = LoadConfig();
                config.LastSession = new List<SessionTab>();
                config.ActiveTabIndex = 0;
                SaveConfig(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清除会话失败: {ex.Message}");
            }
        }
        public static void SaveWindowSettings(double width, double height)
        {
            try
            {
                var config = LoadConfig();
                config.WindowWidth = width;
                config.WindowHeight = height;
                SaveConfig(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存窗口设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载完整配置
        /// </summary>
        public static UserConfig LoadConfig()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<UserConfig>(json);
                    return config ?? new UserConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
            }

            return new UserConfig();
        }

        /// <summary>
        /// 保存完整配置
        /// </summary>
        public static void SaveConfig(UserConfig config)
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
            }
        }
    }
}