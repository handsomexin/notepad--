# Smart Text Editor

一个现代化的智能文本编辑器，专为开发者和文档处理场景设计。

## 📁 目录结构

```
SmartTextEditor/
├── App.xaml                 # 应用程序入口点
├── MainWindow.xaml          # 主窗口界面
├── README.md                # 项目说明文档
├── SmartTextEditor.csproj   # 项目配置文件
├── 启动程序.bat             # 应用启动脚本
├── 构建发布版.bat           # 构建发布脚本
├── test_large_file.txt      # 测试文件
│
├── Components/              # 组件和窗口
│   ├── Models/              # 数据模型
│   │   └── FileTabItem.cs   # 标签页数据模型
│   └── Windows/             # 窗口界面
│       ├── FileCompareWindow.xaml      # 文件对比窗口
│       ├── FindReplaceWindow.xaml      # 查找替换窗口
│       ├── BackupManagerWindow.xaml    # 备份管理窗口
│       └── (对应的.xaml.cs文件)
│
├── Core/                    # 核心功能
│   ├── Services/            # 业务服务
│   │   ├── BackupManager.cs      # 备份管理服务
│   │   ├── ConfigManager.cs      # 配置管理服务
│   │   └── EncodingDetector.cs   # 编码检测服务
│   ├── Themes/              # 主题系统
│   │   ├── ThemeManager.cs       # 主题管理器
│   │   └── ThemeApplier.cs       # 主题应用器
│   └── StartupOptimizer.cs  # 启动优化器
│
├── Dialogs/                 # 对话框
│   ├── EncodingSelectionDialog.xaml    # 编码选择对话框
│   └── (对应的.xaml.cs文件)
│
├── Cache/                   # 缓存目录
├── bin/                     # 编译输出目录
├── obj/                     # 编译中间文件
└── publish/                 # 发布文件目录
```

## 🚀 主要功能

### 📝 基础编辑功能
- 多标签页编辑
- 左侧行号显示
- 智能编码检测与转换
- 文件对比功能
- 查找替换功能

### 🎨 界面特性
- 6种精美主题（深色、浅色、高对比度、护眼、Monokai、Solarized）
- 现代化GitHub Dark设计风格
- 响应式布局
- 主题记忆功能

### 💾 数据管理
- 自动缓存机制
- 智能备份系统
- 会话恢复功能
- 配置持久化

### ⚡ 性能优化
- 极速启动优化（30-50ms）
- 大文件处理优化
- 内存使用优化
- 异步处理机制

## 🛠️ 技术栈

- **语言**: C# (.NET 6)
- **框架**: WPF (Windows Presentation Foundation)
- **编码检测**: Ude.NetStandard
- **平台**: Windows x64

## 📦 构建和运行

### 环境要求
- .NET 6 SDK
- Windows操作系统

### 构建项目
```bash
# 使用构建脚本
.\构建发布版.bat

# 或使用命令行
dotnet build -c Release
```

### 运行应用
```bash
# 使用启动脚本
.\启动程序.bat

# 或直接运行
.\bin\Release\net6.0-windows\win-x64\SmartTextEditor.exe
```

## 🎯 使用指南

### 快捷键
- `Ctrl+N` - 新建文件
- `Ctrl+O` - 打开文件
- `Ctrl+S` - 保存文件
- `Ctrl+D` - 文件对比
- `Ctrl+F` - 查找
- `Ctrl+H` - 查找替换
- `Ctrl+Tab` - 切换标签页

### 主题切换
通过工具菜单 → 主题设置选择喜欢的主题，程序会自动记住您的选择。

## 📄 许可证

本项目仅供学习和使用。
