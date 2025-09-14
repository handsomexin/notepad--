using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SmartTextEditor.Services;
using SmartTextEditor.Themes;

namespace SmartTextEditor.Windows
{
    /// <summary>
    /// 文件对比窗口
    /// </summary>
    public partial class FileCompareWindow : Window
    {
        private readonly EncodingDetector _encodingDetector;
        private string _leftFilePath;
        private string _rightFilePath;
        private bool _isComparing = false;

        public FileCompareWindow()
        {
            InitializeComponent();
            _encodingDetector = new EncodingDetector();
            InitializeWindow();
            ApplyCurrentTheme();
        }

        public FileCompareWindow(string leftContent, string rightContent, string leftTitle = "左侧内容", string rightTitle = "右侧内容") : this()
        {
            LoadContent(leftContent, rightContent, leftTitle, rightTitle);
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                var themeColors = ThemeManager.GetCurrentThemeColors();
                ApplyThemeToCompareWindow(themeColors);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题失败: {ex.Message}");
            }
        }

        private void ApplyThemeToCompareWindow(ThemeColors theme)
        {
            // 窗口背景
            this.Background = new SolidColorBrush(theme.WindowBackground);
            
            // 工具栏
            if (FindName("MainToolBar") is ToolBar toolBar)
            {
                toolBar.Background = new SolidColorBrush(theme.ToolBarBackground);
            }
            
            // 信息栏
            if (LeftFileInfo != null)
            {
                LeftFileInfo.Foreground = new SolidColorBrush(theme.TextForeground);
            }
            if (RightFileInfo != null)
            {
                RightFileInfo.Foreground = new SolidColorBrush(theme.TextForeground);
            }
            
            // 编辑器
            ApplyThemeToRichTextBox(LeftTextEditor, theme);
            ApplyThemeToRichTextBox(RightTextEditor, theme);
            
            // 行号
            ApplyThemeToLineNumbers(LeftLineNumbers, theme);
            ApplyThemeToLineNumbers(RightLineNumbers, theme);
            
            // 状态栏
            if (FindName("StatusBar") is StatusBar statusBar)
            {
                statusBar.Background = new SolidColorBrush(theme.StatusBarBackground);
                statusBar.Foreground = new SolidColorBrush(theme.TextForeground);
            }
        }

        private void ApplyThemeToRichTextBox(RichTextBox richTextBox, ThemeColors theme)
        {
            if (richTextBox != null)
            {
                richTextBox.Background = new SolidColorBrush(theme.EditorBackground);
                richTextBox.Foreground = new SolidColorBrush(theme.TextForeground);
                richTextBox.BorderBrush = new SolidColorBrush(theme.BorderColor);
            }
        }

        private void ApplyThemeToLineNumbers(TextBox lineNumbers, ThemeColors theme)
        {
            if (lineNumbers != null)
            {
                lineNumbers.Background = new SolidColorBrush(theme.LineNumberBackground);
                lineNumbers.Foreground = new SolidColorBrush(theme.LineNumberForeground);
                lineNumbers.BorderBrush = new SolidColorBrush(theme.BorderColor);
            }
        }

        #region 初始化

