namespace SharpLog.Model
{
    /// <summary>
    /// 业余无线电传播模式模型
    /// （来源于 ADIF 官方 enumerations_propagation_mode.json 的 Propagation_Mode 枚举）。
    ///
    /// 【用途】
    ///   · 传播模式下拉列表的数据源
    ///   · 传播模式代码的合法性校验
    ///
    /// 【数据来源】
    ///   程序运行目录下：ADIFCode_Json\enumerations_propagation_mode.json
    ///   由 AdifDataLoader 在程序启动时一次性加载并转换为本类的实例集合。
    ///
    /// 【什么是传播模式】
    ///   描述无线电信号从发送方到接收方所经过的传播途径，例如：
    ///     · F2    电离层 F2 层反射（远距离 HF 通联的主要方式）
    ///     · ES    偶发 E 层反射
    ///     · MS    流星散射
    ///     · EME   月面反射（Earth-Moon-Earth）
    ///     · SAT   卫星转发
    ///     · TROPO 对流层波导
    ///   该值对应 ADIF 文件中的 PROP_MODE 字段。
    ///
    ///   ✅ 本项目下拉列表（StaticValues.Set_PropagationModeList）已于 2026-09-18 完成标准化：
    ///     · 官方 20 条代码全部收录；
    ///     · 原 "REPEATER" 改为官方的 "RPT"；
    ///     · 原首项带零宽字符（U+200C）的 "Simplex" 并非 ADIF 官方值，已删除，
    ///       改为 Code 为 null 的「（不指定）」项，并作为默认选中项。
    ///
    ///   【与本类的分工】
    ///     · 本类（AdifPropagationMode）保存的是**官方原始数据**（含英文描述），
    ///       由 AdifDataLoader 加载，用于校验与查询；
    ///     · 下拉列表使用的是**带中文显示名的 PropagationItem**，
    ///       两者职责不同，不要混用。
    /// </summary>
    public class AdifPropagationMode
    {
        /// <summary>
        /// 传播模式代码（ADIF 官方代码，全大写，如 "F2"、"ES"、"EME"、"SAT"）。
        /// 该值即为 ADIF 文件中 PROP_MODE 字段应写入的内容。
        /// </summary>
        public string Code { get; init; } = string.Empty;

        /// <summary>
        /// 传播模式的英文全称描述，例如 "F2 Reflection"（F2 层反射）、
        /// "Meteor scatter"（流星散射）。
        /// 用于在界面上给出更易理解的提示，或直接作为下拉项显示文本。
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// 返回便于阅读的文本形式，如 "F2 - F2 Reflection"。
        /// 主要用于调试与日志输出。
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Description) ? Code : $"{Code} - {Description}";
        }
    }
}
