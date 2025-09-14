using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;

namespace SmartTextEditor.Models
{
    /// <summary>
    /// 文件标签页数据模型
    /// </summary>
    public class FileTabItem : INotifyPropertyChanged
    {
        private string _filePath;
        private string _fileName;
        private string _content;
        private string _originalContent;
        private bool _isModified;
        private string _encoding;
        private int _caretIndex;
        private int _selectionStart;
        private int _selectionLength;

        public FileTabItem()
        {
            _fileName = "无标题";
            _content = "";
            _originalContent = "";
            _encoding = "UTF-8";
            _isModified = false;
            Id = Guid.NewGuid().ToString();
        }

        public FileTabItem(string filePath) : this()
        {
            _filePath = filePath;
            _fileName = Path.GetFileName(filePath);
        }

        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                if (!string.IsNullOrEmpty(value))
                {
                    FileName = Path.GetFileName(value);
                }
                OnPropertyChanged(nameof(FilePath));
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// 显示名称（包含修改标记）
        /// </summary>
        public string DisplayName
        {
            get
            {
                var name = string.IsNullOrEmpty(_fileName) ? "无标题" : _fileName;
                return _isModified ? $"{name} *" : name;
            }
        }

        /// <summary>
        /// 文件内容
        /// </summary>
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                UpdateModifiedStatus();
                OnPropertyChanged(nameof(Content));
            }
        }

        /// <summary>
        /// 原始内容（用于检测修改状态）
        /// </summary>
        public string OriginalContent
        {
            get => _originalContent;
            set
            {
                _originalContent = value;
                UpdateModifiedStatus();
                OnPropertyChanged(nameof(OriginalContent));
            }
        }

        /// <summary>
        /// 是否已修改
        /// </summary>
        public bool IsModified
        {
            get => _isModified;
            private set
            {
                _isModified = value;
                OnPropertyChanged(nameof(IsModified));
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// 文件编码
        /// </summary>
        public string Encoding
        {
            get => _encoding;
            set
            {
                _encoding = value;
                OnPropertyChanged(nameof(Encoding));
            }
        }

        /// <summary>
        /// 光标位置
        /// </summary>
        public int CaretIndex
        {
            get => _caretIndex;
            set
            {
                _caretIndex = value;
                OnPropertyChanged(nameof(CaretIndex));
            }
        }

        /// <summary>
        /// 选择开始位置
        /// </summary>
        public int SelectionStart
        {
            get => _selectionStart;
            set
            {
                _selectionStart = value;
                OnPropertyChanged(nameof(SelectionStart));
            }
        }

        /// <summary>
        /// 选择长度
        /// </summary>
        public int SelectionLength
        {
            get => _selectionLength;
            set
            {
                _selectionLength = value;
                OnPropertyChanged(nameof(SelectionLength));
            }
        }

        /// <summary>
        /// 关联的文本编辑器控件
        /// </summary>
        public TextBox TextEditor { get; set; }

        /// <summary>
        /// 关联的行号控件
        /// </summary>
        public TextBox LineNumbersEditor { get; set; }

        /// <summary>
        /// 更新修改状态
        /// </summary>
        private void UpdateModifiedStatus()
        {
            IsModified = _content != _originalContent;
        }

        /// <summary>
        /// 标记为已保存
        /// </summary>
        public void MarkAsSaved()
        {
            _originalContent = _content;
            IsModified = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}