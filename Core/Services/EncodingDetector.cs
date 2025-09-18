using System;
using System.IO;
using System.Text;
using Ude;

namespace SmartTextEditor.Services
{
    /// <summary>
    /// 编码检测结果
    /// </summary>
    public class EncodingDetectionResult
    {
        public Encoding Encoding { get; set; }
        public string EncodingName { get; set; }
        public float Confidence { get; set; }
    }

    /// <summary>
    /// 智能编码检测器
    /// </summary>
    public class EncodingDetector
    {
        /// <summary>
        /// 检测文件编码
        /// </summary>
        public EncodingDetectionResult DetectFileEncoding(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            var bytes = File.ReadAllBytes(filePath);
            return DetectEncoding(bytes);
        }

        /// <summary>
        /// 检测字节数组编码
        /// </summary>
        public EncodingDetectionResult DetectEncoding(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return new EncodingDetectionResult
                {
                    Encoding = Encoding.UTF8,
                    EncodingName = "UTF-8",
                    Confidence = 1.0f
                };
            }

            // 检查BOM
            var bomResult = DetectBOM(bytes);
            if (bomResult != null)
                return bomResult;

            // 使用Ude库进行检测
            var detector = new CharsetDetector();
            detector.Feed(bytes, 0, bytes.Length);
            detector.DataEnd();

            if (detector.Charset != null && detector.Confidence > 0.5f)
            {
                try
                {
                    var encoding = GetEncodingFromCharset(detector.Charset);
                    return new EncodingDetectionResult
                    {
                        Encoding = encoding,
                        EncodingName = GetStandardEncodingName(encoding),
                        Confidence = detector.Confidence
                    };
                }
                catch
                {
                    // 如果获取编码失败，使用自定义检测
                }
            }

            // 自定义检测逻辑
            return CustomDetection(bytes);
        }

        /// <summary>
        /// BOM检测
        /// </summary>
        private EncodingDetectionResult DetectBOM(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return new EncodingDetectionResult
                {
                    Encoding = Encoding.UTF8,
                    EncodingName = "UTF-8",
                    Confidence = 0.99f
                };
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return new EncodingDetectionResult
                {
                    Encoding = Encoding.Unicode,
                    EncodingName = "UTF-16",
                    Confidence = 0.99f
                };
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return new EncodingDetectionResult
                {
                    Encoding = Encoding.BigEndianUnicode,
                    EncodingName = "UTF-16",
                    Confidence = 0.99f
                };
            }

            return null;
        }

        /// <summary>
        /// 自定义编码检测
        /// </summary>
        private EncodingDetectionResult CustomDetection(byte[] bytes)
        {
            // ASCII检测
            if (IsAscii(bytes))
            {
                return new EncodingDetectionResult
                {
                    Encoding = Encoding.ASCII,
                    EncodingName = "ASCII",
                    Confidence = 0.95f
                };
            }

            // UTF-8检测
            if (IsValidUtf8(bytes))
            {
                return new EncodingDetectionResult
                {
                    Encoding = Encoding.UTF8,
                    EncodingName = "UTF-8",
                    Confidence = 0.85f
                };
            }

            // 中文检测(GBK)
            if (HasChineseCharacters(bytes))
            {
                return new EncodingDetectionResult
                {
                    Encoding = Encoding.GetEncoding("GBK"),
                    EncodingName = "GBK",
                    Confidence = 0.75f
                };
            }

            // 默认UTF-8
            return new EncodingDetectionResult
            {
                Encoding = Encoding.UTF8,
                EncodingName = "UTF-8",
                Confidence = 0.5f
            };
        }

        /// <summary>
        /// 检查是否为ASCII
        /// </summary>
        private bool IsAscii(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                if (b > 127) return false;
            }
            return true;
        }

        /// <summary>
        /// 检查是否为有效UTF-8
        /// </summary>
        private bool IsValidUtf8(byte[] bytes)
        {
            try
            {
                var text = Encoding.UTF8.GetString(bytes);
                var reEncoded = Encoding.UTF8.GetBytes(text);
                
                if (reEncoded.Length != bytes.Length) return false;
                
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (reEncoded[i] != bytes[i]) return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查是否包含中文字符
        /// </summary>
        private bool HasChineseCharacters(byte[] bytes)
        {
            // 更准确的中文字符检测
            if (bytes == null || bytes.Length < 2)
                return false;

            try
            {
                // 尝试使用GBK编码解码，检查是否包含中文字符
                var gbkEncoding = Encoding.GetEncoding("GBK");
                var text = gbkEncoding.GetString(bytes);
                
                // 如果Unicode检测失败，回退到字节范围检测
                for (int i = 0; i < bytes.Length - 1; i++)
                {
                    byte b1 = bytes[i];
                    byte b2 = bytes[i + 1];

                    // GBK中文字符范围
                    if ((b1 >= 0xB0 && b1 <= 0xF7) && (b2 >= 0xA1 && b2 <= 0xFE))
                    {
                        return true;
                    }
                    
                    // GBK符号区
                    if ((b1 >= 0xA1 && b1 <= 0xA9) && (b2 >= 0xA1 && b2 <= 0xFE))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                // 如果出现异常，使用简单的字节范围检测
                for (int i = 0; i < bytes.Length - 1; i++)
                {
                    byte b1 = bytes[i];
                    byte b2 = bytes[i + 1];

                    // GBK中文字符范围
                    if ((b1 >= 0xB0 && b1 <= 0xF7) && (b2 >= 0xA1 && b2 <= 0xFE))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 从字符集名称获取编码
        /// </summary>
        private Encoding GetEncodingFromCharset(string charset)
        {
            return charset.ToUpper() switch
            {
                "UTF-8" => Encoding.UTF8,
                "GB18030" => Encoding.GetEncoding("GB18030"),
                "GBK" => Encoding.GetEncoding("GBK"),
                "GB2312" => Encoding.GetEncoding("GB2312"),
                "BIG5" => Encoding.GetEncoding("Big5"),
                "UTF-16LE" => Encoding.Unicode,
                "UTF-16BE" => Encoding.BigEndianUnicode,
                "ASCII" => Encoding.ASCII,
                "ISO-8859-1" => Encoding.GetEncoding("ISO-8859-1"),
                _ => Encoding.UTF8
            };
        }

        /// <summary>
        /// 获取标准编码名称
        /// </summary>
        private string GetStandardEncodingName(Encoding encoding)
        {
            if (encoding.Equals(Encoding.UTF8)) return "UTF-8";
            if (encoding.Equals(Encoding.Unicode)) return "UTF-16";
            if (encoding.Equals(Encoding.BigEndianUnicode)) return "UTF-16";
            if (encoding.Equals(Encoding.ASCII)) return "ASCII";
            
            var name = encoding.WebName.ToUpper();
            return name switch
            {
                "GB2312" => "GBK",
                "GBK" => "GBK",
                "GB18030" => "GBK",
                "ISO-8859-1" => "ISO-8859-1",
                _ => "UTF-8"
            };
        }

        /// <summary>
        /// 获取支持的编码列表
        /// </summary>
        public static string[] GetSupportedEncodings()
        {
            return new[] { "UTF-8", "GBK", "UTF-16", "ASCII", "ISO-8859-1" };
        }
    }
}