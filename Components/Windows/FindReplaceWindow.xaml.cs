using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartTextEditor.Themes;

namespace SmartTextEditor.Windows
{
    /// <summary>
    /// 查找替换窗口
    /// </summary>
    public partial class FindReplaceWindow : Window
    {
        private TextBox _targetTextBox;
        private int _lastFoundIndex = -1;
        private int _totalMatches = 0;
        private int _currentMatchIndex = 0;

        public FindReplaceWindow(TextBox targetTextBox)
        {
            InitializeComponent();
            _targetTextBox = targetTextBox;
            
            // 应用当前主题
            ApplyCurrentTheme();
            
            // 如果有选中文本，自动填入查找框
            if (!string.IsNullOrEmpty(_targetTextBox.SelectedText))
            {
                FindTextBox.Text = _targetTextBox.SelectedText;
            }
            
            // 焦点设置到查找框
            FindTextBox.Focus();
            
            // 绑定键盘事件
            this.KeyDown += FindReplaceWindow_KeyDown;
            FindTextBox.KeyDown += FindTextBox_KeyDown;
            ReplaceTextBox.KeyDown += ReplaceTextBox_KeyDown;
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                var theme = ThemeManager.GetCurrentThemeColors();
                
                this.Background = new SolidColorBrush(theme.WindowBackground);
                
                // 更新所有文本框样式
                ApplyTextBoxTheme(FindTextBox, theme);
                ApplyTextBoxTheme(ReplaceTextBox, theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题失败: {ex.Message}");
            }
        }

        private void ApplyTextBoxTheme(TextBox textBox, ThemeColors theme)
        {
            if (textBox != null)
            {
                textBox.Background = new SolidColorBrush(theme.EditorBackground);
                textBox.Foreground = new SolidColorBrush(theme.TextForeground);
                textBox.BorderBrush = new SolidColorBrush(theme.BorderColor);
                textBox.SelectionBrush = new SolidColorBrush(theme.SelectionBackground);
            }
        }

        #region 事件处理

