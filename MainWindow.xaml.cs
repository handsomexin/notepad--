using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using SmartTextEditor.Services;
using SmartTextEditor.Models;
using SmartTextEditor.Windows;
using SmartTextEditor.Themes;
using System.IO.Compression;

namespace SmartTextEditor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑 - 支持多标签页 - 极速启动优化
    /// </summary>
    public partial class MainWindow : Window
    {
        // 延迟初始化的服务
        private EncodingDetector _encodingDetector;
        private DispatcherTimer _autoCacheTimer;
        private readonly string _cacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Cache");
        private readonly ObservableCollection<FileTabItem> _tabItems;
        private FileTabItem _currentTab;
        private int _newFileCounter = 1;
        private bool _isFullyInitialized = false;

        public MainWindow()
        {
            // 极简初始化，只做必要的UI初始化
            InitializeComponent();
            _tabItems = new ObservableCollection<FileTabItem>();
            
            // 设置窗口基本属性（延迟到显示后）
            this.Title = "Smart Text Editor";
            
            // 加载保存的窗口设置
            LoadWindowSettings();
            
            // 创建最简单的欢迎标签页
            CreateMinimalWelcomeTab();
        }

        private void LoadWindowSettings()
        {
            try
            {
                var config = ConfigManager.LoadConfig();
                if (config.RememberWindowSize)
                {
                    this.Width = config.WindowWidth;
                    this.Height = config.WindowHeight;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载窗口设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步完成剩余的初始化工作
        /// </summary>
        public async Task CompleteInitializationAsync()
        {
            if (_isFullyInitialized) return;
            
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    // 延迟初始化服务
                    await Task.Run(() =>
                    {
                        _encodingDetector = new EncodingDetector();
                    });
                    
                    // 初始化编辑器功能
                    InitializeEditor();
                    
                    // 初始化自动缓存
                    InitializeAutoCache();
                    
                    // 完善欢迎页内容
                    CompleteWelcomeTab();
                    
                    // 初始化主题系统
                    InitializeThemeSystem();
                    
                    // 恢复上次会话
                    await RestoreLastSession();
                    
                    _isFullyInitialized = true;
                    
                    // 更新状态
                    UpdateStatus("就绪 - 所有功能已加载");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化失败: {ex.Message}");
            }
        }

        #region 初始化

        private void CreateMinimalWelcomeTab()
        {
            // 极简欢迎页，只显示基本信息
            var welcomeContent = "🚀 Smart Text Editor - 正在加载...";

            var welcomeTab = new FileTabItem
            {
                FileName = "欢迎",
                Content = welcomeContent
            };
            welcomeTab.OriginalContent = welcomeContent;

            _tabItems.Add(welcomeTab);
            _currentTab = welcomeTab;

            // 简单设置欢迎页内容（无事件绑定）
            if (WelcomeTextEditor != null)
            {
                WelcomeTextEditor.Text = welcomeContent;
            }
            if (WelcomeLineNumbers != null)
            {
                UpdateLineNumbers(WelcomeLineNumbers, WelcomeTextEditor);
            }
        }
        
        private void CompleteWelcomeTab()
        {
            // 在异步初始化后完善欢迎页内容
            var fullWelcomeContent = @"🎉 欢迎使用 Smart Text Editor v1.3！

✨ 最新功能 - 智能会话恢复：
• 会话自动保存 - 关闭程序时自动保存所有标签页
• 无缝恢复体验 - 重启后完全恢复工作状态
• 智能状态保持 - 文件内容、修改状态、光标位置
• 编码格式记忆 - 各文件编码设置完整保留
• 异常情况处理 - 文件移动删除的智能处理

🎨 多主题系统：
• 6种精美主题 - 适应不同使用场景
• 智能主题记忆 - 程序记住您的主题选择
• 自动保存设置 - 下次启动自动应用
• 窗口大小记忆 - 保持您喜欢的界面布局

💾 会话恢复特性：
• 标签页完整恢复 - 所有打开的文件自动恢复
• 编辑状态保持 - 光标位置、文本选择状态
• 修改状态记忆 - 未保存修改的完整保留
• 活跃标签恢复 - 恢复到关闭前的工作标签
• 混合文件支持 - 新文件、已保存文件统一处理

⌨️ 操作指南：
• 正常编辑工作，程序会自动记录状态
• 关闭程序时会自动保存当前会话
• 重新启动时自动恢复到上次的工作状态
• 配置文件：%LOCALAPPDATA%\SmartTextEditor\

⌨️ 快捷键：
• Ctrl+N - 新建标签页
• Ctrl+O - 打开文件
• Ctrl+S - 保存文件
• Ctrl+D - 文件对比
• Ctrl+Tab - 切换标签页

🚀 享受无缝的工作体验吧！";

            if (_currentTab != null && _currentTab.FileName == "欢迎")
            {
                _currentTab.Content = fullWelcomeContent;
                _currentTab.OriginalContent = fullWelcomeContent;
                WelcomeTextEditor.Text = fullWelcomeContent;
                
                // 设置事件处理
                WelcomeTextEditor.TextChanged += (s, e) => OnCurrentTabTextChanged();
                WelcomeTextEditor.SelectionChanged += (s, e) => {
                    UpdateCursorPosition();
                    UpdateSelectionInfo();
                };
                WelcomeTextEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
                
                UpdateLineNumbers(WelcomeLineNumbers, WelcomeTextEditor);
                UpdateCursorPosition();
                UpdateSelectionInfo();
            }
        }

        private void InitializeEditor()
        {
            try
            {
                UpdateTitle();
                this.CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (s, e) => NewFile()));
                this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, (s, e) => OpenFile()));
                this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (s, e) => SaveFile()));
                this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Properties, (s, e) => FileCompare_Click(s, e)));
                this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (s, e) => ShowFindDialog()));
                this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, (s, e) => ShowFindReplaceDialog()));
                this.CommandBindings.Add(new CommandBinding(NavigationCommands.Search, (s, e) => FindNext()));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑器初始化失败: {ex.Message}");
            }
        }

        private void InitializeThemeSystem()
        {
            try
            {
                // 加载保存的主题设置
                var savedTheme = ConfigManager.LoadTheme();
                
                // 设置主题
                ThemeManager.SetTheme(savedTheme);
                var themeColors = ThemeManager.GetCurrentThemeColors();

                // 应用主题到主窗口
                ThemeApplier.ApplyThemeToMainWindow(this, themeColors);

                // 应用主题到所有标签页
                foreach (var tabItem in _tabItems)
                {
                    ThemeApplier.ApplyToTabItem(tabItem, themeColors);
                }

                // 更新菜单选中状态
                UpdateThemeMenuSelection(savedTheme);

                System.Diagnostics.Debug.WriteLine($"主题系统初始化完成，已加载主题: {themeColors.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"主题系统初始化失败: {ex.Message}");
            }
        }

        #endregion

        #region 标签页管理

        private void CreateNewTab(string fileName = null, string content = "")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CreateNewTab called with fileName: {fileName}, content length: {content?.Length ?? 0}");

                var tabItem = new FileTabItem
                {
                    FileName = fileName ?? $"无标题{_newFileCounter++}",
                    Content = content,
                    Encoding = "UTF-8"
                };
                tabItem.OriginalContent = content;

                // 获取TabControl
                var tabControl = FileTabControl;
                System.Diagnostics.Debug.WriteLine($"FileTabControl reference: {tabControl != null}");
                if (tabControl == null)
                {
                    var error = "无法找到标签页控件(FileTabControl)";
                    System.Diagnostics.Debug.WriteLine(error);
                    MessageBox.Show(error, "错误");
                    return;
                }

                // 创建新的标签页UI
                var newTabItem = new TabItem
                {
                    Header = tabItem.DisplayName,
                    DataContext = tabItem
                };

                // 创建编辑器内容
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 35 });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lineNumbers = new TextBox
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7D, 0x85, 0x90)),
                    BorderThickness = new Thickness(0, 0, 1, 0),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    IsReadOnly = true,
                    IsTabStop = false,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    TextAlignment = TextAlignment.Right,
                    Padding = new Thickness(8, 12, 8, 12),
                    Width = 35,
                    MinWidth = 35
                };

                var textEditor = new TextBox
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D)),
                    BorderThickness = new Thickness(1),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    Padding = new Thickness(12),
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    TextWrapping = TextWrapping.NoWrap,
                    Text = content
                };

                // 绑定事件
                textEditor.TextChanged += (s, e) => {
                    try {
                        OnTabTextChanged(tabItem, textEditor);
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"文本变化事件错误: {ex.Message}");
                    }
                };
                textEditor.SelectionChanged += (s, e) => {
                    try {
                        UpdateCursorPosition();
                        UpdateSelectionInfo();
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"选择变化事件错误: {ex.Message}");
                    }
                };
                textEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
                
                // 添加滚动同步事件 - 使用附加属性
                textEditor.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((s, e) => {
                    try {
                        // 同步行号滚动
                        if (lineNumbers != null)
                        {
                            lineNumbers.ScrollToVerticalOffset(e.VerticalOffset);
                        }
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"滚动同步错误: {ex.Message}");
                    }
                }));

                // 设置控件引用
                tabItem.TextEditor = textEditor;
                tabItem.LineNumbersEditor = lineNumbers;

                Grid.SetColumn(lineNumbers, 0);
                Grid.SetColumn(textEditor, 1);
                grid.Children.Add(lineNumbers);
                grid.Children.Add(textEditor);

                newTabItem.Content = grid;
                _tabItems.Add(tabItem);

                // 添加到TabControl
                System.Diagnostics.Debug.WriteLine($"Adding tab item to FileTabControl");
                tabControl.Items.Add(newTabItem);
                tabControl.SelectedItem = newTabItem;

                _currentTab = tabItem;
                UpdateLineNumbers(lineNumbers, textEditor);
                UpdateTitle();
                UpdateTabList();
                
                // 应用当前主题到新标签页
                if (_isFullyInitialized)
                {
                    var currentTheme = ThemeManager.GetCurrentThemeColors();
                    ThemeApplier.ApplyToTabItem(tabItem, currentTheme);
                }
                
                UpdateStatus($"已创建新标签页: {tabItem.FileName}");
                System.Diagnostics.Debug.WriteLine($"Successfully created new tab: {tabItem.FileName}");
            }
            catch (Exception ex)
            {
                var errorMessage = $"创建新标签页失败: {ex.Message}\n{ex.StackTrace}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                MessageBox.Show(errorMessage, "错误");
            }
        }

        private void CloseTab(FileTabItem tabItem)
        {
            if (tabItem == null) return;

            // 检查是否有未保存的修改
            if (tabItem.IsModified)
            {
                var result = MessageBox.Show(
                    $"文件 '{tabItem.FileName}' 已修改，是否保存更改？",
                    "保存更改",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        SaveFile(tabItem);
                        break;
                    case MessageBoxResult.Cancel:
                        return; // 取消关闭操作
                }
            }

            // 从集合中移除
            _tabItems.Remove(tabItem);

            // 从UI中移除
            var tabControl = FileTabControl;
            if (tabControl != null)
            {
                var tabToRemove = tabControl.Items.Cast<TabItem>()
                    .FirstOrDefault(t => t.DataContext == tabItem);
                if (tabToRemove != null)
                {
                    tabControl.Items.Remove(tabToRemove);
                }
            }

            // 如果关闭的是当前标签页，切换到其他标签页
            if (_currentTab == tabItem)
            {
                if (_tabItems.Count > 0)
                {
                    _currentTab = _tabItems.Last(); // 切换到最后一个标签页
                    SwitchToTab(_currentTab);
                }
                else
                {
                    // 如果没有标签页了，创建一个新的欢迎标签页
                    CreateMinimalWelcomeTab();
                    _currentTab = _tabItems[0];
                    SwitchToTab(_currentTab);
                }
            }

            UpdateTitle();
            UpdateTabList();
            UpdateStatus("标签页已关闭");
        }

        private void UpdateTabHeader(FileTabItem tabItem)
        {
            var tabControl = FileTabControl;
            if (tabControl == null) return;

            foreach (TabItem tab in tabControl.Items)
            {
                if (tab.DataContext == tabItem)
                {
                    tab.Header = tabItem.DisplayName;
                    break;
                }
            }
        }

        private void UpdateTabList()
        {
            if (TabListMenuItem == null) return;

            // 清除现有菜单项
            TabListMenuItem.Items.Clear();

            // 添加所有标签页到菜单
            for (int i = 0; i < _tabItems.Count; i++)
            {
                var tabItem = _tabItems[i];
                var menuItem = new MenuItem
                {
                    Header = $"{i + 1}. {tabItem.DisplayName}",
                    DataContext = tabItem
                };
                menuItem.Click += (s, e) => {
                    if (menuItem.DataContext is FileTabItem tab)
                    {
                        SwitchToTab(tab);
                    }
                };
                TabListMenuItem.Items.Add(menuItem);
            }
        }

        #endregion

        #region 文件操作

        private void NewFile()
        {
            // 检查是否已完成初始化，如果没有完成则延迟执行
            if (!_isFullyInitialized)
            {
                System.Diagnostics.Debug.WriteLine("Initialization not complete, delaying NewFile operation");
                // 使用Dispatcher延迟执行，确保UI完全初始化
                Dispatcher.BeginInvoke(new Action(() => {
                    if (_isFullyInitialized)
                    {
                        CreateNewTab();
                    }
                    else
                    {
                        // 如果还是没有初始化完成，显示提示
                        MessageBox.Show("编辑器正在初始化，请稍后再试...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
                return;
            }
            
            CreateNewTab();
        }

        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "所有文件 (*.*)|*.*|文本文件 (*.txt)|*.txt|C# 文件 (*.cs)|*.cs|JSON 文件 (*.json)|*.json|XML 文件 (*.xml)|*.xml",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = openFileDialog.FileName;
                    var encodingResult = _encodingDetector?.DetectFileEncoding(filePath);
                    var encoding = encodingResult?.Encoding ?? Encoding.UTF8;
                    var content = File.ReadAllText(filePath, encoding);

                    CreateNewTab(Path.GetFileName(filePath), content);
                    _currentTab.FilePath = filePath;
                    _currentTab.Encoding = encodingResult?.EncodingName ?? encoding.EncodingName;
                    UpdateStatus($"已打开文件: {filePath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"打开文件失败: {ex.Message}");
                }
            }
        }

        private void SaveFile(FileTabItem tabItem = null)
        {
            tabItem = tabItem ?? _currentTab;
            if (tabItem == null) return;

            try
            {
                if (string.IsNullOrEmpty(tabItem.FilePath))
                {
                    // 保存为新文件
                    SaveAsFile(tabItem);
                }
                else
                {
                    // 保存到现有文件
                    var encoding = GetEncodingByName(tabItem.Encoding);
                    File.WriteAllText(tabItem.FilePath, tabItem.Content, encoding);
                    tabItem.OriginalContent = tabItem.Content;
                    UpdateStatus($"文件已保存: {tabItem.FilePath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"保存文件失败: {ex.Message}");
            }
        }

        private void SaveAsFile(FileTabItem tabItem = null)
        {
            tabItem = tabItem ?? _currentTab;
            if (tabItem == null) return;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                FileName = tabItem.FileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = saveFileDialog.FileName;
                    var encoding = Encoding.UTF8; // 默认使用UTF-8编码
                    File.WriteAllText(filePath, tabItem.Content, encoding);

                    tabItem.FilePath = filePath;
                    tabItem.Encoding = encoding.EncodingName;
                    tabItem.OriginalContent = tabItem.Content;
                    UpdateStatus($"文件已保存: {filePath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"保存文件失败: {ex.Message}");
                }
            }
        }

        private Encoding GetEncodingByName(string encodingName)
        {
            try
            {
                return encodingName.ToUpper() switch
                {
                    "UTF-8" => Encoding.UTF8,
                    "GBK" => Encoding.GetEncoding("GBK"),
                    "UTF-16" => Encoding.Unicode,
                    "ASCII" => Encoding.ASCII,
                    _ => Encoding.UTF8
                };
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        #endregion

        #region UI更新

        private void UpdateTitle()
        {
            var appName = "Smart Text Editor";
            if (_currentTab != null)
            {
                this.Title = $"{_currentTab.DisplayName} - {appName}";
            }
            else
            {
                this.Title = appName;
            }
        }

        private void UpdateLineNumbers(TextBox lineNumbersEditor, TextBox textEditor)
        {
            if (lineNumbersEditor == null || textEditor == null) return;

            try
            {
                var lineCount = textEditor.LineCount;
                var lineNumbersText = new StringBuilder();

                for (int i = 1; i <= lineCount; i++)
                {
                    lineNumbersText.AppendLine(i.ToString());
                }

                lineNumbersEditor.Text = lineNumbersText.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新行号失败: {ex.Message}");
            }
        }

        private void UpdateCursorPosition()
        {
            if (_currentTab?.TextEditor == null || CursorPositionText == null) return;

            try
            {
                var textEditor = _currentTab.TextEditor;
                var caretIndex = textEditor.CaretIndex;
                var line = textEditor.GetLineIndexFromCharacterIndex(caretIndex) + 1;
                var col = caretIndex - textEditor.GetCharacterIndexFromLineIndex(line - 1) + 1;

                CursorPositionText.Text = $"行 {line}, 列 {col}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新光标位置失败: {ex.Message}");
            }
        }

        private void UpdateSelectionInfo()
        {
            if (_currentTab?.TextEditor == null || SelectionInfoText == null) return;

            try
            {
                var textEditor = _currentTab.TextEditor;
                var selectionLength = textEditor.SelectionLength;

                if (selectionLength > 0)
                {
                    SelectionInfoText.Text = $"已选择 {selectionLength} 个字符";
                }
                else
                {
                    SelectionInfoText.Text = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新选择信息失败: {ex.Message}");
            }
        }

        private void UpdateStatus(string message)
        {
            if (StatusText != null)
            {
                StatusText.Text = message;
            }
        }

        #endregion

        #region 文件对比

        private void FileCompare_Click(object sender, RoutedEventArgs e)
        {
            var compareWindow = new FileCompareWindow();
            compareWindow.Show();
        }

        #endregion

        #region 搜索替换

        private void ShowFindDialog()
        {
            if (_currentTab?.TextEditor != null)
            {
                var findDialog = new FindReplaceWindow(_currentTab.TextEditor);
                findDialog.Show();
            }
        }

        private void ShowFindReplaceDialog()
        {
            if (_currentTab?.TextEditor != null)
            {
                var findReplaceDialog = new FindReplaceWindow(_currentTab.TextEditor);
                findReplaceDialog.Show();
            }
        }

        private void FindNext()
        {
            // 这里可以实现查找下一个的功能
        }

        #endregion

        #region 事件处理

        private void OnTabTextChanged(FileTabItem tabItem, TextBox textEditor)
        {
            if (tabItem == null || textEditor == null) return;

            tabItem.Content = textEditor.Text;
            UpdateLineNumbers(tabItem.LineNumbersEditor, textEditor);
            UpdateCursorPosition();
            UpdateSelectionInfo();
        }

        private void OnCurrentTabTextChanged()
        {
            if (_currentTab?.TextEditor != null)
            {
                OnTabTextChanged(_currentTab, _currentTab.TextEditor);
            }
        }

        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 处理Tab键输入
            if (e.Key == Key.Tab && sender is TextBox textBox)
            {
                // 插入制表符而不是切换焦点
                var caretIndex = textBox.CaretIndex;
                var text = textBox.Text;
                textBox.Text = text.Insert(caretIndex, "\t");
                textBox.CaretIndex = caretIndex + 1;
                e.Handled = true;
            }
        }

        private void WelcomeTextEditor_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 同步欢迎页行号滚动
            if (WelcomeLineNumbers != null)
            {
                WelcomeLineNumbers.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        #endregion

        #region 点击事件处理方法

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            NewFile();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            SaveAsFile();
        }

        private void SaveAllFiles_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tabItem in _tabItems)
            {
                if (tabItem.IsModified)
                {
                    SaveFile(tabItem);
                }
            }
            UpdateStatus("所有文件已保存");
        }

        private void CloseCurrentTab_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab != null)
            {
                CloseTab(_currentTab);
            }
        }

        private void CloseOtherTabs_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == null) return;

            // 创建要关闭的标签页列表（除了当前标签页）
            var tabsToClose = _tabItems.Where(t => t != _currentTab).ToList();

            // 关闭所有其他标签页
            foreach (var tab in tabsToClose)
            {
                CloseTab(tab);
            }
        }

        private void CloseAllTabs_Click(object sender, RoutedEventArgs e)
        {
            // 创建要关闭的标签页列表
            var tabsToClose = _tabItems.ToList();

            // 关闭所有标签页
            foreach (var tab in tabsToClose)
            {
                CloseTab(tab);
            }
        }

        private void NextTab_Click(object sender, RoutedEventArgs e)
        {
            if (_tabItems.Count <= 1) return;

            var currentIndex = _tabItems.IndexOf(_currentTab);
            var nextIndex = (currentIndex + 1) % _tabItems.Count;
            SwitchToTab(_tabItems[nextIndex]);
        }

        private void PreviousTab_Click(object sender, RoutedEventArgs e)
        {
            if (_tabItems.Count <= 1) return;

            var currentIndex = _tabItems.IndexOf(_currentTab);
            var previousIndex = (currentIndex - 1 + _tabItems.Count) % _tabItems.Count;
            SwitchToTab(_tabItems[previousIndex]);
        }

        private void CompareCurrentFiles_Click(object sender, RoutedEventArgs e)
        {
            if (_tabItems.Count < 2)
            {
                MessageBox.Show("至少需要两个打开的文件才能进行对比", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 获取前两个标签页的内容
            var firstTab = _tabItems[0];
            var secondTab = _tabItems[1];

            var compareWindow = new FileCompareWindow(firstTab.Content, secondTab.Content, 
                firstTab.FileName, secondTab.FileName);
            compareWindow.Show();
        }

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            ShowFindDialog();
        }

        private void FindReplace_Click(object sender, RoutedEventArgs e)
        {
            ShowFindReplaceDialog();
        }

        private void DetectEncoding_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab?.FilePath != null)
            {
                try
                {
                    var detectedResult = _encodingDetector?.DetectFileEncoding(_currentTab.FilePath);
                    if (detectedResult != null)
                    {
                        _currentTab.Encoding = detectedResult.EncodingName;
                        UpdateStatus($"检测到编码: {detectedResult.EncodingName} (置信度: {detectedResult.Confidence:P1})");
                    }
                    else
                    {
                        UpdateStatus("无法检测文件编码");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"编码检测失败: {ex.Message}");
                }
            }
            else
            {
                UpdateStatus("当前文件未保存，无法检测编码");
            }
        }

        private void ConvertEncoding_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == null) return;

            // 创建一个简单的编码转换窗口
            var dialog = new Window
            {
                Title = "编码转换",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var comboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            comboBox.Items.Add("UTF-8");
            comboBox.Items.Add("GBK");
            comboBox.Items.Add("UTF-16");
            comboBox.SelectedItem = _currentTab.Encoding;

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "取消", Width = 80 };

            okButton.Click += (s, ev) => {
                if (comboBox.SelectedItem != null)
                {
                    _currentTab.Encoding = comboBox.SelectedItem.ToString();
                    UpdateStatus($"编码已转换为: {_currentTab.Encoding}");
                }
                dialog.Close();
            };

            cancelButton.Click += (s, ev) => dialog.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(new Label { Content = "选择编码:" });
            stackPanel.Children.Add(comboBox);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;
            dialog.ShowDialog();
        }

        private void ConvertToUtf8_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab != null)
            {
                _currentTab.Encoding = "UTF-8";
                UpdateStatus("编码已设置为 UTF-8");
            }
        }

        private void ConvertToGbk_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab != null)
            {
                _currentTab.Encoding = "GBK";
                UpdateStatus("编码已设置为 GBK");
            }
        }

        private void ConvertToUtf16_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab != null)
            {
                _currentTab.Encoding = "UTF-16";
                UpdateStatus("编码已设置为 UTF-16");
            }
        }

        private void BackupManager_Click(object sender, RoutedEventArgs e)
        {
            var backupManagerWindow = new BackupManagerWindow();
            backupManagerWindow.Show();
        }

        #endregion

        #region TabControl 事件处理

        private void FileTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
            {
                _currentTab = selectedTab.DataContext as FileTabItem;
                UpdateTitle();
                UpdateCursorPosition();
                UpdateSelectionInfo();
            }
        }

        private void FileTabControl_RightClick(object sender, MouseButtonEventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;

            // 获取点击的标签页
            var tabItem = FindParent<TabItem>(e.OriginalSource as DependencyObject);
            if (tabItem != null)
            {
                tabControl.SelectedItem = tabItem;
                TabContextMenu.IsOpen = true;
            }
        }

        private void FileTabControl_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 检查是否点击在标签页控件的空白区域
            var tabControl = sender as TabControl;
            if (tabControl == null) return;

            // 如果点击的是标签页控件本身（而不是标签项），创建新文件
            if (e.OriginalSource is TabPanel || e.OriginalSource is Border)
            {
                NewFile();
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileTabItem tabItem)
            {
                CloseTab(tabItem);
            }
        }

        #endregion

        #region 主题系统

        private void SetDarkTheme_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(ThemeType.Dark);
        }

        private void SetLightTheme_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(ThemeType.Light);
        }

        private void SetHighContrastTheme_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(ThemeType.HighContrast);
        }

        private void SetEyeCareTheme_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(ThemeType.EyeCare);
        }

        private void SetMonokaiTheme_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(ThemeType.Monokai);
        }

        private void SetSolarizedTheme_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(ThemeType.Solarized);
        }

        private void SetTheme(ThemeType themeType)
        {
            try
            {
                // 设置主题
                ThemeManager.SetTheme(themeType);
                var themeColors = ThemeManager.GetCurrentThemeColors();

                // 应用主题到主窗口
                ThemeApplier.ApplyThemeToMainWindow(this, themeColors);

                // 应用主题到所有标签页
                foreach (var tabItem in _tabItems)
                {
                    ThemeApplier.ApplyToTabItem(tabItem, themeColors);
                }

                // 更新菜单选中状态
                UpdateThemeMenuSelection(themeType);

                // 保存主题设置
                ConfigManager.SaveTheme(themeType);

                // 更新状态
                UpdateStatus($"主题已切换为: {themeColors.Name}，已保存设置");
            }
            catch (Exception ex)
            {
                UpdateStatus($"主题切换失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"主题切换错误: {ex}");
            }
        }

        private void UpdateThemeMenuSelection(ThemeType selectedTheme)
        {
            // 取消所有主题菜单项的选中状态
            DarkThemeMenuItem.IsChecked = false;
            LightThemeMenuItem.IsChecked = false;
            HighContrastThemeMenuItem.IsChecked = false;
            EyeCareThemeMenuItem.IsChecked = false;
            MonokaiThemeMenuItem.IsChecked = false;
            SolarizedThemeMenuItem.IsChecked = false;

            // 设置当前主题菜单项为选中状态
            switch (selectedTheme)
            {
                case ThemeType.Dark:
                    DarkThemeMenuItem.IsChecked = true;
                    break;
                case ThemeType.Light:
                    LightThemeMenuItem.IsChecked = true;
                    break;
                case ThemeType.HighContrast:
                    HighContrastThemeMenuItem.IsChecked = true;
                    break;
                case ThemeType.EyeCare:
                    EyeCareThemeMenuItem.IsChecked = true;
                    break;
                case ThemeType.Monokai:
                    MonokaiThemeMenuItem.IsChecked = true;
                    break;
                case ThemeType.Solarized:
                    SolarizedThemeMenuItem.IsChecked = true;
                    break;
            }
            
            // 同时更新工具栏上的ComboBox选择
            UpdateThemeComboBox(selectedTheme);
        }
        
        private void UpdateThemeComboBox(ThemeType selectedTheme)
        {
            if (ThemeSelector == null) return;
            
            // 临时移除事件处理器，避免循环触发
            ThemeSelector.SelectionChanged -= ThemeSelector_SelectionChanged;
            
            var themeTag = selectedTheme.ToString();
            foreach (ComboBoxItem item in ThemeSelector.Items)
            {
                if (item.Tag?.ToString() == themeTag)
                {
                    ThemeSelector.SelectedItem = item;
                    break;
                }
            }
            
            // 重新添加事件处理器
            ThemeSelector.SelectionChanged += ThemeSelector_SelectionChanged;
        }
        
        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 防止在初始化过程中触发
            if (!_isFullyInitialized || sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem selectedItem)
                return;
                
            // 防止重复设置相同主题
            var themeTag = selectedItem.Tag?.ToString();
            var currentTheme = ThemeManager.CurrentTheme.ToString();
            if (themeTag == currentTheme)
                return;
                
            if (Enum.TryParse<ThemeType>(themeTag, out var themeType))
            {
                SetTheme(themeType);
            }
        }

        #endregion

        #region 辅助方法

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            return parent is T ? (T)parent : FindParent<T>(parent);
        }

        #endregion

        #region 窗口事件

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            try
            {
                // 保存当前会话
                var sessionTabs = new List<ConfigManager.SessionTab>();
                foreach (var tabItem in _tabItems)
                {
                    sessionTabs.Add(new ConfigManager.SessionTab
                    {
                        FileName = tabItem.FileName,
                        FilePath = tabItem.FilePath,
                        Content = tabItem.Content,
                        Encoding = tabItem.Encoding,
                        IsModified = tabItem.IsModified,
                        CursorPosition = tabItem.TextEditor?.CaretIndex ?? 0,
                        SelectionStart = tabItem.TextEditor?.SelectionStart ?? 0,
                        SelectionLength = tabItem.TextEditor?.SelectionLength ?? 0
                    });
                }

                var activeTabIndex = _tabItems.IndexOf(_currentTab);
                ConfigManager.SaveSession(sessionTabs, activeTabIndex);

                // 停止自动缓存定时器
                _autoCacheTimer?.Stop();

                System.Diagnostics.Debug.WriteLine("会话已保存，应用程序正在关闭");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存会话失败: {ex.Message}");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Smart Text Editor v1.3\n\n" +
                "一个现代化的文本编辑器，具有多标签页、主题系统、文件对比等功能。\n\n" +
                "© 2025 Smart Text Editor Team",
                "关于",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        #region 自动缓存

        private void InitializeAutoCache()
        {
            try
            {
                // 确保缓存目录存在
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }

                // 初始化自动缓存定时器
                _autoCacheTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(60) // 每60秒自动缓存一次
                };
                _autoCacheTimer.Tick += AutoCacheTimer_Tick;
                _autoCacheTimer.Start();

                System.Diagnostics.Debug.WriteLine("自动缓存系统初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"自动缓存初始化失败: {ex.Message}");
            }
        }

        private async void AutoCacheTimer_Tick(object sender, EventArgs e)
        {
            await SaveSessionAsync();
            await CleanupOldCacheFiles();
        }

        private async Task SaveSessionAsync()
        {
            try
            {
                // 异步保存会话
                await Task.Run(() =>
                {
                    var sessionTabs = new List<ConfigManager.SessionTab>();
                    foreach (var tabItem in _tabItems)
                    {
                        sessionTabs.Add(new ConfigManager.SessionTab
                        {
                            FileName = tabItem.FileName,
                            FilePath = tabItem.FilePath,
                            Content = tabItem.Content,
                            Encoding = tabItem.Encoding,
                            IsModified = tabItem.IsModified,
                            CursorPosition = tabItem.TextEditor?.CaretIndex ?? 0,
                            SelectionStart = tabItem.TextEditor?.SelectionStart ?? 0,
                            SelectionLength = tabItem.TextEditor?.SelectionLength ?? 0
                        });
                    }

                    var activeTabIndex = _tabItems.IndexOf(_currentTab);
                    ConfigManager.SaveSession(sessionTabs, activeTabIndex);
                });

                System.Diagnostics.Debug.WriteLine("会话已自动保存");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"自动保存会话失败: {ex.Message}");
            }
        }

        private async Task CleanupOldCacheFiles()
        {
            try
            {
                await Task.Run(() =>
                {
                    var cacheFiles = Directory.GetFiles(_cacheDirectory, "*.cache");
                    var cutoffDate = DateTime.Now.AddDays(-7);

                    foreach (var file in cacheFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastWriteTime < cutoffDate)
                        {
                            try
                            {
                                File.Delete(file);
                                System.Diagnostics.Debug.WriteLine($"已删除旧缓存文件: {file}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"删除缓存文件失败 {file}: {ex.Message}");
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理缓存文件失败: {ex.Message}");
            }
        }

        #endregion

        #region 会话恢复

        private async Task RestoreLastSession()
        {
            try
            {
                var (sessionTabs, activeTabIndex) = ConfigManager.LoadSession();
                
                if (sessionTabs == null || sessionTabs.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("没有上次会话需要恢复");
                    return;
                }

                // 移除默认欢迎标签页（如果存在）
                if (_tabItems.Count > 0 && _tabItems[0].FileName == "欢迎")
                {
                    var welcomeTab = _tabItems[0];
                    _tabItems.RemoveAt(0);
                    
                    var tabControl = FileTabControl;
                    if (tabControl?.Items.Count > 0)
                    {
                        tabControl.Items.RemoveAt(0);
                    }
                }

                // 恢复每个标签页
                for (int i = 0; i < sessionTabs.Count; i++)
                {
                    var sessionTab = sessionTabs[i];
                    await RestoreTabFromSession(sessionTab);
                }

                // 设置活跃标签页
                if (activeTabIndex >= 0 && activeTabIndex < _tabItems.Count)
                {
                    SwitchToTab(_tabItems[activeTabIndex]);
                }
                else if (_tabItems.Count > 0)
                {
                    SwitchToTab(_tabItems[0]);
                }

                UpdateStatus($"已恢复上次会话：{sessionTabs.Count}个标签页");
                System.Diagnostics.Debug.WriteLine($"会话恢复完成：{sessionTabs.Count}个标签页");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复会话失败: {ex.Message}");
                UpdateStatus("恢复上次会话失败");
            }
        }

        private async Task RestoreTabFromSession(ConfigManager.SessionTab sessionTab)
        {
            try
            {
                // 在UI线程上执行所有操作，避免线程问题
                await Dispatcher.InvokeAsync(() =>
                {
                    // 如果有文件路径且文件存在，尝试重新加载文件
                    if (!string.IsNullOrEmpty(sessionTab.FilePath) && File.Exists(sessionTab.FilePath))
                    {
                        try
                        {
                            // 读取最新文件内容
                            var encoding = GetEncodingByName(sessionTab.Encoding);
                            var currentContent = File.ReadAllText(sessionTab.FilePath, encoding);
                            
                            // 如果文件内容没有变化或者会话中有未保存的修改，使用会话内容
                            var contentToUse = sessionTab.IsModified ? sessionTab.Content : currentContent;
                            CreateTabFromSession(sessionTab, contentToUse);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"读取文件失败，使用会话内容: {ex.Message}");
                            CreateTabFromSession(sessionTab, sessionTab.Content);
                        }
                    }
                    else
                    {
                        // 文件不存在或是新文件，使用会话中的内容
                        CreateTabFromSession(sessionTab, sessionTab.Content);
                    }
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复标签页失败: {ex.Message}");
            }
        }

        private void CreateTabFromSession(ConfigManager.SessionTab sessionTab, string content)
        {
            try
            {
                // 创建标签页
                CreateNewTab(sessionTab.FileName, content);
                
                // 获取刚创建的标签页
                var newTab = _tabItems.LastOrDefault();
                if (newTab != null)
                {
                    // 恢复属性
                    newTab.FilePath = sessionTab.FilePath;
                    newTab.Encoding = sessionTab.Encoding;
                    
                    // 设置原始内容和当前内容来恢复修改状态
                    newTab.OriginalContent = sessionTab.IsModified ? "" : content;
                    newTab.Content = content;
                    
                    // 恢复光标位置和选择
                    if (newTab.TextEditor != null)
                    {
                        newTab.TextEditor.CaretIndex = Math.Min(sessionTab.CursorPosition, content.Length);
                        if (sessionTab.SelectionLength > 0)
                        {
                            var selStart = Math.Min(sessionTab.SelectionStart, content.Length);
                            var selLength = Math.Min(sessionTab.SelectionLength, Math.Max(0, content.Length - selStart));
                            if (selLength > 0)
                            {
                                newTab.TextEditor.Select(selStart, selLength);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从会话创建标签页失败: {ex.Message}");
            }
        }

        private void SwitchToTab(FileTabItem tabItem)
        {
            if (tabItem == null) return;

            var tabControl = FileTabControl;
            if (tabControl == null) return;

            // 找到对应的TabItem UI元素
            foreach (TabItem tab in tabControl.Items)
            {
                if (tab.DataContext == tabItem)
                {
                    tabControl.SelectedItem = tab;
                    _currentTab = tabItem;
                    UpdateTitle();
                    UpdateCursorPosition();
                    UpdateSelectionInfo();
                    break;
                }
            }
        }

        #endregion
    }
}