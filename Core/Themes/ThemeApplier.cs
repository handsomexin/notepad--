using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SmartTextEditor.Models;

namespace SmartTextEditor.Themes
{
    /// <summary>
    /// 主题应用器 - 负责将主题应用到UI控件
    /// </summary>
    public static class ThemeApplier
    {
        // 添加动画持续时间常量
        private const double ANIMATION_DURATION_MS = 200.0;

        /// <summary>
        /// 应用主题到主窗口
        /// </summary>
        public static void ApplyThemeToMainWindow(Window window, ThemeColors theme)
        {
            try
            {
                // 使用动画平滑切换窗口背景
                AnimateBackground(window, theme.WindowBackground);

                // 应用到各个组件
                ApplyToMenus(window, theme);
                ApplyToToolBars(window, theme);
                ApplyToTabControl(window, theme);
                ApplyToStatusBar(window, theme);
                ApplyToTextEditors(window, theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用主题到菜单
        /// </summary>
        private static void ApplyToMenus(Window window, ThemeColors theme)
        {
            var menu = window.FindName("MainMenu") as Menu;
            if (menu != null)
            {
                AnimateBackground(menu, theme.MenuBackground);
                AnimateForeground(menu, theme.TextForeground);
                
                // 更新菜单项样式
                UpdateMenuItemStyles(menu, theme);
            }
        }

        /// <summary>
        /// 更新菜单项样式
        /// </summary>
        private static void UpdateMenuItemStyles(Menu menu, ThemeColors theme)
        {
            try
            {
                // 直接设置菜单的背景和前景色
                menu.Background = new SolidColorBrush(theme.MenuBackground);
                menu.Foreground = new SolidColorBrush(theme.TextForeground);
                
                // 遍历菜单项并设置样式
                foreach (var item in menu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        ApplyMenuItemStyle(menuItem, theme);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新菜单样式失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 应用样式到单个菜单项
        /// </summary>
        private static void ApplyMenuItemStyle(MenuItem menuItem, ThemeColors theme)
        {
            try
            {
                menuItem.Background = new SolidColorBrush(theme.MenuBackground);
                menuItem.Foreground = new SolidColorBrush(theme.TextForeground);
                
                // 设置鼠标悬停效果
                var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                var hoverBackground = new SolidColorBrush(theme.ButtonHoverBackground);
                trigger.Setters.Add(new Setter(Control.BackgroundProperty, hoverBackground));
                menuItem.Triggers.Add(trigger);
                
                // 递归应用到子菜单项
                foreach (var subItem in menuItem.Items)
                {
                    if (subItem is MenuItem subMenuItem)
                    {
                        ApplyMenuItemStyle(subMenuItem, theme);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用菜单项样式失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用主题到工具栏
        /// </summary>
        private static void ApplyToToolBars(Window window, ThemeColors theme)
        {
            var toolBar = window.FindName("MainToolBar") as ToolBar;
            if (toolBar != null)
            {
                AnimateBackground(toolBar, theme.ToolBarBackground);
                
                // 更新工具栏按钮
                UpdateToolBarButtons(toolBar, theme);
            }
        }

        /// <summary>
        /// 更新工具栏按钮样式
        /// </summary>
        private static void UpdateToolBarButtons(ToolBar toolBar, ThemeColors theme)
        {
            foreach (var child in toolBar.Items)
            {
                if (child is Button button)
                {
                    AnimateBackground(button, theme.ButtonBackground);
                    AnimateForeground(button, theme.TextForeground);
                    AnimateBorderBrush(button, theme.BorderColor);
                }
            }
        }

        /// <summary>
        /// 应用主题到标签页控件
        /// </summary>
        private static void ApplyToTabControl(Window window, ThemeColors theme)
        {
            var tabControl = window.FindName("FileTabControl") as TabControl;
            if (tabControl != null)
            {
                AnimateBackground(tabControl, theme.WindowBackground);
                
                // 更新现有标签页样式
                UpdateTabItemStyles(tabControl, theme);
            }
        }

        /// <summary>
        /// 更新标签页样式
        /// </summary>
        private static void UpdateTabItemStyles(TabControl tabControl, ThemeColors theme)
        {
            try
            {
                var tabItemStyle = new Style(typeof(TabItem));
                
                tabItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, 
                    new SolidColorBrush(theme.TabBackground)));
                tabItemStyle.Setters.Add(new Setter(Control.ForegroundProperty, 
                    new SolidColorBrush(theme.TextForeground)));
                tabItemStyle.Setters.Add(new Setter(Control.BorderBrushProperty, 
                    new SolidColorBrush(theme.TabBorder)));
                
                // 选中状态样式
                var selectedTrigger = new Trigger();
                selectedTrigger.Property = TabItem.IsSelectedProperty;
                selectedTrigger.Value = true;
                selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, 
                    new SolidColorBrush(theme.TabActiveBackground)));
                selectedTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, 
                    new SolidColorBrush(theme.TabActiveBorder)));
                selectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, 
                    new SolidColorBrush(theme.AccentColor)));
                
                tabItemStyle.Triggers.Add(selectedTrigger);
                
                tabControl.Resources[typeof(TabItem)] = tabItemStyle;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新标签页样式失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用主题到状态栏
        /// </summary>
        private static void ApplyToStatusBar(Window window, ThemeColors theme)
        {
            var statusBar = window.FindName("MainStatusBar") as StatusBar;
            if (statusBar != null)
            {
                AnimateBackground(statusBar, theme.StatusBarBackground);
                AnimateForeground(statusBar, theme.TextForeground);
            }
        }

        /// <summary>
        /// 应用主题到文本编辑器
        /// </summary>
        private static void ApplyToTextEditors(Window window, ThemeColors theme)
        {
            // 主欢迎页编辑器
            ApplyToTextEditor(window.FindName("WelcomeTextEditor") as TextBox, 
                             window.FindName("WelcomeLineNumbers") as TextBox, theme);
        }

        /// <summary>
        /// 应用主题到单个文本编辑器
        /// </summary>
        public static void ApplyToTextEditor(TextBox textEditor, TextBox lineNumbers, ThemeColors theme)
        {
            if (textEditor != null)
            {
                AnimateBackground(textEditor, theme.EditorBackground);
                AnimateForeground(textEditor, theme.TextForeground);
                AnimateBorderBrush(textEditor, theme.BorderColor);
                AnimateSelectionBrush(textEditor, theme.SelectionBackground);
            }

            if (lineNumbers != null)
            {
                AnimateBackground(lineNumbers, theme.LineNumberBackground);
                AnimateForeground(lineNumbers, theme.LineNumberForeground);
                AnimateBorderBrush(lineNumbers, theme.BorderColor);
            }
        }

        /// <summary>
        /// 应用主题到标签页项
        /// </summary>
        public static void ApplyToTabItem(FileTabItem tabItem, ThemeColors theme)
        {
            if (tabItem?.TextEditor != null)
            {
                ApplyToTextEditor(tabItem.TextEditor, tabItem.LineNumbersEditor, theme);
            }
        }

        #region 动画辅助方法

        /// <summary>
        /// 平滑动画背景色切换
        /// </summary>
        private static void AnimateBackground(Control control, Color newColor)
        {
            if (control == null) return;

            var animation = new ColorAnimation
            {
                To = newColor,
                Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS),
                EasingFunction = new QuadraticEase()
            };

            if (control.Background is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            else
            {
                control.Background = new SolidColorBrush(newColor);
            }
        }

        /// <summary>
        /// 平滑动画前景色切换
        /// </summary>
        private static void AnimateForeground(Control control, Color newColor)
        {
            if (control == null) return;

            var animation = new ColorAnimation
            {
                To = newColor,
                Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS),
                EasingFunction = new QuadraticEase()
            };

            if (control.Foreground is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            else
            {
                control.Foreground = new SolidColorBrush(newColor);
            }
        }

        /// <summary>
        /// 平滑动画边框色切换
        /// </summary>
        private static void AnimateBorderBrush(Control control, Color newColor)
        {
            if (control == null) return;

            var animation = new ColorAnimation
            {
                To = newColor,
                Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS),
                EasingFunction = new QuadraticEase()
            };

            if (control.BorderBrush is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            else
            {
                control.BorderBrush = new SolidColorBrush(newColor);
            }
        }

        /// <summary>
        /// 平滑动画窗口背景色切换
        /// </summary>
        private static void AnimateBackground(Window window, Color newColor)
        {
            if (window == null) return;

            var animation = new ColorAnimation
            {
                To = newColor,
                Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS),
                EasingFunction = new QuadraticEase()
            };

            if (window.Background is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            else
            {
                window.Background = new SolidColorBrush(newColor);
            }
        }

        /// <summary>
        /// 平滑动画选择背景色切换
        /// </summary>
        private static void AnimateSelectionBrush(TextBox textBox, Color newColor)
        {
            if (textBox == null) return;

            var animation = new ColorAnimation
            {
                To = newColor,
                Duration = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS),
                EasingFunction = new QuadraticEase()
            };

            if (textBox.SelectionBrush is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            else
            {
                textBox.SelectionBrush = new SolidColorBrush(newColor);
            }
        }

        #endregion
    }
}