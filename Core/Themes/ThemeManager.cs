using System;
using System.Windows.Media;

namespace SmartTextEditor.Themes
{
    /// <summary>
    /// 主题类型枚举
    /// </summary>
    public enum ThemeType
    {
        Dark,           // 深色主题 (默认)
        Light,          // 浅色主题
        HighContrast,   // 高对比度主题
        EyeCare,        // 护眼主题
        Monokai,        // Monokai 编程主题
        Solarized       // Solarized 主题
    }

    /// <summary>
    /// 主题配色方案
    /// </summary>
    public class ThemeColors
    {
        public string Name { get; set; }
        public Color WindowBackground { get; set; }
        public Color MenuBackground { get; set; }
        public Color ToolBarBackground { get; set; }
        public Color StatusBarBackground { get; set; }
        public Color TabBackground { get; set; }
        public Color TabActiveBackground { get; set; }
        public Color TabBorder { get; set; }
        public Color TabActiveBorder { get; set; }
        public Color EditorBackground { get; set; }
        public Color LineNumberBackground { get; set; }
        public Color TextForeground { get; set; }
        public Color LineNumberForeground { get; set; }
        public Color SelectionBackground { get; set; }
        public Color BorderColor { get; set; }
        public Color ButtonBackground { get; set; }
        public Color ButtonHoverBackground { get; set; }
        public Color AccentColor { get; set; }
    }

    /// <summary>
    /// 主题管理器
    /// </summary>
    public static class ThemeManager
    {
        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Dark;
        public static event Action<ThemeType> ThemeChanged;
        
        // 添加缓存机制避免重复计算
        private static readonly ThemeColors[] _themeCache = new ThemeColors[6];
        private static ThemeColors _currentThemeColors;

