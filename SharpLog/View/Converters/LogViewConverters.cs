using SharpLog.Model;
using SharpLog.ToolsBox;
using SharpLog.ViewModel;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SharpLog.View.Converters
{
    /// <summary>
    /// 日志查看表格专用的一组值转换器。
    ///
    /// ════════════════════════════════════════════════════════════════════
    /// 【为什么需要转换器】
    ///   数据库实体 QsoLog 里存的是【ADIF 标准原始值】，
    ///   不能直接显示给用户看：
    ///     · FREQ 存 kHz 数字（7050），列表里要显示成所属波段（40m）
    ///     · MODE 存 ADIF 主模式（MFSK），要配合 SUBMODE（FT4）反查成界面显示名
    ///     · PROP_MODE 存代码（F2），要反查成中文名称
    ///     · TIME_ON 存 ADIF 的 HHmmss 字符串（143000），要显示成 14:30:00
    ///     · QSL_RCVD 是 bool，列表里显示「是 / 否」比 True/False 直观
    ///
    /// 【为什么不做成"显示模型"类】
    ///   QsoLog 有 30 多个字段，日志查看表格要全部列出来。
    ///   若为它写一个逐字段转发的显示模型，等于把 30 多个属性抄一遍，
    ///   日后每加一个字段都要同步改两处，容易漏。
    ///   用转换器则列绑定直接指向实体属性，只对【确实需要变形】的那几列做处理。
    ///
    /// 【用法】在 XAML 的资源字典里声明实例后，由列绑定引用，例如：
    ///   Converter={StaticResource FreqToBand}
    /// ════════════════════════════════════════════════════════════════════
    /// </summary>
    internal static class ConverterNotes
    {
        // 本类不参与运行，仅用于承载上面这段说明注释。
        // （C# 不允许把长注释挂在命名空间上，故用一个空类承载）
    }

    /// <summary>
    /// 频率（kHz）→ 所属业余波段名称（如 7050 → "40m"）。
    ///
    /// ⚠ 单位换算：本项目界面与数据库用【kHz】（7050），
    ///   而 ADIF 官方波段表用【MHz】（7.05），所以查表前必须先除以 1000。
    /// </summary>
    public class FreqToBandConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 频率非数字或非正数时返回空串，界面上这一格就是空白，不显示误导信息
            if (value is not double freqKhz || freqKhz <= 0)
            {
                return string.Empty;
            }

            double freqMhz = freqKhz / 1000.0;

            // 在官方波段表里找包含该频率的那一条。
            // 找不到说明这个频率不属于任何业余波段（例如用户随手填的），返回空串。
            AdifBand? band = AdifDataLoader.Bands.FirstOrDefault(b => b.IsInRange(freqMhz));
            return band?.Band ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;   // 本列只读，不参与回写
    }

    /// <summary>
    /// 通联模式 → 界面显示名（多值绑定：MODE + SUBMODE 两级反查成一个名字）。
    ///
    /// 【为什么必须反查】
    ///   数据库里 MODE 存的是 ADIF 官方【主模式】，不是界面上看到的那个名字。
    ///   例如界面选「FT4」，入库时被拆成 MODE="MFSK" + SUBMODE="FT4"；
    ///   界面选「D-STAR」，入库是 MODE="DIGITALVOICE" + SUBMODE="DSTAR"。
    ///   若直接把 MODE 显示出来，用户会看到一堆 MFSK / DIGITALVOICE，
    ///   既不是他选的那个，也分不清是 FT4 还是 JS8。
    ///   所以必须用 ResolveDisplayName(MODE, SUBMODE) 反查回显示名。
    /// </summary>
    public class ModeDisplayConverter : IMultiValueConverter
    {
        /// <summary>
        /// values[0] = MODE（主模式），values[1] = SUBMODE（子模式，可为 null）
        /// </summary>
        public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0) return string.Empty;

            string? mode = values[0] as string;
            string? submode = values.Length > 1 ? values[1] as string : null;

            return ModeConvertTools.ResolveDisplayName(mode, submode);
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => targetTypes.Select(_ => (object?)Binding.DoNothing).ToArray();
    }

    /// <summary>
    /// 传播模式代码 → 中文显示名（如 "F2" → "F2 电离层反射传播"）。
    /// 未填写传播方式时返回空串，该格显示为空白。
    /// </summary>
    public class PropModeDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => ModeConvertTools.ResolvePropDisplayName(value as string);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>
    /// ADIF 时间字符串 → 带冒号的钟表写法（"143000" → "14:30:00"）。
    ///
    /// 【背景】ADIF 规范的时间格式是 HHmmss 定长字符串，不含冒号。
    ///   直接显示成 143000 用户不好读，故插入冒号。
    /// 长度不足 6 位（异常数据）时原样返回，不猜也不报错。
    /// </summary>
    public class TimeDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string? raw = value as string;

            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            if (raw.Length != 6) return raw;   // 长度不对就当异常数据，原样显示

            return $"{raw.Substring(0, 2)}:{raw.Substring(2, 2)}:{raw.Substring(4, 2)}";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>
    /// 布尔值 → 「是 / 否」。
    /// 数据库里的 QSL_RCVD、IsConfirmed 都是 bool，
    /// 表格里显示 True/False 既难看也不符合中文界面习惯。
    /// </summary>
    public class BoolToYesNoConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? "是" : "否";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
