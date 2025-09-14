using System.Collections.Generic;
using System.Windows;

namespace SmartTextEditor
{
    /// <summary>
    /// 编码选择对话框
    /// </summary>
    public partial class EncodingSelectionDialog : Window
    {
        public string SelectedEncoding { get; private set; }

        public EncodingSelectionDialog(string currentEncoding)
        {
            InitializeComponent();
            InitializeEncodings(currentEncoding);
        }

        private void InitializeEncodings(string currentEncoding)
        {
            CurrentEncodingText.Text = $"当前编码: {currentEncoding}";
            
            var encodings = new List<EncodingInfo>
            {
                new EncodingInfo { Name = "UTF-8", Description = "通用Unicode编码，推荐使用" },
                new EncodingInfo { Name = "GBK", Description = "中文简体编码" },
                new EncodingInfo { Name = "UTF-16", Description = "宽字符Unicode编码" },
                new EncodingInfo { Name = "ASCII", Description = "基本ASCII编码" },
                new EncodingInfo { Name = "ISO-8859-1", Description = "西欧字符编码" }
            };

            EncodingListBox.ItemsSource = encodings;
            
            // 选中当前编码
            foreach (EncodingInfo encoding in EncodingListBox.Items)
            {
                if (encoding.Name == currentEncoding)
                {
                    EncodingListBox.SelectedItem = encoding;
                    break;
                }
            }
            
            // 如果没有找到匹配的，默认选择第一个
            if (EncodingListBox.SelectedItem == null && EncodingListBox.Items.Count > 0)
            {
                EncodingListBox.SelectedIndex = 0;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (EncodingListBox.SelectedItem is EncodingInfo selectedEncoding)
            {
                SelectedEncoding = selectedEncoding.Name;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("请选择一个编码格式", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

    /// <summary>
    /// 编码信息类
    /// </summary>
    public class EncodingInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}