        private void InitializeWindow()
        {
            try
            {
                this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, (s, e) => StartCompare()));
                this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close()));
                
                // 初始化RichTextBox
                InitializeRichTextBoxes();
                
                UpdateLineNumbers();
                UpdateStatus("准备对比", false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化窗口失败: {ex.Message}");
            }
        }
        
        private void InitializeRichTextBoxes()
        {
            try
            {
                // 确保RichTextBox有默认文档
                if (LeftTextEditor.Document == null)
                {
                    LeftTextEditor.Document = new FlowDocument();
                }
                if (RightTextEditor.Document == null)
                {
                    RightTextEditor.Document = new FlowDocument();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化RichTextBox失败: {ex.Message}");
            }
        }

        private void LoadContent(string leftContent, string rightContent, string leftTitle, string rightTitle)
        {
            SetRichTextBoxContent(LeftTextEditor, leftContent ?? "");
            SetRichTextBoxContent(RightTextEditor, rightContent ?? "");
            
            LeftFileInfo.Text = $"左侧: {leftTitle}";
            RightFileInfo.Text = $"右侧: {rightTitle}";
            
            UpdateLineNumbers();
            UpdateCompareInfo();
        }

        #endregion

        #region 文件操作

        private void LoadLeftFile_Click(object sender, RoutedEventArgs e)
        {
            LoadFile(true);
        }

        private void LoadRightFile_Click(object sender, RoutedEventArgs e)
        {
            LoadFile(false);
        }

        private void LoadFile(bool isLeft)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = isLeft ? "选择左侧对比文件" : "选择右侧对比文件",
                    Filter = "所有文件 (*.*)|*.*|文本文件 (*.txt)|*.txt|C# 文件 (*.cs)|*.cs|JavaScript 文件 (*.js)|*.js|Python 文件 (*.py)|*.py"
                };

                if (dialog.ShowDialog() == true)
                {
                    var encodingResult = _encodingDetector.DetectFileEncoding(dialog.FileName);
                    var content = File.ReadAllText(dialog.FileName, encodingResult.Encoding);
                    var fileName = Path.GetFileName(dialog.FileName);

                    if (isLeft)
                    {
                        SetRichTextBoxContent(LeftTextEditor, content);
                        _leftFilePath = dialog.FileName;
                        LeftFileInfo.Text = $"左侧: {fileName} [{encodingResult.EncodingName}]";
                    }
                    else
                    {
                        SetRichTextBoxContent(RightTextEditor, content);
                        _rightFilePath = dialog.FileName;
                        RightFileInfo.Text = $"右侧: {fileName} [{encodingResult.EncodingName}]";
                    }

                    UpdateLineNumbers();
                    UpdateCompareInfo();
                    UpdateStatus($"已加载{(isLeft ? "左侧" : "右侧")}文件: {fileName}", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 剪贴板操作

        private void PasteToLeft_Click(object sender, RoutedEventArgs e)
        {
            PasteFromClipboard(true);
        }

        private void PasteToRight_Click(object sender, RoutedEventArgs e)
        {
            PasteFromClipboard(false);
        }

        private void PasteFromClipboard(bool isLeft)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var content = Clipboard.GetText();
                    
                    if (isLeft)
                    {
                        SetRichTextBoxContent(LeftTextEditor, content);
                        _leftFilePath = null;
                        LeftFileInfo.Text = "左侧: 来自剪贴板";
                    }
                    else
                    {
                        SetRichTextBoxContent(RightTextEditor, content);
                        _rightFilePath = null;
                        RightFileInfo.Text = "右侧: 来自剪贴板";
                    }

                    UpdateLineNumbers();
                    UpdateCompareInfo();
                    UpdateStatus($"已从剪贴板粘贴到{(isLeft ? "左侧" : "右侧")}", false);
                }
                else
                {
                    MessageBox.Show("剪贴板中没有文本内容", "提示", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"粘贴失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 文件对比

        private void StartCompare_Click(object sender, RoutedEventArgs e)
        {
            StartCompare();
        }

        private void StartCompare()
        {
            try
            {
                var leftText = GetRichTextBoxContent(LeftTextEditor);
                var rightText = GetRichTextBoxContent(RightTextEditor);
                
                if (string.IsNullOrEmpty(leftText) && string.IsNullOrEmpty(rightText))
                {
                    MessageBox.Show("请先加载要对比的内容", "提示", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _isComparing = true;
                UpdateStatus("正在对比...", true);

                // 执行对比
                var differences = CompareTexts(leftText, rightText);
                
                // 应用高亮
                ApplyHighlighting(differences, leftText, rightText);

                var diffCount = differences.Count;
                UpdateStatus($"对比完成，发现 {diffCount} 处差异", false);
                DifferenceCountText.Text = $"差异: {diffCount}";

                if (diffCount == 0)
                {
                    CompareStatusIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
                    MessageBox.Show("文件内容完全相同！", "对比结果", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    CompareStatusIndicator.Fill = new SolidColorBrush(Colors.Orange);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"对比失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("对比失败", false);
            }
            finally
            {
                _isComparing = false;
            }
        }

        private List<DifferenceInfo> CompareTexts(string leftText, string rightText)
        {
            var differences = new List<DifferenceInfo>();
            
            // 统一换行符处理
            leftText = leftText?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
            rightText = rightText?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
            
            var leftLines = leftText.Split(new[] { '\n' }, StringSplitOptions.None);
            var rightLines = rightText.Split(new[] { '\n' }, StringSplitOptions.None);
            
            var maxLines = Math.Max(leftLines.Length, rightLines.Length);
            
            for (int i = 0; i < maxLines; i++)
            {
                var leftLine = i < leftLines.Length ? leftLines[i] : "";
                var rightLine = i < rightLines.Length ? rightLines[i] : "";
                
                if (!string.Equals(leftLine, rightLine, StringComparison.Ordinal))
                {
                    differences.Add(new DifferenceInfo
                    {
                        LineNumber = i + 1,
                        LeftContent = leftLine,
                        RightContent = rightLine,
                        DifferenceType = GetDifferenceType(leftLine, rightLine)
                    });
                }
            }
            
            return differences;
        }

        private DifferenceType GetDifferenceType(string leftLine, string rightLine)
        {
            if (string.IsNullOrEmpty(leftLine)) return DifferenceType.Added;
            if (string.IsNullOrEmpty(rightLine)) return DifferenceType.Removed;
            return DifferenceType.Modified;
        }

        private void ApplyHighlighting(List<DifferenceInfo> differences, string leftText, string rightText)
        {
            try
            {
                // 清除原有高亮
                ClearHighlight();
                
                if (differences.Count == 0) return;
                
                // 重新设置内容并添加高亮
                SetRichTextBoxContentWithHighlight(LeftTextEditor, leftText, differences, true);
                SetRichTextBoxContentWithHighlight(RightTextEditor, rightText, differences, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用高亮失败: {ex.Message}");
            }
        }

        private void ClearHighlight_Click(object sender, RoutedEventArgs e)
        {
            ClearHighlight();
        }

        private void ClearHighlight()
        {
            try
            {
                // 重置RichTextBox背景
                LeftTextEditor.Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17));
                RightTextEditor.Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17));
                
                CompareStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x7D, 0x85, 0x90));
                DifferenceCountText.Text = "差异: -";
                
                UpdateStatus("已清除对比标记", false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清除高亮失败: {ex.Message}");
            }
        }

        #endregion

        #region UI更新

        private void UpdateLineNumbers()
        {
            try
            {
                UpdateLeftLineNumbers();
                UpdateRightLineNumbers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新行号失败: {ex.Message}");
            }
        }

        private void UpdateLeftLineNumbers()
        {
            var lineCount = GetRichTextBoxLineCount(LeftTextEditor);
            var lineNumbers = new StringBuilder();
            
            for (int i = 1; i <= lineCount; i++)
            {
                lineNumbers.AppendLine(i.ToString());
            }
            
            LeftLineNumbers.Text = lineNumbers.ToString();
        }

        private void UpdateRightLineNumbers()
        {
            var lineCount = GetRichTextBoxLineCount(RightTextEditor);
            var lineNumbers = new StringBuilder();
            
            for (int i = 1; i <= lineCount; i++)
            {
                lineNumbers.AppendLine(i.ToString());
            }
            
            RightLineNumbers.Text = lineNumbers.ToString();
        }

        private void UpdateCompareInfo()
        {
            try
            {
                var leftLines = GetRichTextBoxLineCount(LeftTextEditor);
                var rightLines = GetRichTextBoxLineCount(RightTextEditor);
                CompareInfoText.Text = $"行数: 左{leftLines} | 右{rightLines}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新对比信息失败: {ex.Message}");
            }
        }

        private void UpdateStatus(string message, bool isProcessing)
        {
            try
            {
                CompareStatusText.Text = message;
                if (isProcessing)
                {
                    CompareStatusIndicator.Fill = new SolidColorBrush(Colors.Yellow);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新状态失败: {ex.Message}");
            }
        }

        #endregion

        #region 事件处理

        private void LeftTextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isComparing)
            {
                UpdateLeftLineNumbers();
                UpdateCompareInfo();
            }
        }

        private void RightTextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isComparing)
            {
                UpdateRightLineNumbers();
                UpdateCompareInfo();
            }
        }

        private void LeftTextEditor_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 同步左侧行号滚动
            LeftLineNumbers.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private void RightTextEditor_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 同步右侧行号滚动
            RightLineNumbers.ScrollToVerticalOffset(e.VerticalOffset);
        }

        #endregion
        
        #region RichTextBox 辅助方法
        
        private void SetRichTextBoxContent(RichTextBox richTextBox, string content)
        {
            richTextBox.Document.Blocks.Clear();
            if (!string.IsNullOrEmpty(content))
            {
                var paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.LineHeight = 1;
                
                // 按行处理，避免自动添加额外格式
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    var run = new Run(lines[i]);
                    paragraph.Inlines.Add(run);
                    
                    // 添加换行符，除了最后一行
                    if (i < lines.Length - 1)
                    {
                        paragraph.Inlines.Add(new LineBreak());
                    }
                }
                
                richTextBox.Document.Blocks.Add(paragraph);
            }
        }
        
        private string GetRichTextBoxContent(RichTextBox richTextBox)
        {
            try
            {
                var textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                var text = textRange.Text;
                
                // 移除RichTextBox自动添加的末尾换行符
                if (text.EndsWith("\r\n"))
                    text = text.Substring(0, text.Length - 2);
                else if (text.EndsWith("\n"))
                    text = text.Substring(0, text.Length - 1);
                else if (text.EndsWith("\r"))
                    text = text.Substring(0, text.Length - 1);
                    
                return text;
            }
            catch
            {
                return "";
            }
        }
        
        private int GetRichTextBoxLineCount(RichTextBox richTextBox)
        {
            try
            {
                var content = GetRichTextBoxContent(richTextBox);
                if (string.IsNullOrEmpty(content))
                    return 1;
                    
                // 精确计算行数
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                return lines.Length;
            }
            catch
            {
                return 1;
            }
        }
        
        private void SetRichTextBoxContentWithHighlight(RichTextBox richTextBox, string content, List<DifferenceInfo> differences, bool isLeft)
        {
            richTextBox.Document.Blocks.Clear();
            
            if (string.IsNullOrEmpty(content))
            {
                // 即使内容为空，也要创建一个空段落
                var emptyParagraph = new Paragraph();
                emptyParagraph.Margin = new Thickness(0);
                emptyParagraph.LineHeight = 1;
                richTextBox.Document.Blocks.Add(emptyParagraph);
                return;
            }
                
            // 统一换行符处理
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = content.Split(new[] { '\n' }, StringSplitOptions.None);
            
            var paragraph = new Paragraph();
            paragraph.Margin = new Thickness(0);
            paragraph.LineHeight = 1;
            
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                var lineNumber = lineIndex + 1;
                
                // 检查这一行是否有差异
                var lineDiff = differences.FirstOrDefault(d => d.LineNumber == lineNumber);
                
                if (lineDiff != null)
                {
                    // 有差异的行，需要字符级高亮
                    var targetContent = isLeft ? lineDiff.LeftContent : lineDiff.RightContent;
                    var otherContent = isLeft ? lineDiff.RightContent : lineDiff.LeftContent;
                    
                    ApplyCharacterLevelHighlight(paragraph, targetContent, otherContent, lineDiff.DifferenceType);
                }
                else
                {
                    // 没有差异的行，正常显示
                    var run = new Run(line);
                    run.Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3));
                    paragraph.Inlines.Add(run);
                }
                
                // 添加换行符（除了最后一行）
                if (lineIndex < lines.Length - 1)
                {
                    paragraph.Inlines.Add(new LineBreak());
                }
            }
            
            richTextBox.Document.Blocks.Add(paragraph);
        }
        
        private void ApplyCharacterLevelHighlight(Paragraph paragraph, string currentLine, string otherLine, DifferenceType diffType)
        {
            if (string.IsNullOrEmpty(currentLine) && string.IsNullOrEmpty(otherLine))
                return;
                
            // 根据差异类型设置颜色
            Color highlightColor = diffType switch
            {
                DifferenceType.Added => Color.FromRgb(0x23, 0x86, 0x36),    // 绿色 - 新增
                DifferenceType.Removed => Color.FromRgb(0xDA, 0x36, 0x33),  // 红色 - 删除
                DifferenceType.Modified => Color.FromRgb(0xD2, 0x99, 0x22), // 黄色 - 修改
                _ => Color.FromRgb(0x7D, 0x85, 0x90)
            };
            
            if (string.IsNullOrEmpty(otherLine) || diffType == DifferenceType.Added || diffType == DifferenceType.Removed)
            {
                // 整行高亮
                var run = new Run(currentLine);
                run.Background = new SolidColorBrush(Color.FromArgb(100, highlightColor.R, highlightColor.G, highlightColor.B));
                run.Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3));
                paragraph.Inlines.Add(run);
            }
            else
            {
                // 字符级对比
                var charDiffs = GetCharacterDifferences(currentLine, otherLine);
                
                for (int i = 0; i < currentLine.Length; i++)
                {
                    var run = new Run(currentLine[i].ToString());
                    
                    if (charDiffs.Contains(i))
                    {
                        // 不同的字符高亮
                        run.Background = new SolidColorBrush(Color.FromArgb(150, highlightColor.R, highlightColor.G, highlightColor.B));
                        run.Foreground = new SolidColorBrush(Colors.White);
                    }
                    else
                    {
                        // 相同的字符正常显示
                        run.Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3));
                    }
                    
                    paragraph.Inlines.Add(run);
                }
            }
        }
        
        private HashSet<int> GetCharacterDifferences(string line1, string line2)
        {
            var differences = new HashSet<int>();
            var maxLength = Math.Max(line1.Length, line2.Length);
            
            for (int i = 0; i < maxLength; i++)
            {
                var char1 = i < line1.Length ? line1[i] : '\0';
                var char2 = i < line2.Length ? line2[i] : '\0';
                
                if (char1 != char2)
                {
                    if (i < line1.Length) differences.Add(i);
                }
            }
            
            return differences;
        }
        
        #endregion
    }

    #region 辅助类

    public class DifferenceInfo
    {
        public int LineNumber { get; set; }
        public string LeftContent { get; set; }
        public string RightContent { get; set; }
        public DifferenceType DifferenceType { get; set; }
    }

    public enum DifferenceType
    {
        Added,    // 右侧新增
        Removed,  // 左侧删除
        Modified  // 内容修改
    }

    #endregion
}