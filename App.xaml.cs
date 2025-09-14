using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using SmartTextEditor.Models;
using SmartTextEditor.Services;

namespace SmartTextEditor
{
    /// <summary>
    /// Smart Text Editor 应用程序入口 - 极速启动优化
    /// </summary>
    public partial class App : Application
    {
        private static readonly Stopwatch _startupTimer = Stopwatch.StartNew();
        
        // 静态构造函数，用于预热关键类型
        static App()
        {
            // 预热关键程序集，减少首次加载时间
            _ = typeof(MainWindow);
            _ = typeof(FileTabItem);
            _ = typeof(EncodingDetector);
            _ = typeof(ObservableCollection<FileTabItem>);
            
            // 预热.NET类型系统
            _ = typeof(System.Windows.Controls.TextBox);
            _ = typeof(System.Windows.Controls.TabControl);
            _ = typeof(System.Windows.Input.ApplicationCommands);
        }
        
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 立即启动性能优化
            StartupOptimizer.OptimizeStartup();
            
            // 最小化启动逻辑，只做必要的初始化
            
            // 异步注册编码提供程序，避免阻塞UI线程
            _ = Task.Run(() => 
            {
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"编码提供程序注册失败: {ex.Message}");
                }
            });
            
            // 立即创建并显示主窗口（极简模式）
            var mainWindow = new MainWindow();
            
            // 记录启动时间
            var startupTime = _startupTimer.ElapsedMilliseconds;
            Debug.WriteLine($"⚙️ 窗口创建时间: {startupTime}ms");
            
            mainWindow.Show();
            
            // 记录显示时间
            var showTime = _startupTimer.ElapsedMilliseconds;
            Debug.WriteLine($"🚀 窗口显示时间: {showTime}ms");
            
            // 异步完成剩余初始化
            _ = Task.Run(async () => 
            {
                await Task.Delay(30); // 让UI先显示
                await mainWindow.CompleteInitializationAsync();
                
                var totalTime = _startupTimer.ElapsedMilliseconds;
                Debug.WriteLine($"✨ 总启动时间: {totalTime}ms");
                
                // 启动完成后恢复正常设置
                StartupOptimizer.RestoreNormalSettings();
            });
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            // 清理资源
            base.OnExit(e);
        }
    }
}