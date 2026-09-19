using SharpLog.Model;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SharpLog.View.Converters
{
    /// <summary>
    /// EditQso 编辑窗口专用的一组值转换器。
    ///
    /// ════════════════════════════════════════════════════════════════════
    /// 与 LogViewConverters 的分工：
    ///   · LogViewConverters —— 日志查看【表格】的列显示转换
    ///   · 本文件            —— 编辑【表单】的视觉转换
    /// ════════════════════════════════════════════════════════════════════
    /// </summary>
    internal static class EditQsoConverterNotes
    {
        // 本类不参与运行，仅用于承载上面这段说明注释。
        // （C# 不允许把长注释挂在命名空间上，故用一个空类承载）
    }

    /// <summary>
    /// 通联模式文字 → 颜色条配色。
    ///
    /// 配色表复用 StaticValues.ModeColorMap —— 与 OneLog 卡片左侧竖条、
    /// 以及 ModeConvertTools.ResolveModeColor 是【同一套色表】，
    /// 保证同一个模式在卡片上和编辑窗口里颜色一致。
    ///
    /// 查找顺序（三级兜底）：
    ///   ① 整串精确匹配（形如 "SSB"、"FT4" 的纯模式名）
    ///   ② 取第一个空格前的部分再匹配
    ///      （本项目模式下拉框的显示名形如「SSB 单边带」，
    ///        直接拿整串查表会落空，故退一步只取模式代码部分）
    ///   ③ 仍不中 → DefaultModeColor 中性灰
    ///
    /// ⚠ 全程用 TryGetValue 而非字典索引器：
    ///   索引器在键不存在时会抛 KeyNotFoundException 直接崩溃，
    ///   而用户在编辑框里随手打字时几乎必然会命中未知值。
    /// </summary>
    public class ModeTextToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string modeText = (value as string)?.Trim() ?? string.Empty;

            // 输入框为空（新建记录时）→ 不显示颜色，
            // 否则色条上会挂着一块毫无意义的灰，看起来像"未加载"的故障态
            if (modeText.Length == 0)
            {
                return Brushes.Transparent;
            }

            // ① 整串精确匹配
            if (StaticValues.ModeColorMap.TryGetValue(modeText, out Color exactColor))
            {
                return new SolidColorBrush(exactColor);
            }

            // ② 取第一个空格前的部分（"SSB 单边带" → "SSB"）
            int spaceIndex = modeText.IndexOf(' ');
            if (spaceIndex > 0)
            {
                string modeCode = modeText.Substring(0, spaceIndex);
                if (StaticValues.ModeColorMap.TryGetValue(modeCode, out Color codeColor))
                {
                    return new SolidColorBrush(codeColor);
                }
            }

            // ③ 兜底：中性灰
            return new SolidColorBrush(StaticValues.DefaultModeColor);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;   // 色条是只读展示，不参与回写
    }
}