        private void FindReplaceWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
            else if (e.Key == System.Windows.Input.Key.F3)
            {
                if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift)
                {
                    FindPrev_Click(null, null);
                }
                else
                {
                    FindNext_Click(null, null);
                }
                e.Handled = true;
            }
        }

        private void FindTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                FindNext_Click(null, null);
                e.Handled = true;
            }
        }

        private void ReplaceTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Replace_Click(null, null);
                e.Handled = true;
            }
        }

        #endregion

        #region 查找功能

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            FindText(true);
        }

        private void FindPrev_Click(object sender, RoutedEventArgs e)
        {
            FindText(false);
        }

        private void FindText(bool forward)
        {
            if (string.IsNullOrEmpty(FindTextBox.Text))
            {
                UpdateStatus("请输入要查找的内容", false);
                return;
            }

            try
            {
                var searchText = FindTextBox.Text;
                var targetText = _targetTextBox.Text;
                var startIndex = forward ? _targetTextBox.SelectionStart + _targetTextBox.SelectionLength : _targetTextBox.SelectionStart - 1;

                if (startIndex < 0) startIndex = targetText.Length - 1;
                if (startIndex >= targetText.Length) startIndex = 0;

                int foundIndex = -1;

                if (UseRegexCheckBox.IsChecked == true)
                {
                    foundIndex = FindWithRegex(targetText, searchText, startIndex, forward);
                }
                else
                {
                    foundIndex = FindWithString(targetText, searchText, startIndex, forward);
                }

                if (foundIndex >= 0)
                {
                    var matchLength = GetMatchLength(targetText, searchText, foundIndex);
                    _targetTextBox.Select(foundIndex, matchLength);
                    _targetTextBox.ScrollToLine(_targetTextBox.GetLineIndexFromCharacterIndex(foundIndex));
                    _lastFoundIndex = foundIndex;
                    
                    // 计算当前匹配的位置
                    CountMatches();
                    UpdateStatus($"找到匹配项 ({_currentMatchIndex}/{_totalMatches})", true);
                }
                else
                {
                    UpdateStatus(forward ? "已到达文档末尾" : "已到达文档开头", false);
                    
                    // 从头开始搜索
                    if (forward)
                    {
                        foundIndex = FindWithString(targetText, searchText, 0, true);
                    }
                    else
                    {
                        foundIndex = FindWithString(targetText, searchText, targetText.Length - 1, false);
                    }

                    if (foundIndex >= 0)
                    {
                        var matchLength = GetMatchLength(targetText, searchText, foundIndex);
                        _targetTextBox.Select(foundIndex, matchLength);
                        _targetTextBox.ScrollToLine(_targetTextBox.GetLineIndexFromCharacterIndex(foundIndex));
                        _lastFoundIndex = foundIndex;
                        
                        CountMatches();
                        UpdateStatus($"从{(forward ? "开头" : "末尾")}继续查找 ({_currentMatchIndex}/{_totalMatches})", true);
                    }
                    else
                    {
                        UpdateStatus("未找到匹配项", false);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"查找出错: {ex.Message}", false);
            }
        }

        private int FindWithString(string targetText, string searchText, int startIndex, bool forward)
        {
            var comparison = MatchCaseCheckBox.IsChecked == true ? 
                StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (MatchWholeWordCheckBox.IsChecked == true)
            {
                return FindWholeWord(targetText, searchText, startIndex, forward, comparison);
            }

            if (forward)
            {
                return targetText.IndexOf(searchText, startIndex, comparison);
            }
            else
            {
                return targetText.LastIndexOf(searchText, startIndex, comparison);
            }
        }

        private int FindWholeWord(string targetText, string searchText, int startIndex, bool forward, StringComparison comparison)
        {
            var currentIndex = startIndex;
            
            while (true)
            {
                int foundIndex;
                if (forward)
                {
                    foundIndex = targetText.IndexOf(searchText, currentIndex, comparison);
                    if (foundIndex == -1) return -1;
                }
                else
                {
                    foundIndex = targetText.LastIndexOf(searchText, currentIndex, comparison);
                    if (foundIndex == -1) return -1;
                }

                // 检查是否为完整单词
                bool isWholeWord = true;
                
                if (foundIndex > 0 && char.IsLetterOrDigit(targetText[foundIndex - 1]))
                    isWholeWord = false;
                
                if (foundIndex + searchText.Length < targetText.Length && 
                    char.IsLetterOrDigit(targetText[foundIndex + searchText.Length]))
                    isWholeWord = false;

                if (isWholeWord)
                    return foundIndex;

                if (forward)
                    currentIndex = foundIndex + 1;
                else
                    currentIndex = foundIndex - 1;

                if (currentIndex < 0 || currentIndex >= targetText.Length)
                    return -1;
            }
        }

        private int FindWithRegex(string targetText, string pattern, int startIndex, bool forward)
        {
            try
            {
                var options = RegexOptions.None;
                if (MatchCaseCheckBox.IsChecked != true)
                    options |= RegexOptions.IgnoreCase;

                var regex = new Regex(pattern, options);
                
                if (forward)
                {
                    var match = regex.Match(targetText, startIndex);
                    return match.Success ? match.Index : -1;
                }
                else
                {
                    var matches = regex.Matches(targetText.Substring(0, startIndex + 1));
                    return matches.Count > 0 ? matches[matches.Count - 1].Index : -1;
                }
            }
            catch (ArgumentException)
            {
                UpdateStatus("正则表达式格式错误", false);
                return -1;
            }
        }

        private int GetMatchLength(string targetText, string searchText, int foundIndex)
        {
            if (UseRegexCheckBox.IsChecked == true)
            {
                try
                {
                    var options = RegexOptions.None;
                    if (MatchCaseCheckBox.IsChecked != true)
                        options |= RegexOptions.IgnoreCase;

                    var regex = new Regex(searchText, options);
                    var match = regex.Match(targetText, foundIndex);
                    return match.Success ? match.Length : searchText.Length;
                }
                catch
                {
                    return searchText.Length;
                }
            }
            return searchText.Length;
        }

        private void CountMatches()
        {
            try
            {
                var searchText = FindTextBox.Text;
                var targetText = _targetTextBox.Text;
                
                if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(targetText))
                {
                    _totalMatches = 0;
                    _currentMatchIndex = 0;
                    return;
                }

                _totalMatches = 0;
                _currentMatchIndex = 0;

                if (UseRegexCheckBox.IsChecked == true)
                {
                    try
                    {
                        var options = RegexOptions.None;
                        if (MatchCaseCheckBox.IsChecked != true)
                            options |= RegexOptions.IgnoreCase;

                        var regex = new Regex(searchText, options);
                        var matches = regex.Matches(targetText);
                        _totalMatches = matches.Count;

                        // 找到当前选中的匹配是第几个
                        for (int i = 0; i < matches.Count; i++)
                        {
                            if (matches[i].Index <= _targetTextBox.SelectionStart)
                                _currentMatchIndex = i + 1;
                        }
                    }
                    catch
                    {
                        _totalMatches = 0;
                    }
                }
                else
                {
                    var comparison = MatchCaseCheckBox.IsChecked == true ? 
                        StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    
                    int index = 0;
                    while ((index = targetText.IndexOf(searchText, index, comparison)) != -1)
                    {
                        // 检查全字匹配
                        if (MatchWholeWordCheckBox.IsChecked == true)
                        {
                            bool isWholeWord = true;
                            if (index > 0 && char.IsLetterOrDigit(targetText[index - 1]))
                                isWholeWord = false;
                            if (index + searchText.Length < targetText.Length && 
                                char.IsLetterOrDigit(targetText[index + searchText.Length]))
                                isWholeWord = false;

                            if (isWholeWord)
                            {
                                _totalMatches++;
                                if (index <= _targetTextBox.SelectionStart)
                                    _currentMatchIndex = _totalMatches;
                            }
                        }
                        else
                        {
                            _totalMatches++;
                            if (index <= _targetTextBox.SelectionStart)
                                _currentMatchIndex = _totalMatches;
                        }
                        
                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"计算匹配数量失败: {ex.Message}");
                _totalMatches = 0;
                _currentMatchIndex = 0;
            }
        }

        #endregion

        #region 替换功能

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            if (_targetTextBox.SelectionLength == 0)
            {
                UpdateStatus("请先查找要替换的内容", false);
                return;
            }

            try
            {
                var selectedText = _targetTextBox.SelectedText;
                var searchText = FindTextBox.Text;
                var replaceText = ReplaceTextBox.Text ?? "";

                // 验证选中的文本是否匹配查找条件
                bool isMatch = false;
                
                if (UseRegexCheckBox.IsChecked == true)
                {
                    try
                    {
                        var options = RegexOptions.None;
                        if (MatchCaseCheckBox.IsChecked != true)
                            options |= RegexOptions.IgnoreCase;

                        var regex = new Regex(searchText, options);
                        isMatch = regex.IsMatch(selectedText);
                        
                        if (isMatch)
                        {
                            // 正则替换
                            replaceText = regex.Replace(selectedText, replaceText);
                        }
                    }
                    catch (ArgumentException)
                    {
                        UpdateStatus("正则表达式格式错误", false);
                        return;
                    }
                }
                else
                {
                    var comparison = MatchCaseCheckBox.IsChecked == true ? 
                        StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    
                    if (MatchWholeWordCheckBox.IsChecked == true)
                    {
                        isMatch = string.Equals(selectedText, searchText, comparison);
                    }
                    else
                    {
                        isMatch = selectedText.IndexOf(searchText, comparison) >= 0;
                    }
                }

                if (isMatch)
                {
                    var startIndex = _targetTextBox.SelectionStart;
                    _targetTextBox.SelectedText = replaceText;
                    _targetTextBox.Select(startIndex, replaceText.Length);
                    
                    UpdateStatus("已替换当前匹配项", true);
                    
                    // 自动查找下一个
                    FindNext_Click(null, null);
                }
                else
                {
                    UpdateStatus("选中的文本不匹配查找条件", false);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"替换出错: {ex.Message}", false);
            }
        }

        private void ReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FindTextBox.Text))
            {
                UpdateStatus("请输入要查找的内容", false);
                return;
            }

            try
            {
                var searchText = FindTextBox.Text;
                var replaceText = ReplaceTextBox.Text ?? "";
                var targetText = _targetTextBox.Text;
                int replaceCount = 0;

                if (UseRegexCheckBox.IsChecked == true)
                {
                    try
                    {
                        var options = RegexOptions.None;
                        if (MatchCaseCheckBox.IsChecked != true)
                            options |= RegexOptions.IgnoreCase;

                        var regex = new Regex(searchText, options);
                        var matches = regex.Matches(targetText);
                        replaceCount = matches.Count;
                        
                        if (replaceCount > 0)
                        {
                            var result = regex.Replace(targetText, replaceText);
                            _targetTextBox.Text = result;
                        }
                    }
                    catch (ArgumentException)
                    {
                        UpdateStatus("正则表达式格式错误", false);
                        return;
                    }
                }
                else
                {
                    var comparison = MatchCaseCheckBox.IsChecked == true ? 
                        StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    
                    if (MatchWholeWordCheckBox.IsChecked == true)
                    {
                        // 全字匹配替换
                        var result = ReplaceWholeWords(targetText, searchText, replaceText, comparison, out replaceCount);
                        _targetTextBox.Text = result;
                    }
                    else
                    {
                        // 普通替换
                        var originalLength = targetText.Length;
                        var result = targetText.Replace(searchText, replaceText, comparison);
                        _targetTextBox.Text = result;
                        
                        // 估算替换次数
                        if (searchText.Length > 0)
                        {
                            replaceCount = (originalLength - result.Length + (replaceText.Length * (originalLength - result.Length) / searchText.Length)) / searchText.Length;
                            if (replaceCount < 0) replaceCount = 0;
                        }
                    }
                }

                UpdateStatus($"已替换 {replaceCount} 处", replaceCount > 0);
                
                if (replaceCount > 0)
                {
                    _targetTextBox.Select(0, 0); // 移动光标到开头
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"全部替换出错: {ex.Message}", false);
            }
        }

        private string ReplaceWholeWords(string text, string searchText, string replaceText, StringComparison comparison, out int replaceCount)
        {
            replaceCount = 0;
            var result = text;
            var searchLength = searchText.Length;
            var replaceLength = replaceText.Length;
            
            int index = 0;
            while ((index = result.IndexOf(searchText, index, comparison)) != -1)
            {
                // 检查是否为完整单词
                bool isWholeWord = true;
                
                if (index > 0 && char.IsLetterOrDigit(result[index - 1]))
                    isWholeWord = false;
                
                if (index + searchLength < result.Length && 
                    char.IsLetterOrDigit(result[index + searchLength]))
                    isWholeWord = false;

                if (isWholeWord)
                {
                    result = result.Substring(0, index) + replaceText + result.Substring(index + searchLength);
                    replaceCount++;
                    index += replaceLength;
                }
                else
                {
                    index++;
                }
            }
            
            return result;
        }

        #endregion

        #region UI更新

        private void UpdateStatus(string message, bool isSuccess)
        {
            StatusText.Text = message;
            StatusText.Foreground = new SolidColorBrush(isSuccess ? 
                Color.FromRgb(0x3F, 0xB9, 0x50) : Color.FromRgb(0xDA, 0x36, 0x33));
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }

    // 扩展方法：字符串替换支持StringComparison
    public static class StringExtensions
    {
        public static string Replace(this string original, string pattern, string replacement, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(pattern))
                return original;

            var result = original;
            int index = 0;
            
            while ((index = result.IndexOf(pattern, index, comparison)) != -1)
            {
                result = result.Substring(0, index) + replacement + result.Substring(index + pattern.Length);
                index += replacement.Length;
            }
            
            return result;
        }
    }
}