        /// <summary>
        /// 获取指定主题的配色方案
        /// </summary>
        public static ThemeColors GetThemeColors(ThemeType theme)
        {
            // 检查缓存
            int themeIndex = (int)theme;
            if (_themeCache[themeIndex] != null)
            {
                return _themeCache[themeIndex];
            }

            var colors = theme switch
            {
                ThemeType.Dark => new ThemeColors
                {
                    Name = "深色主题",
                    WindowBackground = Color.FromRgb(0x0D, 0x11, 0x17),
                    MenuBackground = Color.FromRgb(0x16, 0x1B, 0x22),
                    ToolBarBackground = Color.FromRgb(0x16, 0x1B, 0x22),
                    StatusBarBackground = Color.FromRgb(0x16, 0x1B, 0x22),
                    TabBackground = Color.FromRgb(0x21, 0x26, 0x2D),
                    TabActiveBackground = Color.FromRgb(0x0D, 0x11, 0x17),
                    TabBorder = Color.FromRgb(0x30, 0x36, 0x3D),
                    TabActiveBorder = Color.FromRgb(0x58, 0xA6, 0xFF),
                    EditorBackground = Color.FromRgb(0x0D, 0x11, 0x17),
                    LineNumberBackground = Color.FromRgb(0x16, 0x1B, 0x22),
                    TextForeground = Color.FromRgb(0xE6, 0xED, 0xF3),
                    LineNumberForeground = Color.FromRgb(0x7D, 0x85, 0x90),
                    SelectionBackground = Color.FromRgb(0x58, 0xA6, 0xFF),
                    BorderColor = Color.FromRgb(0x30, 0x36, 0x3D),
                    ButtonBackground = Color.FromRgb(0x23, 0x86, 0x36),
                    ButtonHoverBackground = Color.FromRgb(0x2E, 0xA0, 0x43),
                    AccentColor = Color.FromRgb(0x58, 0xA6, 0xFF)
                },

                ThemeType.Light => new ThemeColors
                {
                    Name = "浅色主题",
                    WindowBackground = Color.FromRgb(0xFF, 0xFF, 0xFF),
                    MenuBackground = Color.FromRgb(0xF6, 0xF8, 0xFA),
                    ToolBarBackground = Color.FromRgb(0xF6, 0xF8, 0xFA),
                    StatusBarBackground = Color.FromRgb(0xF6, 0xF8, 0xFA),
                    TabBackground = Color.FromRgb(0xE1, 0xE4, 0xE8),
                    TabActiveBackground = Color.FromRgb(0xFF, 0xFF, 0xFF),
                    TabBorder = Color.FromRgb(0xD1, 0xD9, 0xE0),
                    TabActiveBorder = Color.FromRgb(0x0D, 0x66, 0xD0),
                    EditorBackground = Color.FromRgb(0xFF, 0xFF, 0xFF),
                    LineNumberBackground = Color.FromRgb(0xF6, 0xF8, 0xFA),
                    TextForeground = Color.FromRgb(0x24, 0x29, 0x2E),
                    LineNumberForeground = Color.FromRgb(0x65, 0x6D, 0x76),
                    SelectionBackground = Color.FromRgb(0x0D, 0x66, 0xD0),
                    BorderColor = Color.FromRgb(0xD1, 0xD9, 0xE0),
                    ButtonBackground = Color.FromRgb(0x1F, 0x88, 0x3D),
                    ButtonHoverBackground = Color.FromRgb(0x1A, 0x7F, 0x37),
                    AccentColor = Color.FromRgb(0x0D, 0x66, 0xD0)
                },

                ThemeType.HighContrast => new ThemeColors
                {
                    Name = "高对比度主题",
                    WindowBackground = Color.FromRgb(0x00, 0x00, 0x00),
                    MenuBackground = Color.FromRgb(0x00, 0x00, 0x00),
                    ToolBarBackground = Color.FromRgb(0x00, 0x00, 0x00),
                    StatusBarBackground = Color.FromRgb(0x00, 0x00, 0x00),
                    TabBackground = Color.FromRgb(0x1C, 0x1C, 0x1C),
                    TabActiveBackground = Color.FromRgb(0x00, 0x00, 0x00),
                    TabBorder = Color.FromRgb(0xFF, 0xFF, 0xFF),
                    TabActiveBorder = Color.FromRgb(0xFF, 0xFF, 0x00),
                    EditorBackground = Color.FromRgb(0x00, 0x00, 0x00),
                    LineNumberBackground = Color.FromRgb(0x1C, 0x1C, 0x1C),
                    TextForeground = Color.FromRgb(0xFF, 0xFF, 0xFF),
                    LineNumberForeground = Color.FromRgb(0xFF, 0xFF, 0x00),
                    SelectionBackground = Color.FromRgb(0x00, 0xFF, 0xFF),
                    BorderColor = Color.FromRgb(0xFF, 0xFF, 0xFF),
                    ButtonBackground = Color.FromRgb(0x00, 0x80, 0x00),
                    ButtonHoverBackground = Color.FromRgb(0x00, 0xFF, 0x00),
                    AccentColor = Color.FromRgb(0xFF, 0xFF, 0x00)
                },

                ThemeType.EyeCare => new ThemeColors
                {
                    Name = "护眼主题",
                    WindowBackground = Color.FromRgb(0xF7, 0xF3, 0xE3),
                    MenuBackground = Color.FromRgb(0xF0, 0xEB, 0xD8),
                    ToolBarBackground = Color.FromRgb(0xF0, 0xEB, 0xD8),
                    StatusBarBackground = Color.FromRgb(0xF0, 0xEB, 0xD8),
                    TabBackground = Color.FromRgb(0xE8, 0xE1, 0xCC),
                    TabActiveBackground = Color.FromRgb(0xF7, 0xF3, 0xE3),
                    TabBorder = Color.FromRgb(0xD4, 0xC5, 0xA0),
                    TabActiveBorder = Color.FromRgb(0x8B, 0x7D, 0x6B),
                    EditorBackground = Color.FromRgb(0xF7, 0xF3, 0xE3),
                    LineNumberBackground = Color.FromRgb(0xF0, 0xEB, 0xD8),
                    TextForeground = Color.FromRgb(0x3C, 0x3C, 0x3C),
                    LineNumberForeground = Color.FromRgb(0x8B, 0x7D, 0x6B),
                    SelectionBackground = Color.FromRgb(0xD4, 0xC5, 0xA0),
                    BorderColor = Color.FromRgb(0xD4, 0xC5, 0xA0),
                    ButtonBackground = Color.FromRgb(0x8B, 0x7D, 0x6B),
                    ButtonHoverBackground = Color.FromRgb(0x75, 0x6A, 0x5A),
                    AccentColor = Color.FromRgb(0x8B, 0x7D, 0x6B)
                },

                ThemeType.Monokai => new ThemeColors
                {
                    Name = "Monokai 主题",
                    WindowBackground = Color.FromRgb(0x27, 0x28, 0x22),
                    MenuBackground = Color.FromRgb(0x3E, 0x3D, 0x32),
                    ToolBarBackground = Color.FromRgb(0x3E, 0x3D, 0x32),
                    StatusBarBackground = Color.FromRgb(0x3E, 0x3D, 0x32),
                    TabBackground = Color.FromRgb(0x49, 0x48, 0x3E),
                    TabActiveBackground = Color.FromRgb(0x27, 0x28, 0x22),
                    TabBorder = Color.FromRgb(0x75, 0x71, 0x5E),
                    TabActiveBorder = Color.FromRgb(0xF9, 0x26, 0x72),
                    EditorBackground = Color.FromRgb(0x27, 0x28, 0x22),
                    LineNumberBackground = Color.FromRgb(0x3E, 0x3D, 0x32),
                    TextForeground = Color.FromRgb(0xF8, 0xF8, 0xF2),
                    LineNumberForeground = Color.FromRgb(0x75, 0x71, 0x5E),
                    SelectionBackground = Color.FromRgb(0xF9, 0x26, 0x72),
                    BorderColor = Color.FromRgb(0x75, 0x71, 0x5E),
                    ButtonBackground = Color.FromRgb(0xA6, 0xE2, 0x2E),
                    ButtonHoverBackground = Color.FromRgb(0x9B, 0xD0, 0x27),
                    AccentColor = Color.FromRgb(0xF9, 0x26, 0x72)
                },

                ThemeType.Solarized => new ThemeColors
                {
                    Name = "Solarized 主题",
                    WindowBackground = Color.FromRgb(0x00, 0x2B, 0x36),
                    MenuBackground = Color.FromRgb(0x07, 0x36, 0x42),
                    ToolBarBackground = Color.FromRgb(0x07, 0x36, 0x42),
                    StatusBarBackground = Color.FromRgb(0x07, 0x36, 0x42),
                    TabBackground = Color.FromRgb(0x58, 0x6E, 0x75),
                    TabActiveBackground = Color.FromRgb(0x00, 0x2B, 0x36),
                    TabBorder = Color.FromRgb(0x65, 0x7B, 0x83),
                    TabActiveBorder = Color.FromRgb(0x26, 0x8B, 0xD2),
                    EditorBackground = Color.FromRgb(0x00, 0x2B, 0x36),
                    LineNumberBackground = Color.FromRgb(0x07, 0x36, 0x42),
                    TextForeground = Color.FromRgb(0x83, 0x94, 0x96),
                    LineNumberForeground = Color.FromRgb(0x58, 0x6E, 0x75),
                    SelectionBackground = Color.FromRgb(0x26, 0x8B, 0xD2),
                    BorderColor = Color.FromRgb(0x65, 0x7B, 0x83),
                    ButtonBackground = Color.FromRgb(0x85, 0x99, 0x00),
                    ButtonHoverBackground = Color.FromRgb(0x9F, 0xB7, 0x00),
                    AccentColor = Color.FromRgb(0x26, 0x8B, 0xD2)
                },

                _ => GetThemeColors(ThemeType.Dark)
            };

            // 缓存结果
            _themeCache[themeIndex] = colors;
            return colors;
        }

        /// <summary>
        /// 切换主题
        /// </summary>
        public static void SetTheme(ThemeType theme)
        {
            // 避免重复设置相同主题
            if (CurrentTheme == theme) return;
            
            CurrentTheme = theme;
            _currentThemeColors = null; // 清除当前主题缓存
            ThemeChanged?.Invoke(theme);
        }

        /// <summary>
        /// 获取当前主题的配色方案
        /// </summary>
        public static ThemeColors GetCurrentThemeColors()
        {
            // 使用缓存避免重复计算
            if (_currentThemeColors != null)
            {
                return _currentThemeColors;
            }
            
            _currentThemeColors = GetThemeColors(CurrentTheme);
            return _currentThemeColors;
        }
    }
}