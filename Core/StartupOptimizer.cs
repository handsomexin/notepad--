using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SmartTextEditor
{
    /// <summary>
    /// 启动性能优化器
    /// </summary>
    public static class StartupOptimizer
    {
        /// <summary>
        /// 优化应用程序启动性能
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void OptimizeStartup()
        {
            try
            {
                // 设置垃圾回收器为工作站模式，减少启动时间
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                
                // 设置进程优先级
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                
                // 预热JIT编译器
                PrepareJIT();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动优化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 预热JIT编译器关键方法
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PrepareJIT()
        {
            try
            {
                // 预编译关键方法
                var appType = typeof(Application);
                var showMethod = typeof(Window).GetMethod("Show");
                var setTextMethod = typeof(System.Windows.Controls.TextBox).GetMethod("set_Text");
                
                if (appType != null && showMethod != null && setTextMethod != null)
                {
                    // 只有在方法存在时才预热
                    var runMethod = appType.GetMethod("Run", new Type[0]);
                    if (runMethod != null)
                        RuntimeHelpers.PrepareMethod(runMethod.MethodHandle);
                    RuntimeHelpers.PrepareMethod(showMethod.MethodHandle);
                    RuntimeHelpers.PrepareMethod(setTextMethod.MethodHandle);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"JIT预热失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 恢复正常性能设置
        /// </summary>
        public static void RestoreNormalSettings()
        {
            try
            {
                // 启动完成后恢复正常优先级
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                
                // 恢复正常的垃圾回收模式
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"恢复设置失败: {ex.Message}");
            }
        }
    }
}